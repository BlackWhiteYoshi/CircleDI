using CircleDI.Defenitions;
using CircleDI.Generation;

namespace CircleDI.Tests;

/// <summary>
/// Tests the function <i>FindService</i> in <i>ServiceProvider.DependencyTreeInitializer</i>.
/// </summary>
public static class FindServiceTests {
    private static ServiceProvider CreateProvider(string[] serviceTypeList) {
        List<Service> serviceList = serviceTypeList.Select((string serviceType) => new Service() {
            ServiceType = new TypeName(serviceType),
            Name = serviceType,
            ImplementationType = new TypeName(serviceType),
            Lifetime = ServiceLifetime.Singleton,
            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = []
        }).ToList();
        serviceList.Sort((Service x, Service y) => x.ServiceType.CompareTo(y.ServiceType));

        ServiceProvider serviceProvider = new(null!) {
            Identifier = new TypeName("TestProvider", TypeKeyword.Class, [], [], [], []),
            IdentifierScope = new TypeName("TestProvider.Scope", TypeKeyword.Class, [], [], [], []),
            InterfaceIdentifier = new TypeName("ITestProvider", TypeKeyword.Interface, [], [], [], []),
            InterfaceIdentifierScope = new TypeName("ITestProvider.IScope", TypeKeyword.Interface, [], [], [], [])
        };
        SetSortedServiceList(serviceProvider, serviceList);
        return serviceProvider;
    }

    [System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "set_SortedServiceList")]
    private extern static void SetSortedServiceList(ServiceProvider instance, List<Service> value);

    private static (int index, int count) FindService(TypeName serviceType, List<Service> serviceList)
        => (ValueTuple<int, int>)typeof(ServiceProvider).Assembly
            .GetType("CircleDI.Generation.ServiceProvider+DependencyTreeInitializer")!
            .GetMethod("FindService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!
            .Invoke(null, [serviceType, serviceList])!;



    [Fact]
    public static void Empty() {
        ServiceProvider serviceProvider = CreateProvider([]);

        (int index, int count) = FindService(new TypeName("notPresent"), serviceProvider.SortedServiceList);

        Assert.Equal(0, index);
        Assert.Equal(0, count);
    }

    [Fact]
    public static void NotFoundFirst() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = FindService(new TypeName("test0"), serviceProvider.SortedServiceList);

        Assert.Equal(0, index);
        Assert.Equal(0, count);
    }

    [Fact]
    public static void NotFoundLast() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = FindService(new TypeName("test4"), serviceProvider.SortedServiceList);

        Assert.Equal(3, index);
        Assert.Equal(0, count);
    }

    [Fact]
    public static void NotFoundSecond() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test3", "test4", "test5"]);

        (int index, int count) = FindService(new TypeName("test2"), serviceProvider.SortedServiceList);

        Assert.Equal(1, index);
        Assert.Equal(0, count);
    }

    [Fact]
    public static void NotFoundSecondLast() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3", "test5"]);

        (int index, int count) = FindService(new TypeName("test4"), serviceProvider.SortedServiceList);

        Assert.Equal(3, index);
        Assert.Equal(0, count);
    }

    [Fact]
    public static void OneElement() {
        ServiceProvider serviceProvider = CreateProvider(["test1"]);

        (int index, int count) = FindService(new TypeName("test1"), serviceProvider.SortedServiceList);

        Assert.Equal(0, index);
        Assert.Equal(1, count);
    }


    [Fact]
    public static void TwoElements_FindFirst() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2"]);

        (int index, int count) = FindService(new TypeName("test1"), serviceProvider.SortedServiceList);

        Assert.Equal(0, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public static void TwoElements_FindSecond() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2"]);

        (int index, int count) = FindService(new TypeName("test2"), serviceProvider.SortedServiceList);

        Assert.Equal(1, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public static void TwoElements_BothEqual() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test1"]);

        (int index, int count) = FindService(new TypeName("test1"), serviceProvider.SortedServiceList);

        Assert.Equal(0, index);
        Assert.Equal(2, count);
    }


    [Fact]
    public static void ThreeElements_FindFirst() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = FindService(new TypeName("test1"), serviceProvider.SortedServiceList);

        Assert.Equal(0, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public static void ThreeElements_FindSecond() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = FindService(new TypeName("test2"), serviceProvider.SortedServiceList);

        Assert.Equal(1, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public static void ThreeElements_FindThird() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = FindService(new TypeName("test3"), serviceProvider.SortedServiceList);

        Assert.Equal(2, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public static void ThreeElements_FindFirst2() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test1", "test3"]);

        (int index, int count) = FindService(new TypeName("test1"), serviceProvider.SortedServiceList);

        Assert.Equal(0, index);
        Assert.Equal(2, count);
    }

    [Fact]
    public static void ThreeElements_FindLast2() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test2"]);

        (int index, int count) = FindService(new TypeName("test2"), serviceProvider.SortedServiceList);

        Assert.Equal(1, index);
        Assert.Equal(2, count);
    }

    [Fact]
    public static void ThreeElements_FindAll3() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test1", "test1"]);

        (int index, int count) = FindService(new TypeName("test1"), serviceProvider.SortedServiceList);

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

        (int index, int count) = FindService(new TypeName("test07"), serviceProvider.SortedServiceList);

        Assert.Equal(3, index);
        Assert.Equal(4, count);
    }
}
