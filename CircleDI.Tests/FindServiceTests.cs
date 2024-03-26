using CircleDI.Defenitions;
using CircleDI.Generation;

namespace CircleDI.Tests;

/// <summary>
/// Tests the function <see cref="ServiceProvider.FindService(string)"/>.
/// </summary>
public static class FindServiceTests {
    private static ServiceProvider CreateProvider(string[] serviceTypeList) {
        List<Service> serviceList = serviceTypeList.Select((string serviceType) => new Service() {
            ServiceType = new TypeName(serviceType),
            Name = string.Empty,
            ImplementationType = default,
            Lifetime = ServiceLifetime.Singleton,
            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = []
        }).ToList();
        serviceList.Sort((Service x, Service y) => x.ServiceType.CompareTo(y.ServiceType));

        ServiceProvider serviceProvider = new(null!) {
            Identifier = new TypeName("TestProvider", [], [], []),
            IdentifierScope = new TypeName("TestProviderScope", [], [], []),
            InterfaceIdentifier = new TypeName("ITestProvider", [], [], []),
            InterfaceIdentifierScope = new TypeName("ITestProvider.IScope", [], [], []),
        };
        SetSortedServiceList(serviceProvider, serviceList);
        return serviceProvider;
    }

    [System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "set_SortedServiceList")]
    private extern static void SetSortedServiceList(ServiceProvider instance, List<Service> value);


    [Fact]
    public static void NotFound() {
        ServiceProvider serviceProvider = CreateProvider(["test1, test2, test3"]);

        (int index, int count) = serviceProvider.FindService(new TypeName("notPresent"));
        
        Assert.Equal(-1, index);
        Assert.Equal(0, count);
    }

    [Fact]
    public static void Empty() {
        ServiceProvider serviceProvider = CreateProvider([]);

        (int index, int count) = serviceProvider.FindService(new TypeName("notPresent"));

        Assert.Equal(-1, index);
        Assert.Equal(0, count);
    }

    [Fact]
    public static void OneElement() {
        ServiceProvider serviceProvider = CreateProvider(["test1"]);

        (int index, int count) = serviceProvider.FindService(new TypeName("test1"));

        Assert.Equal(0, index);
        Assert.Equal(1, count);
    }


    [Fact]
    public static void TwoElements_FindFirst() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2"]);

        (int index, int count) = serviceProvider.FindService(new TypeName("test1"));

        Assert.Equal(0, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public static void TwoElements_FindSecond() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2"]);

        (int index, int count) = serviceProvider.FindService(new TypeName("test2"));

        Assert.Equal(1, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public static void TwoElements_BothEqual() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test1"]);

        (int index, int count) = serviceProvider.FindService(new TypeName("test1"));

        Assert.Equal(0, index);
        Assert.Equal(2, count);
    }


    [Fact]
    public static void ThreeElements_FindFirst() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = serviceProvider.FindService(new TypeName("test1"));

        Assert.Equal(0, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public static void ThreeElements_FindSecond() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = serviceProvider.FindService(new TypeName("test2"));

        Assert.Equal(1, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public static void ThreeElements_FindThird() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = serviceProvider.FindService(new TypeName("test3"));

        Assert.Equal(2, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public static void ThreeElements_FindFirst2() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test1", "test3"]);

        (int index, int count) = serviceProvider.FindService(new TypeName("test1"));

        Assert.Equal(0, index);
        Assert.Equal(2, count);
    }

    [Fact]
    public static void ThreeElements_FindLast2() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test2"]);

        (int index, int count) = serviceProvider.FindService(new TypeName("test2"));

        Assert.Equal(1, index);
        Assert.Equal(2, count);
    }

    [Fact]
    public static void ThreeElements_FindAll3() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test1", "test1"]);

        (int index, int count) = serviceProvider.FindService(new TypeName("test1"));

        Assert.Equal(0, index);
        Assert.Equal(3, count);
    }


    [Fact]
    public static void Twentylements_Find04To07() {
        ServiceProvider serviceProvider = CreateProvider([
            "test01",
            "test02",
            "test03",
            "test07",
            "test07",
            "test07",
            "test07",
            "test08",
            "test09",
            "test10",
            "test11",
            "test12",
            "test13",
            "test14",
            "test15",
            "test15",
            "test17",
            "test18",
            "test18",
            "test20"
        ]);

        (int index, int count) = serviceProvider.FindService(new TypeName("test07"));

        Assert.Equal(3, index);
        Assert.Equal(4, count);
    }
}
