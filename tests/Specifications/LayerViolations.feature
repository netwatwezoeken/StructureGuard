Feature: Detect and report layer violations

    Background: 
        Given an analyzer with root namespace Nwwz and the following allowed dependencies
          | From           | To     |
          | Infrastructure | Domain |

    Scenario: Inheritance from a forbidden layer is not allowed
        Given file Source.cs with code
            """cs
            using Nwwz.Infrastructure.Person;
            
            namespace Nwwz.Domain.Person
            {
                class PersonDetails : {|#0:DbPersonDetails|} { }
            }

            namespace Nwwz.Infrastructure.Person
            {
                class DbPersonDetails { }
            }
            """
        Then the following problems are found
          | Location | From   | To             |
          | 0        | Domain | Infrastructure |

    Scenario: Using a class from a forbidden layer is not allowed
        Given file Domain.cs with code
            """cs
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain;

            public class Person
            {
                public Person()
                {
                    var dal = new {|#0:DbPerson|}();
                }
            }
            """
        And file Infra.cs with code
            """cs
            namespace Nwwz.Infrastructure;

            public class DbPerson
            {
            }
            """
        Then the following problems are found
          | Location | From   | To             |
          | 0        | Domain | Infrastructure |

    Scenario: Using a fully qualified class from a forbidden layer is not allowed
        Given file Source.cs with code
            """cs
            namespace Nwwz.Domain.Person
            {
                class PersonDetails : {|#0:Nwwz.Infrastructure.Person.DbPersonDetails|} { }
            }

            namespace Nwwz.Infrastructure.Person
            {
                class DbPersonDetails { }
            }
            """
        Then the following problems are found
          | Location | From   | To             |
          | 0        | Domain | Infrastructure |

    Scenario: Using a extensionmethod from a forbidden layer is not allowed
        Given file Domain.cs with code
        """cs
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
        """
        And file Infrastructure.cs with code
        """cs
            using Nwwz.Domain;

            namespace Nwwz.Infrastructure;

            public static class DbPerson
            {
                public static Person PersonExtensionMethod(this Person person)
                {
                    return person;
                }
            }
        """

        Then the following problems are found
          | Location | From   | To             |
          | 0        | Domain | Infrastructure |

    Scenario: Using a type from a forbidden layer in a constructor is not allowed
        Given file Source.cs with code
        """cs
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain
            {
                public class Person
                {
                    public Person({|#0:Status|} status)
                    {
                        var state = status;
                    }
                }
            }

            namespace Nwwz.Infrastructure
            {
                public enum Status
                {
                    Ok,
                    Error
                }
            }
        """
        Then the following problems are found
          | Location | From   | To             |
          | 0        | Domain | Infrastructure |

    Scenario: Using a type from a forbidden layer in a primary constructor is not allowed
        Given file Source.cs with code
        """cs
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain
            {
                public class Person({|#0:Status|} status)
                {
                }
            }

            namespace Nwwz.Infrastructure
            {
                public enum Status
                {
                    Ok,
                    Error
                }
            }
        """
        Then the following problems are found
          | Location | From   | To             |
          | 0        | Domain | Infrastructure |

    Scenario: Using a type from a forbidden layer is not allowed
        Given file Source.cs with code
        """cs
            using Nwwz.Infrastructure;

            namespace Nwwz.Domain
            {
                public class Person
                {
                    {|#0:Status|} status;
                }
            }

            namespace Nwwz.Infrastructure
            {
                public enum Status
                {
                    Ok,
                    Error
                }
            }
        """
        Then the following problems are found
          | Location | From   | To             |
          | 0        | Domain | Infrastructure |

    Scenario: Using a attribute from a forbidden layer is not allowed
        Given file Source.cs with code
        """cs
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
        """
        Then the following problems are found
          | Location | From   | To             |
          | 0        | Domain | Infrastructure |