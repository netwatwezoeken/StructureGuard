using Microsoft.CodeAnalysis.Testing;
using Reqnroll;
using StructureGuard;

namespace Specifications;

[Binding]
public class LayerViolationSteps
{
    private CustomContext<DefaultVerifier> _context = null!;

    [Given(@"an analyzer with root namespace (.*) and the following allowed dependencies")]
    public void GivenAnAnalyzerWithRootNamespaceNwwzAndTheFollowingAllowedDependencies(
        string rootNamespace, DataTable table)
    {
        var allowedDependencies = table.CreateSet<DependencyRow>().ToList();
        _context = new CustomContext<DefaultVerifier>(rootNamespace, allowedDependencies
            .Select(row => new Dependency(
                new Layer(row.From), new Layer(row.To)
            )).ToList())
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90
        };
    }

    [Given(@"file (.*) with code")]
    public void GivenSourceCode(string filename, string source)
    {
        _context.TestState.Sources.Add((filename, source));
    }

    [Then(@"problems are found")]
    public async Task ThenProblemsAreFound()
    {
        _context.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _context.RunAsync();
    }
    
    [Then(@"the following problems are found")]
    public async Task ThenTheFollowingProblemsAreFound(DataTable table)
    {
        var expectedProblems = table.CreateSet<ProblemRow>().ToList();
        foreach (var problem in expectedProblems.OfType<ProblemRow>())
        {
            _context.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
                .WithLocation(problem.Location)
                .WithArguments(problem.From, problem.To));
        }
        await _context.RunAsync();
    }
}

public record DependencyRow(
    string From,
    string To);

public record ProblemRow(
    int Location,
    string From,
    string To);