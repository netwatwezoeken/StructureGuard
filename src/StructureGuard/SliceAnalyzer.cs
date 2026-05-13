using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace StructureGuard
{
    public abstract class SliceAnalyzer : DiagnosticAnalyzer
    {
        public static string RootNameSpace { get; set; } = null;

        public IList<Dependency> PermittedDependencies { get; set; } = new List<Dependency>();

        public static readonly DiagnosticDescriptor LayerDependencyRule = new DiagnosticDescriptor(
            "STR001",
            "Layer dependency not allowed",
            "References from layer '{0}' to layer '{1}' are not allowed",
            "Design",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            "This Analyzer only allows configured dependencies between layers.");

        public override void Initialize(AnalysisContext context)
        {
            context.EnableConcurrentExecution();
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            OnInitialize(context);
        }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(LayerDependencyRule);

        protected virtual void OnInitialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyseMethodSyntax, SyntaxKind.InvocationExpression);
            context.RegisterSyntaxNodeAction(AnalyseAttributeSyntax, SyntaxKind.Attribute);
            context.RegisterSyntaxNodeAction(AnalyseType, 
                SyntaxKind.ObjectCreationExpression, 
                SyntaxKind.Parameter, 
                SyntaxKind.FieldDeclaration,
                SyntaxKind.SimpleBaseType,
                SyntaxKind.CastExpression,
                SyntaxKind.PropertyDeclaration,
                SyntaxKind.VariableDeclaration,
                SyntaxKind.MethodDeclaration,
                SyntaxKind.AsExpression,
                SyntaxKind.IsExpression,
                SyntaxKind.TypeOfExpression,
                SyntaxKind.SimpleMemberAccessExpression,
                SyntaxKind.GenericName
                );
        }
    
        private void AnalyseAttributeSyntax(SyntaxNodeAnalysisContext context)
        {
            if (context.Node is AttributeSyntax attributeNode)
            {
                var symbolInfo = context.SemanticModel.GetSymbolInfo(attributeNode);
                var toNamespace = symbolInfo.Symbol?.ContainingNamespace.ToString();
                var to = NamespaceMatcher.ToCodePart(RootNameSpace, toNamespace);
                ReportIfViolatingNew(context, to);
            }
        }
    
        private void AnalyseType(SyntaxNodeAnalysisContext context)
        { 
            switch (context.Node)
            {
                case ObjectCreationExpressionSyntax oces:
                    AnalyzeType(context, oces.Type);
                    break;
                case ParameterSyntax ps:
                    AnalyzeType(context, ps.Type);
                    break;
                case FieldDeclarationSyntax fds:
                    AnalyzeType(context, fds.Declaration.Type);
                    break;
                case PropertyDeclarationSyntax pds:
                    AnalyzeType(context, pds.Type);
                    break;
                case VariableDeclarationSyntax vds:
                    if (vds.Parent is not FieldDeclarationSyntax && vds.Parent is not PropertyDeclarationSyntax)
                    {
                        // Avoid duplicate when using 'var' (type inferred from initializer or expression)
                        if (vds.Type is not IdentifierNameSyntax ins || !ins.IsVar)
                            AnalyzeType(context, vds.Type);
                    }
                    break;
                case MethodDeclarationSyntax mds:
                    AnalyzeType(context, mds.ReturnType);
                    break;
                case SimpleBaseTypeSyntax sbst:
                    AnalyzeType(context, sbst.Type);
                    break;
                case CastExpressionSyntax ces:
                    AnalyzeType(context, ces.Type);
                    break;
                case BinaryExpressionSyntax bes when bes.IsKind(SyntaxKind.AsExpression):
                    if (bes.Right is TypeSyntax asType)
                        AnalyzeType(context, asType);
                    break;
                case BinaryExpressionSyntax bes when bes.IsKind(SyntaxKind.IsExpression):
                    if (bes.Right is TypeSyntax isType)
                        AnalyzeType(context, isType);
                    break;
                case TypeOfExpressionSyntax toes:
                    AnalyzeType(context, toes.Type);
                    break;
                case GenericNameSyntax gns:
                    foreach (var arg in gns.TypeArgumentList.Arguments)
                    {
                        AnalyzeType(context, arg);
                    }
                    break;
                case MemberAccessExpressionSyntax maes:
                    var symbolInfo = context.SemanticModel.GetSymbolInfo(maes);
                    // Skip methods here to avoid duplicate with invocation handler (covers extension methods too)
                    if (symbolInfo.Symbol is not IMethodSymbol)
                    {
                        var toNamespace = symbolInfo.Symbol?.ContainingNamespace.ToString();
                        var to = NamespaceMatcher.ToCodePart(RootNameSpace, toNamespace);
                        ReportIfViolatingNew(context, to);
                    }
                    break;
            }
        }

        private void AnalyzeType(SyntaxNodeAnalysisContext context, TypeSyntax type)
        {
            var to = GetCodePart(context, type);
            ReportIfViolatingNew(context, to, type.GetLocation());
        }

        private void AnalyseMethodSyntax(SyntaxNodeAnalysisContext context)
        {
            // Get the invocation expression
            var invocationNode = (InvocationExpressionSyntax)context.Node;

            // Get the symbol info for the invoked method
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocationNode);
            var fromNamespace = GetContainingNamespace(context.Node.SyntaxTree);
            var fromNamespaceThing = NamespaceMatcher.ToCodePart(RootNameSpace, fromNamespace);
            var toNamespace = symbolInfo.Symbol?.ContainingNamespace.ToString();
            var toNamespaceThing = NamespaceMatcher.ToCodePart(RootNameSpace, toNamespace);
            var namespaceDependencies = new Dictionary<CodePart, HashSet<(CodePart, Location, string)>>();

            if (fromNamespaceThing != null && toNamespaceThing != null && fromNamespaceThing != toNamespaceThing &&
                toNamespaceThing.Layer != null)
            {
                if (!namespaceDependencies.TryGetValue(fromNamespaceThing, out var dependencies))
                {
                    dependencies = new HashSet<(CodePart, Location, string)>();
                    namespaceDependencies[fromNamespaceThing] = dependencies;
                }

                dependencies.Add((toNamespaceThing, context.Node.GetLocation(), "Method"));
            }
        
            ReportViolatingDependencies(context, namespaceDependencies);
        }
    
        private void ReportViolatingDependencies(SyntaxNodeAnalysisContext context, Dictionary<CodePart, HashSet<(CodePart, Location, string)>> namespaceDependencies)
        {
            var actualDeps = namespaceDependencies.Select(np =>
            {
                var l = new List<Dependency>();
                foreach (var t in np.Value)
                {
                    l.Add(new Dependency(new Layer(np.Key.Layer), new Layer(np.Value.First().Item1.Layer)));
                }

                return l;
            });

            foreach (var d in actualDeps)
            {
                if (d.Any(dd => !PermittedDependencies.Contains(dd)))
                {
                    var violatingDependency = d.First(dd => !PermittedDependencies.Contains(dd));
                    var location = namespaceDependencies.First().Value.First().Item2;
                    context.ReportDiagnostic(Diagnostic.Create(LayerDependencyRule,
                        namespaceDependencies.First().Value.First().Item2,
                        violatingDependency.From.Name, violatingDependency.To.Name, namespaceDependencies.First().Value.First().Item3)
                    );
                }
            }
        }

        private string GetContainingNamespace(SyntaxTree syntaxTree)
        {
            var root = syntaxTree.GetRoot();

            // Find the namespace declaration containing the current node
            var namespaceDeclaration = root
                .DescendantNodes()
                .OfType<FileScopedNamespaceDeclarationSyntax>()
                .FirstOrDefault();

            var namespaceDeclaration2 = root
                .DescendantNodes()
                .OfType<NamespaceDeclarationSyntax>()
                .FirstOrDefault();

            return namespaceDeclaration?.Name.ToString() ?? namespaceDeclaration2?.Name.ToString();
        }
    
        private void ReportIfViolatingNew(SyntaxNodeAnalysisContext context, CodePart to, Location location = null)
        {
            var from = GetCodePart(context, context.Node.SyntaxTree);
            if (to is null || !to.FullName.StartsWith(RootNameSpace))
                return;
            if (from is null || !from.FullName.StartsWith(RootNameSpace))
                return;
            var actualDependency = new Dependency(new Layer(from.Layer), new Layer(to.Layer));
        
            if (actualDependency.From == actualDependency.To)
                return;
        
            if (location is null)
            {
                location = context.Node.GetLocation();
            }
            if (!PermittedDependencies.Contains(actualDependency))
            {
                context.ReportDiagnostic(Diagnostic.Create(LayerDependencyRule,
                    location,
                    actualDependency.From.Name, actualDependency.To.Name)
                );
            }
        }

        private CodePart GetCodePart(SyntaxNodeAnalysisContext context, SyntaxTree tree)
        {
            var fullNamespace = GetContainingNamespace(tree);
            return NamespaceMatcher.ToCodePart(RootNameSpace, fullNamespace);
        }
    
        private static CodePart GetCodePart(SyntaxNodeAnalysisContext context, TypeSyntax type)
        {
            if (type is null)
                return null;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(type);
            var fullNamespace = symbolInfo.Symbol?.ContainingNamespace.ToString();
            return NamespaceMatcher.ToCodePart(RootNameSpace, fullNamespace);
        }
    }
}