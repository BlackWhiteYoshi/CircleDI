﻿using CircleDI.Defenitions;
using CircleDI.Generation;
using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Tests;

/// <summary>
/// Tests the function <see cref="ServiceProvider.CreateDependencyTree()"/>.
/// </summary>
public static class DependencyTreeTests {
    private static ServiceProvider CreateProvider(List<Service> serviceList) {
        ServiceProvider serviceProvider = new(null!) {
            Identifier = new TypeName("TestProvider", TypeKeyword.Class, [], [], [], []),
            IdentifierScope = new TypeName("TestProvider.Scope", TypeKeyword.Class, [], [], [], []),
            InterfaceIdentifier = new TypeName("ITestProvider", TypeKeyword.Interface, [], [], [], []),
            InterfaceIdentifierScope = new TypeName("ITestProvider.IScope", TypeKeyword.Interface, [], [], [], [])
        };

        foreach (Service service in serviceList)
            if (service.Lifetime is ServiceLifetime.Singleton)
                serviceProvider.SingletonList.Add(service);
        foreach (Service service in serviceList)
            if (service.Lifetime is ServiceLifetime.Scoped)
                serviceProvider.SingletonList.Add(service);
        foreach (Service service in serviceList)
            if (service.Lifetime is ServiceLifetime.Transient or ServiceLifetime.TransientScoped or ServiceLifetime.TransientSingleton)
                serviceProvider.SingletonList.Add(service);
        foreach (Service service in serviceList)
            if (service.Lifetime is ServiceLifetime.Delegate or ServiceLifetime.DelegateScoped)
                serviceProvider.SingletonList.Add(service);

        return serviceProvider;
    }

    private static void SetDefaultDependenciesIterator(Service service) => SetDependencies(service, GetDependenciesDefaultIterator(service));

    [System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "get_DependenciesDefaultIterator")]
    private extern static IEnumerable<Dependency> GetDependenciesDefaultIterator(Service instance);

    [System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "set_Dependencies")]
    private extern static void SetDependencies(Service instance, IEnumerable<Dependency> value);



    [Fact]
    public static void EmptyDoesNothing() {
        ServiceProvider serviceProvider = CreateProvider([]);

        serviceProvider.CreateDependencyTree();

        Assert.Equal([], serviceProvider.SingletonList);
        Assert.Equal([], serviceProvider.ScopedList);
        Assert.Equal([], serviceProvider.TransientList);
    }

    [Fact]
    public static void MultipleServicesWithSameTypeWithoutNamingFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService1>]
            [Singleton<TestService2>(Name = "TestService2_1")]
            [Singleton<TestService2>(Name = "TestService2_2")]
            public sealed partial class TestProvider;

            public sealed class TestService1(TestService2 testService2);
            public sealed class TestService2;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI031", diagnostics[0].Id);
        Assert.Equal("Ambiguous dependency at Service 'TestService1' with type 'MyCode.TestService2': There are multiple Services registered for this type: [\"TestService2_1\", \"TestService2_2\"]. Use the '[Dependency(Name=\"...\")]'-attribute on the parameter to choose one specific service", diagnostics[0].GetMessage());
    }


    #region Tree

    [Fact]
    public static void Tree_SimplePath_Constructor() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service3"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        Service service3 = new() {
            Name = "service3",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service3"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service3);

        ServiceProvider serviceProvider = CreateProvider([service1, service2, service3]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
        Assert.Same(service2.ConstructorDependencyList[0].Service, service3);
    }

    [Fact]
    public static void Tree_SimplePath_Property() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [
                new PropertyDependency() {
                    Name = "Service2",
                    ServiceType = new TypeName("Service2"),
                    IsRequired = true,
                    IsInit = true,
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ImplementationBaseName = null!
                }
            ],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [
                new PropertyDependency() {
                    Name = "Service3",
                    ServiceType = new TypeName("Service3"),
                    IsRequired = true,
                    IsInit = true,
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ImplementationBaseName = null!
                }
            ],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        Service service3 = new() {
            Name = "service3",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service3"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service3);

        ServiceProvider serviceProvider = CreateProvider([service1, service2, service3]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.PropertyDependencyList[0].Service, service2);
        Assert.Same(service2.PropertyDependencyList[0].Service, service3);
        Assert.False(service1.PropertyDependencyList[0].IsCircular);
        Assert.False(service2.PropertyDependencyList[0].IsCircular);
    }

    [Fact]
    public static void Tree_NormalTreePath() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [
                new PropertyDependency() {
                    Name = "Service2",
                    ServiceType = new TypeName("Service2"),
                    IsRequired = true,
                    IsInit = true,
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ImplementationBaseName = null!
                },
                new PropertyDependency() {
                    Name = "Service3",
                    ServiceType = new TypeName("Service3"),
                    IsRequired = true,
                    IsInit = true,
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ImplementationBaseName = null!
                }
            ],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service4"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                },
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service5"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        Service service3 = new() {
            Name = "service3",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service3"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [
                new PropertyDependency() {
                    Name = "Service6",
                    ServiceType = new TypeName("Service6"),
                    IsRequired = true,
                    IsInit = true,
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ImplementationBaseName = null!
                },
                new PropertyDependency() {
                    Name = "Service7",
                    ServiceType = new TypeName("Service7"),
                    IsRequired = true,
                    IsInit = true,
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ImplementationBaseName = null!
                }
            ],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service3);

        Service service4 = new() {
            Name = "service4",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service4"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service4);

        Service service5 = new() {
            Name = "service5",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service5"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service5);

        Service service6 = new() {
            Name = "service6",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service6"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service6);

        Service service7 = new() {
            Name = "service7",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service7"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service7);

        ServiceProvider serviceProvider = CreateProvider([service1, service2, service3, service4, service5, service6, service7]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.PropertyDependencyList[0].Service, service2);
        Assert.Same(service1.PropertyDependencyList[1].Service, service3);

        Assert.Same(service2.ConstructorDependencyList[0].Service, service4);
        Assert.Same(service2.ConstructorDependencyList[1].Service, service5);

        Assert.Same(service3.PropertyDependencyList[0].Service, service6);
        Assert.Same(service3.PropertyDependencyList[1].Service, service7);
    }

    [Fact]
    public static void Tree_DoublePath() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                },
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
        Assert.Same(service1.ConstructorDependencyList[1].Service, service2);
    }

    [Fact]
    public static void Tree_MergingPath() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                },
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service3"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service3"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        Service service3 = new() {
            Name = "service3",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service3"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service3);

        ServiceProvider serviceProvider = CreateProvider([service1, service2, service3]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
        Assert.Same(service1.ConstructorDependencyList[1].Service, service3);
        Assert.Same(service2.ConstructorDependencyList[0].Service, service3);
    }

    [Fact]
    public static void Tree_DiamondMerging() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                },
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service3"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service4"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        Service service3 = new() {
            Name = "service3",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service3"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service4"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service3);

        Service service4 = new() {
            Name = "service4",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service4"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service4);

        ServiceProvider serviceProvider = CreateProvider([service1, service2, service3, service4]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
        Assert.Same(service1.ConstructorDependencyList[1].Service, service3);
        Assert.Same(service2.ConstructorDependencyList[0].Service, service4);
        Assert.Same(service3.ConstructorDependencyList[0].Service, service4);
    }

    [Fact]
    public static void Tree_MissingDependencyFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService1>]
            public sealed partial class TestProvider;

            public sealed class TestService1(TestService2 testService2);

            public sealed class TestService2;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI029", diagnostics[0].Id);
        Assert.Equal("Unregistered dependency at Service 'TestService1' with type 'MyCode.TestService2'", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void Tree_NotDeclaredInterfaceDependencyFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope([Dependency] ITestProvider serviceProvider) {
                        InitServices(serviceProvider);
                    }
                }
            }

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Equal(2, diagnostics.Length);
        Assert.Equal("CDI038", diagnostics[0].Id);
        Assert.Equal("CDI030", diagnostics[1].Id);
        Assert.Equal("Unregistered dependency 'ITestProvider' has the same identifier as generated interface type 'MyCode.ITestProvider', only missing the namespace. If you mean this generated type, you can correct the namespace by just declaring the interface type in namespace 'MyCode': \"public partial interface ITestProvider;\"", diagnostics[1].GetMessage());
    }

    [Fact]
    public static void Tree_MissingNamedDependencyFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService1>]
            public sealed partial class TestProvider;

            public sealed class TestService1([Dependency(Name = "Asdf")]TestService2 testService2);

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI032", diagnostics[0].Id);
        Assert.Equal("Unregistered named dependency at Service 'TestService1' with name \"Asdf\"", diagnostics[0].GetMessage());
    }

    #endregion


    #region MultipleRoots

    [Fact]
    public static void MultipleRoots_MergingRoots() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service3"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service3"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        Service service3 = new() {
            Name = "service3",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service3"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service3);

        ServiceProvider serviceProvider = CreateProvider([service1, service2, service3]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service3);
        Assert.Same(service2.ConstructorDependencyList[0].Service, service3);
    }

    [Fact]
    public static void MultipleRoots_MergingPath() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service4"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service3"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        Service service3 = new() {
            Name = "service3",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service3"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service4"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service3);

        Service service4 = new() {
            Name = "service4",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service4"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service4);

        ServiceProvider serviceProvider = CreateProvider([service1, service2, service3, service4]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service4);
        Assert.Same(service2.ConstructorDependencyList[0].Service, service3);
        Assert.Same(service3.ConstructorDependencyList[0].Service, service4);
    }

    #endregion


    #region MultipleTrees

    [Fact]
    public static void MultipleTrees_IndependServices() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.NotSame(service1, service2);
    }

    [Fact]
    public static void MultipleTrees_IndependentPath() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service4"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service3"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        Service service3 = new() {
            Name = "service3",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service3"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service4"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service3);

        Service service4 = new() {
            Name = "service4",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service4"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service4);

        Service service5 = new() {
            Name = "service5",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service5"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [
                new PropertyDependency() {
                    Name = "Service6",
                    ServiceType = new TypeName("Service6"),
                    IsRequired = true,
                    IsInit = true,
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ImplementationBaseName = null!
                }
            ],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service5);

        Service service6 = new() {
            Name = "service6",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service6"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service6);

        ServiceProvider serviceProvider = CreateProvider([service1, service2, service3, service4, service5, service6]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service4);
        Assert.Same(service2.ConstructorDependencyList[0].Service, service3);
        Assert.Same(service3.ConstructorDependencyList[0].Service, service4);
        Assert.Same(service5.PropertyDependencyList[0].Service, service6);
    }

    #endregion


    #region Circles

    [Fact]
    public static void Circles_SimpleCircle() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [
                new PropertyDependency() {
                    Name = "Service2",
                    ServiceType = new TypeName("Service2"),
                    IsRequired = true,
                    IsInit = true,
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ImplementationBaseName = null!
                }
            ],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [
                new PropertyDependency() {
                    Name = "Service1",
                    ServiceType = new TypeName("Service1"),
                    IsRequired = true,
                    IsInit = true,
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ImplementationBaseName = null!
                }
            ],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.PropertyDependencyList[0].Service, service2);
        Assert.Same(service2.PropertyDependencyList[0].Service, service1);
        Assert.True(service1.PropertyDependencyList[0].IsCircular ^ service2.PropertyDependencyList[0].IsCircular);
    }

    [Fact]
    public static void Circles_SelfCircle() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [
                new PropertyDependency() {
                    Name = "Service1",
                    ServiceType = new TypeName("Service1"),
                    IsRequired = true,
                    IsInit = true,
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ImplementationBaseName = null!
                }
            ],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        ServiceProvider serviceProvider = CreateProvider([service1]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.PropertyDependencyList[0].Service, service1);
        Assert.True(service1.PropertyDependencyList[0].IsCircular);
    }

    [Fact]
    public static void Circles_ConstructorPropertyCircle() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None,
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [
                new PropertyDependency() {
                    Name = "Service1",
                    ServiceType = new TypeName("Service1"),
                    IsRequired = true,
                    IsInit = true,
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ImplementationBaseName = null!
                }
            ],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
        Assert.Same(service2.PropertyDependencyList[0].Service, service1);
        Assert.True(service2.PropertyDependencyList[0].IsCircular);
    }

    [Fact]
    public static void Circles_InvalidCircle() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService1>]
            public sealed partial class TestProvider;

            public sealed class TestService1(TestService1 testService1);

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI033", diagnostics[0].Id);
        Assert.Equal("Circular dependency unresolvable: ['TestService1' -> 'TestService1']. Only singleton and scoped dependencies injected as properties can be resolved circular", diagnostics[0].GetMessage());
    }

    #endregion


    #region Lifetime

    [Fact]
    public static void Lifetime_SingletonToScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService1>]
            [Scoped<TestService2>]
            public sealed partial class TestProvider;

            public sealed class TestService1(TestService2 testService2);
            public sealed class TestService2;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI034", diagnostics[0].Id);
        Assert.Equal("Lifetime Violation: Singleton Service 'TestService1' has Scoped dependency 'MyCode.TestService2'", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void Lifetime_SingletonToTransient() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Transient,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
        Assert.Equal(ServiceLifetime.Transient, service2.Lifetime);
    }

    [Fact]
    public static void Lifetime_SingletonToTransientScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService1>]
            [Transient<TestService2>]
            [Scoped<TestService3>]
            public sealed partial class TestProvider;

            public sealed class TestService1(TestService2 testService2);
            public sealed class TestService2(TestService3 testService3);
            public sealed class TestService3;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI035", diagnostics[0].Id);
        Assert.Equal("Lifetime Violation: Singleton Service 'TestService1' has Transient-Scoped dependency 'MyCode.TestService2'. \"Transient-Scoped\" means the service itself is transient, but it has at least one dependency or one dependency of the dependencies that is Scoped", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void Lifetime_SingletonToDelegate() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Delegate,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
    }

    [Fact]
    public static void Lifetime_SingletonToDelegateScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService1>]
            public sealed partial class TestProvider {

                [Delegate<System.Action>(nameof(ScopedMethod))]
                public sealed partial class Scope {
                    private static void ScopedMethod() { }
                }
            }

            public sealed class TestService1(System.Action scopedMethod);

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI036", diagnostics[0].Id);
        Assert.Equal("Lifetime Violation: Singleton Service 'TestService1' has Delegate-Scoped dependency 'System.Action'. \"Delegate-Scoped\" means the method is declared inside Scope and therefore only available for scoped services.", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void Lifetime_SingletonToMutlipleScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService>]
            [Scoped<ITestDependency, TestDependency1>]
            [Scoped<ITestDependency, TestDependency2>]
            public sealed partial class TestProvider;

            public sealed class TestService(ITestDependency testDependency);

            public interface ITestDependency;
            public sealed class TestDependency1 : ITestDependency;
            public sealed class TestDependency2 : ITestDependency;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI037", diagnostics[0].Id);
        Assert.Equal("Lifetime Violation: Singleton Service 'TestService' has dependency with type 'MyCode.ITestDependency' and there are multiple services of that type, but they are all invalid (Scoped or Transient-Scoped): [\"TestDependency1\", \"TestDependency2\"]", diagnostics[0].GetMessage());
    }


    [Fact]
    public static void Lifetime_ScopedToSingleton() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Scoped,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
    }

    [Fact]
    public static void Lifetime_ScopedToTransient() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Scoped,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Transient,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
        Assert.Equal(ServiceLifetime.Transient, service2.Lifetime);
    }

    [Fact]
    public static void Lifetime_ScopedToDelegate() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Scoped,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Delegate,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
    }


    [Fact]
    public static void Lifetime_TransientToSingleton() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Transient,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
        Assert.Equal(ServiceLifetime.Transient, service1.Lifetime);
    }

    [Fact]
    public static void Lifetime_TransientToScoped() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Transient,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Scoped,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
        Assert.Equal(ServiceLifetime.TransientScoped, service1.Lifetime);
    }

    [Fact]
    public static void Lifetime_TransientToDelegate() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Transient,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Delegate,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
    }

    [Fact]
    public static void Lifetime_TransientToDelegateScoped() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Transient,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.DelegateScoped,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
        Assert.Equal(ServiceLifetime.TransientScoped, service1.Lifetime);
    }

    #endregion


    #region CreationTiming

    [Fact]
    public static void CreationTiming_ConstructorToConstructor() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            CreationTime = CreationTiming.Constructor,
            CreationTimeTransitive = CreationTiming.Constructor,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            CreationTime = CreationTiming.Constructor,
            CreationTimeTransitive = CreationTiming.Constructor,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
    }

    [Fact]
    public static void CreationTiming_ConstructorToLazy() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            CreationTime = CreationTiming.Constructor,
            CreationTimeTransitive = CreationTiming.Constructor,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            CreationTime = CreationTiming.Lazy,
            CreationTimeTransitive = CreationTiming.Lazy,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
        Assert.Equal(CreationTiming.Constructor, service2.CreationTimeTransitive);
    }

    [Fact]
    public static void CreationTiming_LazyToConstructor() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            CreationTime = CreationTiming.Lazy,
            CreationTimeTransitive = CreationTiming.Lazy,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            CreationTime = CreationTiming.Constructor,
            CreationTimeTransitive = CreationTiming.Constructor,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
    }

    [Fact]
    public static void CreationTiming_LazyToLazy() {
        Service service1 = new() {
            Name = "service1",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service1"),
            ImplementationType = null!,

            CreationTime = CreationTiming.Lazy,
            CreationTimeTransitive = CreationTiming.Lazy,

            ConstructorDependencyList = [
                new ConstructorDependency() {
                    Name = string.Empty,
                    ServiceType = new TypeName("Service2"),
                    ServiceName = string.Empty,
                    HasAttribute = false,
                    ByRef = RefKind.None
                }
            ],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service1);

        Service service2 = new() {
            Name = "service2",
            Lifetime = ServiceLifetime.Singleton,
            ServiceType = new TypeName("Service2"),
            ImplementationType = null!,

            CreationTime = CreationTiming.Lazy,
            CreationTimeTransitive = CreationTiming.Lazy,

            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = null!
        };
        SetDefaultDependenciesIterator(service2);

        ServiceProvider serviceProvider = CreateProvider([service1, service2]);


        serviceProvider.CreateDependencyTree();


        Assert.Same(service1.ConstructorDependencyList[0].Service, service2);
    }

    #endregion
}
