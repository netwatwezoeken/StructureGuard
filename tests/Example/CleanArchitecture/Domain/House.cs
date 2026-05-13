using CleanArchitecture.Infrastructure;

namespace CleanArchitecture.Domain;

public class House(Status status) // use forbidden layer as parameter in primary constructor
{
    public void Method()
    {
        var temp = this.ExtensionMethod(); //extension method defined in forbidden layer
    }
}