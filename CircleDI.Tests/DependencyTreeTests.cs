using CircleDI.Defenitions;
using CircleDI.Generation;
using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Tests;

/// <summary>
/// Tests the function <see cref="ServiceProvider.CreateDependencyTree()"/>.
/// </summary>
public sealed class DependencyTreeTests {
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



    [Test]
    public async ValueTask EmptyDoesNothing() {
        ServiceProvider serviceProvider = CreateProvider([]);

        serviceProvider.CreateDependencyTree();

        await Assert.That(serviceProvider.SingletonList).IsEmpty();
        await Assert.That(serviceProvider.ScopedList).IsEmpty();
        await Assert.That(serviceProvider.TransientList).IsEmpty();
    }

    [Test]
    public async ValueTask MultipleServicesWithSameTypeWithoutNamingFails() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI031");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Ambiguous dependency at Service 'TestService1' with type 'MyCode.TestService2': There are multiple Services registered for this type: [\"TestService2_1\", \"TestService2_2\"]. Use the '[Dependency(Name=\"...\")]'-attribute on the parameter to choose one specific service");
    }


    #region Tree

    [Test]
    public async ValueTask Tree_SimplePath_Constructor() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
        await Assert.That(service3).IsSameReferenceAs(service2.ConstructorDependencyList[0].Service);
    }

    [Test]
    public async ValueTask Tree_SimplePath_Property() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.PropertyDependencyList[0].Service);
        await Assert.That(service3).IsSameReferenceAs(service2.PropertyDependencyList[0].Service);
        await Assert.That(service1.PropertyDependencyList[0].IsCircular).IsFalse();
        await Assert.That(service2.PropertyDependencyList[0].IsCircular).IsFalse();
    }

    [Test]
    public async ValueTask Tree_NormalTreePath() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.PropertyDependencyList[0].Service);
        await Assert.That(service3).IsSameReferenceAs(service1.PropertyDependencyList[1].Service);

        await Assert.That(service4).IsSameReferenceAs(service2.ConstructorDependencyList[0].Service);
        await Assert.That(service5).IsSameReferenceAs(service2.ConstructorDependencyList[1].Service);

        await Assert.That(service6).IsSameReferenceAs(service3.PropertyDependencyList[0].Service);
        await Assert.That(service7).IsSameReferenceAs(service3.PropertyDependencyList[1].Service);
    }

    [Test]
    public async ValueTask Tree_DoublePath() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[1].Service);
    }

    [Test]
    public async ValueTask Tree_MergingPath() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
        await Assert.That(service3).IsSameReferenceAs(service1.ConstructorDependencyList[1].Service);
        await Assert.That(service3).IsSameReferenceAs(service2.ConstructorDependencyList[0].Service);
    }

    [Test]
    public async ValueTask Tree_DiamondMerging() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
        await Assert.That(service3).IsSameReferenceAs(service1.ConstructorDependencyList[1].Service);
        await Assert.That(service4).IsSameReferenceAs(service2.ConstructorDependencyList[0].Service);
        await Assert.That(service4).IsSameReferenceAs(service3.ConstructorDependencyList[0].Service);
    }

    [Test]
    public async ValueTask Tree_MissingDependencyFails() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI029");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Unregistered dependency at Service 'TestService1' with type 'MyCode.TestService2'");
    }

    [Test]
    public async ValueTask Tree_NotDeclaredInterfaceDependencyFails() {
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

        await Assert.That(diagnostics.Length).IsEqualTo(2);
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI038");
        await Assert.That(diagnostics[1].Id).IsEqualTo("CDI030");
        await Assert.That(diagnostics[1].GetMessage()).IsEqualTo("Unregistered dependency 'ITestProvider' has the same identifier as generated interface type 'MyCode.ITestProvider', only missing the namespace. If you mean this generated type, you can correct the namespace by just declaring the interface type in namespace 'MyCode': \"public partial interface ITestProvider;\"");
    }

    [Test]
    public async ValueTask Tree_MissingNamedDependencyFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService1>]
            public sealed partial class TestProvider;

            public sealed class TestService1([Dependency(Name = "Asdf")]TestService2 testService2);

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI032");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Unregistered named dependency at Service 'TestService1' with name \"Asdf\"");
    }

    #endregion


    #region MultipleRoots

    [Test]
    public async ValueTask MultipleRoots_MergingRoots() {
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


        await Assert.That(service3).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
        await Assert.That(service3).IsSameReferenceAs(service2.ConstructorDependencyList[0].Service);
    }

    [Test]
    public async ValueTask MultipleRoots_MergingPath() {
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


        await Assert.That(service4).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
        await Assert.That(service3).IsSameReferenceAs(service2.ConstructorDependencyList[0].Service);
        await Assert.That(service4).IsSameReferenceAs(service3.ConstructorDependencyList[0].Service);
    }

    #endregion


    #region MultipleTrees

    [Test]
    public async ValueTask MultipleTrees_IndependServices() {
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


        await Assert.That(service2).IsNotSameReferenceAs(service1);
    }

    [Test]
    public async ValueTask MultipleTrees_IndependentPath() {
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


        await Assert.That(service4).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
        await Assert.That(service3).IsSameReferenceAs(service2.ConstructorDependencyList[0].Service);
        await Assert.That(service4).IsSameReferenceAs(service3.ConstructorDependencyList[0].Service);
        await Assert.That(service6).IsSameReferenceAs(service5.PropertyDependencyList[0].Service);
    }

    #endregion


    #region Circles

    [Test]
    public async ValueTask Circles_SimpleCircle() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.PropertyDependencyList[0].Service);
        await Assert.That(service1).IsSameReferenceAs(service2.PropertyDependencyList[0].Service);
        await Assert.That(service1.PropertyDependencyList[0].IsCircular ^ service2.PropertyDependencyList[0].IsCircular).IsTrue();
    }

    [Test]
    public async ValueTask Circles_SelfCircle() {
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


        await Assert.That(service1).IsSameReferenceAs(service1.PropertyDependencyList[0].Service);
        await Assert.That(service1.PropertyDependencyList[0].IsCircular).IsTrue();
    }

    [Test]
    public async ValueTask Circles_ConstructorPropertyCircle() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
        await Assert.That(service1).IsSameReferenceAs(service2.PropertyDependencyList[0].Service);
        await Assert.That(service2.PropertyDependencyList[0].IsCircular).IsTrue();
    }

    [Test]
    public async ValueTask Circles_InvalidCircle() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService1>]
            public sealed partial class TestProvider;

            public sealed class TestService1(TestService1 testService1);

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI033");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Circular dependency unresolvable: ['TestService1' -> 'TestService1']. Only singleton and scoped dependencies injected as properties can be resolved circular");
    }

    #endregion


    #region Lifetime

    [Test]
    public async ValueTask Lifetime_SingletonToScoped() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI034");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Lifetime Violation: Singleton Service 'TestService1' has Scoped dependency 'MyCode.TestService2'");
    }

    [Test]
    public async ValueTask Lifetime_SingletonToTransient() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
        await Assert.That(service2.Lifetime).IsEqualTo(ServiceLifetime.Transient);
    }

    [Test]
    public async ValueTask Lifetime_SingletonToTransientScoped() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI035");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Lifetime Violation: Singleton Service 'TestService1' has Transient-Scoped dependency 'MyCode.TestService2'. \"Transient-Scoped\" means the service itself is transient, but it has at least one dependency or one dependency of the dependencies that is Scoped");
    }

    [Test]
    public async ValueTask Lifetime_SingletonToDelegate() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
    }

    [Test]
    public async ValueTask Lifetime_SingletonToDelegateScoped() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI036");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Lifetime Violation: Singleton Service 'TestService1' has Delegate-Scoped dependency 'System.Action'. \"Delegate-Scoped\" means the method is declared inside Scope and therefore only available for scoped services.");
    }

    [Test]
    public async ValueTask Lifetime_SingletonToMutlipleScoped() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI037");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Lifetime Violation: Singleton Service 'TestService' has dependency with type 'MyCode.ITestDependency' and there are multiple services of that type, but they are all invalid (Scoped or Transient-Scoped): [\"TestDependency1\", \"TestDependency2\"]");
    }


    [Test]
    public async ValueTask Lifetime_ScopedToSingleton() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
    }

    [Test]
    public async ValueTask Lifetime_ScopedToTransient() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
        await Assert.That(service2.Lifetime).IsEqualTo(ServiceLifetime.Transient);
    }

    [Test]
    public async ValueTask Lifetime_ScopedToDelegate() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
    }


    [Test]
    public async ValueTask Lifetime_TransientToSingleton() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
        await Assert.That(service1.Lifetime).IsEqualTo(ServiceLifetime.Transient);
    }

    [Test]
    public async ValueTask Lifetime_TransientToScoped() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
        await Assert.That(service1.Lifetime).IsEqualTo(ServiceLifetime.TransientScoped);
    }

    [Test]
    public async ValueTask Lifetime_TransientToDelegate() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
    }

    [Test]
    public async ValueTask Lifetime_TransientToDelegateScoped() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
        await Assert.That(service1.Lifetime).IsEqualTo(ServiceLifetime.TransientScoped);
    }

    #endregion


    #region CreationTiming

    [Test]
    public async ValueTask CreationTiming_ConstructorToConstructor() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
    }

    [Test]
    public async ValueTask CreationTiming_ConstructorToLazy() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
        await Assert.That(service2.CreationTimeTransitive).IsEqualTo(CreationTiming.Constructor);
    }

    [Test]
    public async ValueTask CreationTiming_LazyToConstructor() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
    }

    [Test]
    public async ValueTask CreationTiming_LazyToLazy() {
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


        await Assert.That(service2).IsSameReferenceAs(service1.ConstructorDependencyList[0].Service);
    }

    #endregion
}
