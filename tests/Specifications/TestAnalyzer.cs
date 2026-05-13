using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using StructureGuard;

namespace Specifications;

public class TestAnalyzer(string rootNamespace, IList<Dependency> allowedDependencies) : SliceAnalyzer
{
    protected override void OnInitialize(AnalysisContext context)
    {
        RootNameSpace = rootNamespace;
        PermittedDependencies = allowedDependencies;
        base.OnInitialize(context);
    }
}

public class CustomContext<TVerifier>(string rootNamespace, IList<Dependency> allowedDependencies) : AnalyzerTest<TVerifier>
    where TVerifier : IVerifier, new()
{
    private static readonly LanguageVersion DefaultLanguageVersion =
        Enum.TryParse("Default", out LanguageVersion version) ? version : LanguageVersion.CSharp6;

    protected override string DefaultFileExt => "cs";

    public override string Language => LanguageNames.CSharp;

    protected override CompilationOptions CreateCompilationOptions()
        => new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true);

    protected override ParseOptions CreateParseOptions()
        => new CSharpParseOptions(DefaultLanguageVersion, DocumentationMode.Diagnose);

    protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        => new[] { new TestAnalyzer(rootNamespace, allowedDependencies) };
}