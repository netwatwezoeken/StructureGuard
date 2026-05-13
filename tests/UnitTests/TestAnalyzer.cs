using Microsoft.CodeAnalysis.Diagnostics;
using StructureGuard;

namespace UnitTests;

#pragma warning disable RS1001
public class TestAnalyzer(string rootNamespace, IList<Dependency> allowedDependencies) : SliceAnalyzer
#pragma warning restore RS1001
{
    protected override void OnInitialize(AnalysisContext context)
    {
        RootNameSpace = rootNamespace;
        PermittedDependencies = allowedDependencies;
        base.OnInitialize(context);
    }
}