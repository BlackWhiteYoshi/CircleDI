using CircleDI.Defenitions;
using CircleDI.Generation;

namespace CircleDI.Tests;

/// <summary>
/// Tests the function <i>FindService</i> in <i>ServiceProvider.DependencyTreeInitializer</i>.
/// </summary>
public sealed class FindServiceTests {
    private static ServiceProvider CreateProvider(string[] serviceTypeList) {
        List<Service> serviceList = [.. serviceTypeList.Select((string serviceType) => new Service() {
            ServiceType = new TypeName(serviceType),
            Name = serviceType,
            ImplementationType = new TypeName(serviceType),
            Lifetime = ServiceLifetime.Singleton,
            ConstructorDependencyList = [],
            PropertyDependencyList = [],
            Dependencies = []
        })];
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


    [Before(Class)]
    public static void InitFindServiceMethod() {
        findServiceMethod = typeof(ServiceProvider).Assembly
            .GetType("CircleDI.Generation.ServiceProvider+DependencyTreeInitializer")!
            .GetMethod("FindService", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
    }

    private static System.Reflection.MethodInfo findServiceMethod = null!;

    private static (int index, int count) FindService(TypeName serviceType, List<Service> serviceList) => (ValueTuple<int, int>)findServiceMethod.Invoke(null, [serviceType, serviceList])!;



    [Test]
    public async ValueTask Empty() {
        ServiceProvider serviceProvider = CreateProvider([]);

        (int index, int count) = FindService(new TypeName("notPresent"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(0);
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async ValueTask NotFoundFirst() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = FindService(new TypeName("test0"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(0);
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async ValueTask NotFoundLast() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = FindService(new TypeName("test4"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(3);
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async ValueTask NotFoundSecond() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test3", "test4", "test5"]);

        (int index, int count) = FindService(new TypeName("test2"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(1);
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async ValueTask NotFoundSecondLast() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3", "test5"]);

        (int index, int count) = FindService(new TypeName("test4"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(3);
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async ValueTask OneElement() {
        ServiceProvider serviceProvider = CreateProvider(["test1"]);

        (int index, int count) = FindService(new TypeName("test1"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(0);
        await Assert.That(count).IsEqualTo(1);
    }


    [Test]
    public async ValueTask TwoElements_FindFirst() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2"]);

        (int index, int count) = FindService(new TypeName("test1"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(0);
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async ValueTask TwoElements_FindSecond() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2"]);

        (int index, int count) = FindService(new TypeName("test2"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(1);
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async ValueTask TwoElements_BothEqual() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test1"]);

        (int index, int count) = FindService(new TypeName("test1"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(0);
        await Assert.That(count).IsEqualTo(2);
    }


    [Test]
    public async ValueTask ThreeElements_FindFirst() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = FindService(new TypeName("test1"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(0);
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async ValueTask ThreeElements_FindSecond() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = FindService(new TypeName("test2"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(1);
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async ValueTask ThreeElements_FindThird() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = FindService(new TypeName("test3"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(2);
        await Assert.That(count).IsEqualTo(1);
    }

    [Test]
    public async ValueTask ThreeElements_FindFirst2() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test1", "test3"]);

        (int index, int count) = FindService(new TypeName("test1"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(0);
        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    public async ValueTask ThreeElements_FindLast2() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test2"]);

        (int index, int count) = FindService(new TypeName("test2"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(1);
        await Assert.That(count).IsEqualTo(2);
    }

    [Test]
    public async ValueTask ThreeElements_FindAll3() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test1", "test1"]);

        (int index, int count) = FindService(new TypeName("test1"), serviceProvider.SortedServiceList);

        await Assert.That(index).IsEqualTo(0);
        await Assert.That(count).IsEqualTo(3);
    }


    [Test]
    public async ValueTask Twentylements_Find04To07() {
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

        await Assert.That(index).IsEqualTo(3);
        await Assert.That(count).IsEqualTo(4);
    }
}
