using Microsoft.CodeAnalysis.Testing;
using StructureGuard;

namespace UnitTests;

public class LayerViolationDetection
{
    private readonly CustomCSharpAnalyzerTest<CustomVerifier> _cSharpAnalyzerTest;

    public LayerViolationDetection()
    {
        _cSharpAnalyzerTest = new CustomCSharpAnalyzerTest<CustomVerifier>(
            "Nwwz",
            new List<Dependency>()
            {
                new Dependency(new Layer("Infrastructure"), new Layer("Domain"))
            })
        {
            ReferenceAssemblies = ReferenceAssemblies.Net.Net90
        };

        _cSharpAnalyzerTest.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", "name = value"));
    }
    
    [Fact]
    public async Task Contructing_a_class_from_a_forbidden_layer_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain;

            public class Person
            {
                public Person()
                {
                    {|#1:var|} dal = new {|#0:DbPerson|}();
                }
            }
            """));
        
        _cSharpAnalyzerTest.TestState.Sources.Add(("DbPersonDetails.cs", /*lang=csharp*/
            """
            namespace Nwwz.Infrastructure;

            public class DbPerson
            {
            }
            """));
        
        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Contructing_a_class_from_a_permitted_layer_is_allowed()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            namespace Nwwz.Domain;

            public class Person
            {
            }
            """));
        
        _cSharpAnalyzerTest.TestState.Sources.Add(("DbPersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Domain;

            namespace Nwwz.Infrastructure;

            public class DbPerson
            {
                public DbPerson()
                {
                    var person = {|#0:new Person()|};
                }
            }
            """));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Full_class_name_derivative_is_detected()
    {
        _cSharpAnalyzerTest.TestCode = /*lang=csharp*/
            """
            namespace Nwwz.Domain.Person
            {
                class PersonDetails : {|#0:Nwwz.Infra.Person.DbPersonDetails|} { }
            }

            namespace Nwwz.Infra.Person
            {
                class DbPersonDetails { }
            }
            """;
        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infra"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Class_derivative_from_forbidden_layer_is_detected()
    {
        _cSharpAnalyzerTest.TestCode = /*lang=csharp*/
            """
            using Nwwz.Infrastructure.Person;
            
            namespace Nwwz.Domain.Person
            {
                class PersonDetails : {|#0:DbPersonDetails|} { }
            }

            namespace Nwwz.Infrastructure.Person
            {
                class DbPersonDetails { }
            }
            """;
        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Using_a_extension_method_from_a_forbidden_layer_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain;

            public class Person
            {
            }

            public class PersonStore()
            {
                public void GetPerson()
                {
                    var person = {|#0:new Person().PersonExtensionMethod()|};
                }
            }
            """));
        
        _cSharpAnalyzerTest.TestState.Sources.Add(("DbPersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Domain;

            namespace Nwwz.Infrastructure;

            public static class DbPerson
            {
                public static Person PersonExtensionMethod(this Person person)
                {
                    return person;
                }
            }
            """));
        
        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Using_a_extension_method_from_a_permitted_layer_is_allowed()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            namespace Nwwz.Domain;

            public class Person
            {
            }

            public static class PersonStore
            {
                public static Person PersonExtensionMethod(this Person person)
                {
                    return person;
                }
            }
            """));
        
        _cSharpAnalyzerTest.TestState.Sources.Add(("DbPersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Domain;

            namespace Nwwz.Infrastructure;

            public class DbPerson
            {
                public void GetPerson()
                {
                    var person = new Person().PersonExtensionMethod();
                }
            }
            """));
        
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Using_a_type_from_a_forbidden_layer_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain;

            public class Person
            {
                {|#0:Status|} status;
            }
            """));
        
        _cSharpAnalyzerTest.TestState.Sources.Add(("DbPersonDetails.cs", /*lang=csharp*/
            """
            namespace Nwwz.Infrastructure;

            public enum Status
            {
                Ok,
                Error
            }
            """));
        
        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Using_a_type_from_a_forbidden_layer_in_a_constructor_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain;

            public class Person
            {
                public Person({|#0:Status|} status)
                {
                    var state = status;
                }
            }
            """));
        
        _cSharpAnalyzerTest.TestState.Sources.Add(("DbPersonDetails.cs", /*lang=csharp*/
            """
            namespace Nwwz.Infrastructure;

            public enum Status
            {
                Ok,
                Error
            }
            """));
        
        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Using_a_type_from_a_forbidden_layer_in_primary_constructor_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;
            using System;
            
            namespace Nwwz.Domain
            {
                [{|#0:MyCustom|}]
                public class Person
                {
                }
            }
            
            namespace Nwwz.Infrastructure
            {
                public class MyCustomAttribute : Attribute
                {
                }
            }
            """));
        
        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Using_a_attribute_from_a_forbidden_layer_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain
            {
                public class Person({|#0:Status|} status)
                {
                }
            }
            """));
        _cSharpAnalyzerTest.TestState.Sources.Add(("Status.cs", /*lang=csharp*/
            """
            namespace Nwwz.Infrastructure
            {
                public enum Status
                {
                    Ok,
                    Error
                }
            }
            """));
        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Cast_expression_to_type_from_forbidden_layer_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain;

            public class Person
            {
                public void DoSomething(object obj)
                {
                    var val = ({|#0:Status|})obj;
                }
            }
            """));

        _cSharpAnalyzerTest.TestState.Sources.Add(("DbPersonDetails.cs", /*lang=csharp*/
            """
            namespace Nwwz.Infrastructure;

            public class Status { }
            """));

        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Local_variable_type_from_forbidden_layer_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain;

            public class Person
            {
                public void DoSomething()
                {
                    {|#0:Status|} localStatus;
                }
            }
            """));

        _cSharpAnalyzerTest.TestState.Sources.Add(("DbPersonDetails.cs", /*lang=csharp*/
            """
            namespace Nwwz.Infrastructure;

            public enum Status { Ok, Error }
            """));

        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Property_type_from_forbidden_layer_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain;

            public class Person
            {
                public {|#0:Status|} PropertyStatus { get; set; }
            }
            """));

        _cSharpAnalyzerTest.TestState.Sources.Add(("DbPersonDetails.cs", /*lang=csharp*/
            """
            namespace Nwwz.Infrastructure;

            public enum Status { Ok, Error }
            """));

        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Method_return_type_from_forbidden_layer_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain;

            public class Person
            {
                public {|#0:Status|} GetStatus() => default;
            }
            """));

        _cSharpAnalyzerTest.TestState.Sources.Add(("DbStatus.cs", /*lang=csharp*/
            """
            namespace Nwwz.Infrastructure;

            public enum Status { Ok, Error }
            """));

        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task As_expression_to_type_from_forbidden_layer_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain;

            public class Person
            {
                public void Do(object obj)
                {
                    var x = obj as {|#0:Status|};
                }
            }
            """));

        _cSharpAnalyzerTest.TestState.Sources.Add(("DbStatus.cs", /*lang=csharp*/
            """
            namespace Nwwz.Infrastructure;

            public class Status { }
            """));

        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Is_expression_to_type_from_forbidden_layer_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain;

            public class Person
            {
                public void Do(object obj)
                {
                    if (obj is {|#0:Status|}) { }
                }
            }
            """));

        _cSharpAnalyzerTest.TestState.Sources.Add(("DbStatus.cs", /*lang=csharp*/
            """
            namespace Nwwz.Infrastructure;

            public class Status { }
            """));

        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Typeof_expression_with_type_from_forbidden_layer_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain;

            public class Person
            {
                public void Do()
                {
                    var type = typeof({|#0:Status|});
                }
            }
            """));

        _cSharpAnalyzerTest.TestState.Sources.Add(("DbStatus.cs", /*lang=csharp*/
            """
            namespace Nwwz.Infrastructure;

            public class Status { }
            """));

        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Static_member_access_from_forbidden_layer_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain;

            public class Person
            {
                public void Do()
                {
                    var x = {|#0:Constants.Value|};
                }
            }
            """));

        _cSharpAnalyzerTest.TestState.Sources.Add(("Constants.cs", /*lang=csharp*/
            """
            namespace Nwwz.Infrastructure;

            public static class Constants { public const int Value = 1; }
            """));

        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
    
    [Fact]
    public async Task Generic_type_argument_from_forbidden_layer_is_detected()
    {
        _cSharpAnalyzerTest.TestState.Sources.Add(("PersonDetails.cs", /*lang=csharp*/
            """
            using Nwwz.Infrastructure;
            using System.Collections.Generic;

            namespace Nwwz.Domain;

            public class Person
            {
                public Dictionary<string, {|#0:Status|}> Statuses { get; set; }
            }
            """));

        _cSharpAnalyzerTest.TestState.Sources.Add(("DbStatus.cs", /*lang=csharp*/
            """
            namespace Nwwz.Infrastructure;

            public class Status { }
            """));

        _cSharpAnalyzerTest.ExpectedDiagnostics.Add(new DiagnosticResult(SliceAnalyzer.LayerDependencyRule)
            .WithLocation(0)
            .WithArguments("Domain", "Infrastructure"));
        await _cSharpAnalyzerTest.RunAsync();
    }
}