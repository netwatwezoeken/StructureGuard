using CleanArchitecture.Domain;

namespace CleanArchitecture.Infrastructure;

public static class DbHouse
{
    public static House ExtensionMethod(this House house)
    {
        return house;
    }
}