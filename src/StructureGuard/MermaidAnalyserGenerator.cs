using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace StructureGuard
{
    [Generator]
    public sealed class MermaidDependencyAnalyzerGenerator : IIncrementalGenerator
    {
        private static readonly DiagnosticDescriptor MissingNamespaceRule = new DiagnosticDescriptor(
            "STR999",
            "Missing namespace in Mermaid file",
            "The Mermaid file '{0}' does not specify a root namespace in its YAML header. Please add 'namespace: YourNamespace' to the YAML front matter.",
            "Syntax",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
            
            IncrementalValuesProvider<MermaidFile> mermaidFiles =
                context.AdditionalTextsProvider
                    .Where(file =>
                        file.Path.EndsWith(".mmd", StringComparison.OrdinalIgnoreCase) ||
                        file.Path.EndsWith(".mermaid", StringComparison.OrdinalIgnoreCase))
                    .Select((file, cancellationToken) =>
                    {
                        SourceText sourceText = file.GetText(cancellationToken);

                        if (sourceText == null)
                        {
                            return null;
                        }

                        string content = sourceText.ToString();
                        string rootNamespace = ParseNamespaceFromYaml(content);

                        return new MermaidFile(file.Path, content, rootNamespace);
                    })
                    .Where(file => file != null);

            context.RegisterSourceOutput(mermaidFiles, (sourceProductionContext, mermaidFile) =>
            {
                if (string.IsNullOrWhiteSpace(mermaidFile.RootNamespace))
                {
                    sourceProductionContext.ReportDiagnostic(Diagnostic.Create(
                        MissingNamespaceRule,
                        Location.None,
                        mermaidFile.Path));
                    return;
                }

                List<DependencyEdge> dependencies = ParseMermaidDependencies(mermaidFile.Content);
            
                if (dependencies.Count == 0)
                {
                    return;
                }
            
                string generatedSource = GenerateAnalyzerSource(
                    rootNamespace: mermaidFile.RootNamespace,
                    analyzerNamespace: "Analyzer",
                    analyzerClassName: "Analyzer",
                    dependencies: dependencies);
            
                sourceProductionContext.AddSource(
                    "Analyzer.g.cs",
                    SourceText.From(generatedSource, Encoding.UTF8));
            });
        }

        private static string ParseNamespaceFromYaml(string content)
        {
            string[] lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            bool inYaml = false;
            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line == "---")
                {
                    if (!inYaml)
                    {
                        inYaml = true;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }

                if (inYaml)
                {
                    if (line.StartsWith("namespace:", StringComparison.OrdinalIgnoreCase))
                    {
                        return line.Substring("namespace:".Length).Trim();
                    }
                }
            }

            return null;
        }

        private static List<DependencyEdge> ParseMermaidDependencies(string mermaid)
        {
            List<DependencyEdge> dependencies = new List<DependencyEdge>();

            string[] lines = mermaid.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            foreach (string rawLine in lines)
            {
                string line = rawLine.Trim();

                if (line.Length == 0)
                {
                    continue;
                }

                if (line.StartsWith("graph ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                int arrowIndex = line.IndexOf("-->", StringComparison.Ordinal);

                if (arrowIndex < 0)
                {
                    continue;
                }

                string from = CleanNodeName(line.Substring(0, arrowIndex));
                string to = CleanNodeName(line.Substring(arrowIndex + 3));

                if (from.Length == 0 || to.Length == 0)
                {
                    continue;
                }

                dependencies.Add(new DependencyEdge(from, to));
            }

            return dependencies;
        }

        private static string CleanNodeName(string value)
        {
            value = value.Trim();

            int commentIndex = value.IndexOf("%%", StringComparison.Ordinal);
            if (commentIndex >= 0)
            {
                value = value.Substring(0, commentIndex).Trim();
            }

            value = value.Trim(';');
            value = value.Trim();

            if (value.StartsWith("\"", StringComparison.Ordinal) &&
                value.EndsWith("\"", StringComparison.Ordinal) &&
                value.Length >= 2)
            {
                value = value.Substring(1, value.Length - 2);
            }

            return value.Trim();
        }

        private static string GenerateAnalyzerSource(
            string rootNamespace,
            string analyzerNamespace,
            string analyzerClassName,
            List<DependencyEdge> dependencies)
        {
            StringBuilder builder = new StringBuilder();

            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using Microsoft.CodeAnalysis;");
            builder.AppendLine("using Microsoft.CodeAnalysis.Diagnostics;");
            builder.AppendLine("using StructureGuard;");
            builder.AppendLine();
            builder.AppendLine("namespace " + analyzerNamespace);
            builder.AppendLine("{");
            builder.AppendLine("    [DiagnosticAnalyzer(LanguageNames.CSharp)]");
            builder.AppendLine("    public class " + analyzerClassName + " : SliceAnalyzer");
            builder.AppendLine("    {");
            builder.AppendLine("        protected override void OnInitialize(AnalysisContext context)");
            builder.AppendLine("        {");
            builder.AppendLine("            RootNameSpace = \"" + EscapeString(rootNamespace) + "\";");
            builder.AppendLine("            PermittedDependencies = new List<Dependency>()");
            builder.AppendLine("            {");

            for (int i = 0; i < dependencies.Count; i++)
            {
                DependencyEdge dependency = dependencies[i];

                builder.Append("                new Dependency(new Layer(\"");
                builder.Append(EscapeString(dependency.From));
                builder.Append("\"), new Layer(\"");
                builder.Append(EscapeString(dependency.To));
                builder.Append("\"))");

                if (i < dependencies.Count - 1)
                {
                    builder.Append(",");
                }

                builder.AppendLine();
            }

            builder.AppendLine("            };");
            builder.AppendLine("            base.OnInitialize(context);");
            builder.AppendLine("        }");
            builder.AppendLine("    }");
            builder.AppendLine("}");

            return builder.ToString();
        }

        private static string EscapeString(string value)
        {
            return value.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private sealed class MermaidFile
        {
            public MermaidFile(string path, string content, string rootNamespace)
            {
                Path = path;
                Content = content;
                RootNamespace = rootNamespace;
            }

            public string Path { get; }

            public string Content { get; }

            public string RootNamespace { get; }
        }

        private sealed class DependencyEdge
        {
            public DependencyEdge(string from, string to)
            {
                From = from;
                To = to;
            }

            public string From { get; }

            public string To { get; }
        }
    }
}