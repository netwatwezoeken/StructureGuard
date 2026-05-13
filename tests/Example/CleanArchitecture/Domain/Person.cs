using CleanArchitecture.Infrastructure;

namespace CleanArchitecture.Domain;

public class Person
{
    private Status status; // Field type is from forbidden layer
    
    public Status PropertyStatus { get; set; } // Property type is from forbidden layer
    
    public Person(Status status) // use forbidden layer as parameter in constructor
    {
        this.status = status;
        var dal = new DbPerson();  // Construct class from forbidden layer
    }
    public Dictionary<string, Status> Statuses { get; set; } // Generic type referencing forbidden layer
    
    public Status GetStatus() => default; //Re turn type if from forbidden layer
    
    public void DoSomething(object obj)
    {
        var val = (Status)obj; // Cast to type from the forbidden layer
    }
    
    public void Expressions(object obj)
    {
        var type = typeof(ValueThing); // referencing type from forbidden layer
        if (obj is ValueThing) { } // referencing type from forbidden layer
        var x = obj as ValueThing;  // as expression to type from the forbidden layer
    }
}