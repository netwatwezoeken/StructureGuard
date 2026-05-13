using Reqnroll;
using StructureGuard;
using Xunit;
using static System.String;

namespace Specifications;

[Binding]
public class NamespaceSteps
{
    private string _namespaceString = null!;
    private CodePart? _codePart;
    private string _rootnamespace = Empty;

    [Given(@"namespace (.*)")]
    public void Given(string namespaceString)
    {
        _namespaceString = namespaceString;
    }

    [When(@"parsed")]
    public void WhenParsed()
    {
        _codePart = NamespaceMatcher.ToCodePart(_rootnamespace,_namespaceString);
    }

    [Then(@"layer is (.*)")]
    public void ThenLayerIs(string layer)
    {
        if (IsNullOrEmpty(layer))
        {
            Assert.Null(_codePart!.Layer);
        }
        else
        {
            Assert.Equal(layer, _codePart!.Layer);
        }
    }

    [Given(@"rootnamespace is (.*)")]
    public void GivenRootnaamespaceIs(string rootnamespace)
    {
        _rootnamespace = rootnamespace;
    }

    [Then(@"slice is (.*)")]
    public void ThenSliceIs(string slice)
    {
        if (IsNullOrEmpty(slice))
        {
            Assert.Null(_codePart!.Feature);
        }
        else
        {
            Assert.Equal(slice, _codePart!.Feature);
        }
    }

    [Then(@"root is (.*)")]
    public void ThenRootIs(string root)
    {
        if (IsNullOrEmpty(root))
        {
            Assert.Null(_codePart!.Root);
        }
        else
        {
            Assert.Equal(root, _codePart!.Root);
        }
    }

    [Then("msut be analyzed is (.*)")]
    public void ThenMsutBeAnalyzedIsTrue(bool toBeAnalyzed)
    {
        Assert.Equal(toBeAnalyzed, _codePart!.TobeAnalyzed);
    }
}