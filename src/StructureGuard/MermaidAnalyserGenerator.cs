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
            IncrementalValuesProvider<MermaidFile> mermaidFiles =
                context.AdditionalTextsProvider
                    .Where(file =>
                        file.Path.EndsWith(".mmd", StringComparison.OrdinalIgnoreCase) ||
                        file.Path.EndsWith(".mermaid", StringComparison.OrdinalIgnoreCase) ||
                        file.Path.EndsWith(".md", StringComparison.OrdinalIgnoreCase))
                    .SelectMany((file, cancellationToken) =>
                    {
                        SourceText sourceText = file.GetText(cancellationToken);

                        if (sourceText == null)
                        {
                            return Array.Empty<MermaidFile>();
                        }

                        var content = sourceText.ToString();
                        var extension = System.IO.Path.GetExtension(file.Path);

                        if (extension.Equals(".md", StringComparison.OrdinalIgnoreCase))
                        {
                            return ExtractMermaidFromMarkdown(file.Path, content);
                        }
                        else
                        {
                            var rootNamespace = ParseNamespaceFromYaml(content);
                            return new[] { new MermaidFile(file.Path, content, rootNamespace) };
                        }
                    })
                    .Where(file => file != null);

            context.RegisterSourceOutput(mermaidFiles, (sourceProductionContext, mermaidFile) =>
            {
                string fileName = System.IO.Path.GetFileNameWithoutExtension(mermaidFile.Path);
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
                    analyzerClassName: fileName + "Analyzer",
                    dependencies: dependencies);
            
                sourceProductionContext.AddSource(
                    $"{fileName}Analyzer.g.cs",
                    SourceText.From(generatedSource, Encoding.UTF8));
            });
        }

        private static IEnumerable<MermaidFile> ExtractMermaidFromMarkdown(string filePath, string content)
        {
            var mermaidBlocks = new List<MermaidFile>();
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var inMermaidBlock = false;
            var currentBlock = new StringBuilder();

            foreach (var line in lines)
            {
                if (line.Trim().StartsWith("```mermaid"))
                {
                    inMermaidBlock = true;
                    currentBlock.Clear();
                    continue;
                }

                if (inMermaidBlock && line.Trim().StartsWith("```"))
                {
                    inMermaidBlock = false;
                    var blockContent = currentBlock.ToString();
                    if (IsTaggedWithStructureGuard(blockContent))
                    {
                        var rootNamespace = ParseNamespaceFromYaml(blockContent);
                        mermaidBlocks.Add(new MermaidFile(filePath, blockContent, rootNamespace));
                    }
                    continue;
                }

                if (inMermaidBlock)
                {
                    currentBlock.AppendLine(line);
                }
            }

            return mermaidBlocks;
        }

        private static bool IsTaggedWithStructureGuard(string content)
        {
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var inYaml = false;
            var inTags = false;

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
                    if (line.StartsWith("tags:", StringComparison.OrdinalIgnoreCase))
                    {
                        // Check if it's tags: [StructureGuard]
                        if (line.Contains("StructureGuard"))
                        {
                            return true;
                        }
                        inTags = true;
                        continue;
                    }

                    if (inTags)
                    {
                        // If we hit another key, we are no longer in tags
                        if (line.Contains(":") && !line.TrimStart().StartsWith("-"))
                        {
                            inTags = false;
                        }
                        else if (line.TrimStart().StartsWith("-") && line.Contains("StructureGuard"))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private static string ParseNamespaceFromYaml(string content)
        {
            var lines = content.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            var inYaml = false;
            foreach (var rawLine in lines)
            {
                string line = rawLine.Trim();
                if (line == "---")
                {
                    inYaml = !inYaml;
                    continue;
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
            List<DependencyEdge> dependencies = [];

            var lines = mermaid.Split([ "\r\n", "\n" ], StringSplitOptions.None);

            foreach (var rawLine in lines)
            {
                var line = rawLine.Trim();

                if (line.Length == 0)
                {
                    continue;
                }

                if (line.StartsWith("graph ", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var arrowIndex = line.IndexOf("-->", StringComparison.Ordinal);

                if (arrowIndex < 0)
                {
                    continue;
                }

                var from = CleanNodeName(line.Substring(0, arrowIndex));
                var to = CleanNodeName(line.Substring(arrowIndex + 3));

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
            var builder = new StringBuilder();

            builder.AppendLine("// <auto-generated />");
            builder.AppendLine("using System.Collections.Generic;");
            builder.AppendLine("using Microsoft.CodeAnalysis;");
            builder.AppendLine("using Microsoft.CodeAnalysis.Diagnostics;");
            builder.AppendLine("using StructureGuard;");
            builder.AppendLine();
            builder.AppendLine("namespace " + analyzerNamespace);
            builder.AppendLine("{");
            builder.AppendLine("    [DiagnosticAnalyzer(LanguageNames.CSharp)]");
            builder.AppendLine("    public class " + analyzerClassName + " : " + nameof(SliceAnalyzer));
            builder.AppendLine("    {");
            builder.AppendLine("        protected override void OnInitialize(AnalysisContext context)");
            builder.AppendLine("        {");
            builder.AppendLine("            RootNameSpace = \"" + rootNamespace + "\";");
            builder.AppendLine("            PermittedDependencies = new List<Dependency>()");
            builder.AppendLine("            {");

            for (var i = 0; i < dependencies.Count; i++)
            {
                var dependency = dependencies[i];

                builder.Append("                new Dependency(new Layer(\"");
                builder.Append(dependency.From);
                builder.Append("\"), new Layer(\"");
                builder.Append(dependency.To);
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
                RootNamespace = EscapeString(rootNamespace);
            }

            public string Path { get; }

            public string Content { get; }

            public string RootNamespace { get; }
        }
        
        private sealed class DependencyEdge
        {
            public DependencyEdge(string from, string to)
            {
                From = EscapeString(from);
                To = EscapeString(to);
            }

            public string From { get; }

            public string To { get; }
        }
    }
}