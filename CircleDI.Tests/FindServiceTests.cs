namespace CircleDI.Tests;

/// <summary>
/// Tests the function <see cref="ServiceProvider.FindService(string)"/>.
/// </summary>
public sealed class FindServiceTests {
    private static ServiceProvider CreateProvider(string[] serviceTypeList)
        => new(null!) {
            Name = "TestProvider",
            InterfaceName = "ITestProvider",
            NameSpace = string.Empty,
            ServiceList = serviceTypeList.Select((string serviceType) => new Service() {
                ServiceType = serviceType,
                Name = string.Empty,
                ImplementationType = string.Empty,
                Lifetime = ServiceLifetime.Singleton,
                ConstructorDependencyList = [],
                PropertyDependencyList = [],
            }).ToList()
        };

    [Fact]
    public void NotFound() {
        ServiceProvider serviceProvider = CreateProvider(["test1, test2, test3"]);

        (int index, int count) = serviceProvider.FindService("notPresent");
        
        Assert.Equal(-1, index);
        Assert.Equal(0, count);
    }

    [Fact]
    public void Empty() {
        ServiceProvider serviceProvider = CreateProvider([]);

        (int index, int count) = serviceProvider.FindService("notPresent");

        Assert.Equal(-1, index);
        Assert.Equal(0, count);
    }

    [Fact]
    public void OneElement() {
        ServiceProvider serviceProvider = CreateProvider(["test1"]);

        (int index, int count) = serviceProvider.FindService("test1");

        Assert.Equal(0, index);
        Assert.Equal(1, count);
    }


    [Fact]
    public void TwoElements_FindFirst() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2"]);

        (int index, int count) = serviceProvider.FindService("test1");

        Assert.Equal(0, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public void TwoElements_FindSecond() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2"]);

        (int index, int count) = serviceProvider.FindService("test2");

        Assert.Equal(1, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public void TwoElements_BothEqual() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test1"]);

        (int index, int count) = serviceProvider.FindService("test1");

        Assert.Equal(0, index);
        Assert.Equal(2, count);
    }


    [Fact]
    public void ThreeElements_FindFirst() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = serviceProvider.FindService("test1");

        Assert.Equal(0, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public void ThreeElements_FindSecond() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = serviceProvider.FindService("test2");

        Assert.Equal(1, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public void ThreeElements_FindThird() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test3"]);

        (int index, int count) = serviceProvider.FindService("test3");

        Assert.Equal(2, index);
        Assert.Equal(1, count);
    }

    [Fact]
    public void ThreeElements_FindFirst2() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test1", "test3"]);

        (int index, int count) = serviceProvider.FindService("test1");

        Assert.Equal(0, index);
        Assert.Equal(2, count);
    }

    [Fact]
    public void ThreeElements_FindLast2() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test2", "test2"]);

        (int index, int count) = serviceProvider.FindService("test2");

        Assert.Equal(1, index);
        Assert.Equal(2, count);
    }

    [Fact]
    public void ThreeElements_FindAll3() {
        ServiceProvider serviceProvider = CreateProvider(["test1", "test1", "test1"]);

        (int index, int count) = serviceProvider.FindService("test1");

        Assert.Equal(0, index);
        Assert.Equal(3, count);
    }


    [Fact]
    public void Twentylements_Find04To07() {
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

        (int index, int count) = serviceProvider.FindService("test07");

        Assert.Equal(3, index);
        Assert.Equal(4, count);
    }
}
