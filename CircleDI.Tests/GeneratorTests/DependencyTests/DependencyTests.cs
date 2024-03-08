using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Tests;

/// <summary>
/// Tests for dependency injection and corresponding attributes (DependencyAttribute, Constructorttribute).
/// </summary>
public static class DependencyTests {
    [Fact]
    public static Task SingleSingleton() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency, TestDependency>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService(ITestDependency testDependency) : ITestService;

            public interface ITestDependency;
            public sealed class TestDependency : ITestDependency;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task SingleScoped() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>]
            [Scoped<ITestDependency, TestDependency>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService(ITestDependency testDependency) : ITestService;

            public interface ITestDependency;
            public sealed class TestDependency : ITestDependency;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task SingleTransient() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>]
            [Transient<ITestDependency, TestDependency>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService(ITestDependency testDependency) : ITestService;

            public interface ITestDependency;
            public sealed class TestDependency : ITestDependency;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task SingleTransientScoped() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>]
            [Transient<ITestDependency, TestDependency>]
            [Scoped<IScopedDependency, ScopedDependency>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService(ITestDependency testDependency) : ITestService;
            
            public interface ITestDependency;
            public sealed class TestDependency(IScopedDependency scopedDependency) : ITestDependency;

            public interface IScopedDependency;
            public sealed class ScopedDependency : IScopedDependency;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }


    [Fact]
    public static Task MultipleSingleton() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency1, TestDependency1>]
            [Singleton<ITestDependency2, TestDependency2>]
            [Singleton<ITestDependency3, TestDependency3>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService(ITestDependency1 testDependency1, ITestDependency2 testDependency2, ITestDependency3 testDependency3) : ITestService;

            public interface ITestDependency1;
            public sealed class TestDependency1 : ITestDependency1;

            public interface ITestDependency2;
            public sealed class TestDependency2 : ITestDependency2;

            public interface ITestDependency3;
            public sealed class TestDependency3 : ITestDependency3;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task MultipleScoped() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>]
            [Scoped<ITestDependency1, TestDependency1>]
            [Scoped<ITestDependency2, TestDependency2>]
            [Scoped<ITestDependency3, TestDependency3>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService(ITestDependency1 testDependency1, ITestDependency2 testDependency2, ITestDependency3 testDependency3) : ITestService;

            public interface ITestDependency1;
            public sealed class TestDependency1 : ITestDependency1;

            public interface ITestDependency2;
            public sealed class TestDependency2 : ITestDependency2;

            public interface ITestDependency3;
            public sealed class TestDependency3 : ITestDependency3;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task MultipleTransient() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>]
            [Transient<ITestDependency1, TestDependency1>]
            [Transient<ITestDependency2, TestDependency2>]
            [Transient<ITestDependency3, TestDependency3>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService(ITestDependency1 testDependency1, ITestDependency2 testDependency2, ITestDependency3 testDependency3) : ITestService;

            public interface ITestDependency1;
            public sealed class TestDependency1 : ITestDependency1;

            public interface ITestDependency2;
            public sealed class TestDependency2 : ITestDependency2;

            public interface ITestDependency3;
            public sealed class TestDependency3 : ITestDependency3;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task NamedDependency() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency, TestDependency>(Name = "Asdf")]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService([Dependency(Name = "Asdf")] ITestDependency testDependency) : ITestService;

            public interface ITestDependency;
            public sealed class TestDependency : ITestDependency;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task SingleProperty() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency, TestDependency>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService : ITestService {
                public required ITestDependency TestDependency { private get; init; }
            }

            public interface ITestDependency;
            public sealed class TestDependency : ITestDependency;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task TransientProperty() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>]
            [Transient<ITestDependency, TestDependency>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService : ITestService {
                public required ITestDependency TestDependency { private get; init; }
            }

            public interface ITestDependency;
            public sealed class TestDependency : ITestDependency;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task MultipleProperties() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency1, TestDependency1>]
            [Singleton<ITestDependency2, TestDependency2>]
            [Singleton<ITestDependency3, TestDependency3>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService : ITestService {
                public required ITestDependency1 TestDependency1 { private get; init; }
                public required ITestDependency2 TestDependency2 { private get; init; }
                public required ITestDependency3 TestDependency3 { private get; init; }
            }

            public interface ITestDependency1;
            public sealed class TestDependency1 : ITestDependency1;

            public interface ITestDependency2;
            public sealed class TestDependency2 : ITestDependency2;

            public interface ITestDependency3;
            public sealed class TestDependency3 : ITestDependency3;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task PropertyWithAttribute() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency, TestDependency>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService : ITestService {
                [Dependency]
                public ITestDependency TestDependency { private get; init; }
            }

            public interface ITestDependency;
            public sealed class TestDependency : ITestDependency;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task PropertyAsNamedDependency() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency, TestDependency>(Name = "Asdf")]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService : ITestService {
                [Dependency(Name = "Asdf")]
                public ITestDependency TestDependency { private get; init; }
            }

            public interface ITestDependency;
            public sealed class TestDependency : ITestDependency;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task NormalPropertyIsIgnored() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency, TestDependency>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService : ITestService {
                public ITestDependency TestDependency { private get; init; }
            }

            public interface ITestDependency;
            public sealed class TestDependency : ITestDependency;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }


    [Fact]
    public static Task MultipleConstructorWithConstructorAttribute() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency, TestDependency>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService : ITestService {
                [Constructor]
                public TestService() { }

                public TestService(ITestDependency testDependency) { }
            }

            public interface ITestDependency;
            public sealed class TestDependency : ITestDependency;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task MultipleConstructorWithConstructorAttributeOnPrimary() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency, TestDependency>]
            public sealed partial class TestProvider;
            
            public interface ITestService;

            [method: Constructor]
            public sealed class TestService(ITestDependency testDependency) : ITestService {
                public TestService() { }
            }

            public interface ITestDependency;
            public sealed class TestDependency : ITestDependency;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static void MultipleConstructorWithoutAttributeFails() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency, TestDependency>]
            public sealed partial class TestProvider;
            
            public interface ITestService;

            public sealed class TestService(ITestDependency testDependency) : ITestService {
                public TestService() { }
            }

            public interface ITestDependency;
            public sealed class TestDependency : ITestDependency;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI018", diagnostics[0].Id);
        Assert.Equal("No ConstructorAttribute at ServiceImplementation 'MyCode.TestService', but there are multiple constructors", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void MultipleConstructorWithMultipleAttributesFails() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency, TestDependency>]
            public sealed partial class TestProvider;
            
            public interface ITestService;

            [method: Constructor]
            public sealed class TestService(ITestDependency testDependency) : ITestService {
                [Constructor]
                public TestService() { }
            }

            public interface ITestDependency;
            public sealed class TestDependency : ITestDependency;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI019", diagnostics[0].Id);
        Assert.Equal("Multiple ConstructorAttributes at ServiceImplementation 'MyCode.TestService', there must be exactly one when there are multiple constructors", diagnostics[0].GetMessage());
    }

    [Fact]
    public static Task CircularSelfSetAccessor() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService : ITestService {
                public required ITestService Self { private get; set; }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task CircularSelfInitAccessor() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService : ITestService {
                public required ITestService Self { private get; init; }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static void CircularSelfNoAccessorFails() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService : ITestService {
                [Dependency]
                public ITestService Self { get; }
            }

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI020", diagnostics[0].Id);
        Assert.Equal("No set/init accessor at Property 'MyCode.TestService.Self'", diagnostics[0].GetMessage());
    }

    [Fact]
    public static Task CircularServices() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService1, TestService1>]
            [Singleton<ITestService2, TestService2>]
            public sealed partial class TestProvider;
            
            public interface ITestService1;
            public sealed class TestService1 : ITestService1 {
                public required ITestService2 TestService2 { private get; init; }
            }

            public interface ITestService2;
            public sealed class TestService2 : ITestService2 {
                public required ITestService1 TestService1 { private get; init; }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task CircularManyServices() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService1, TestService1>]
            [Singleton<ITestService2, TestService2>]
            [Singleton<ITestService3, TestService3>]
            [Singleton<ITestService4, TestService4>]
            [Singleton<ITestService5, TestService5>]
            public sealed partial class TestProvider;
            
            public interface ITestService1;
            public sealed class TestService1(ITestService2 TestService2) : ITestService1;

            public interface ITestService2;
            public sealed class TestService2(ITestService3 TestService3) : ITestService2;

            public interface ITestService3;
            public sealed class TestService3(ITestService4 TestService4) : ITestService3;

            public interface ITestService4;
            public sealed class TestService4(ITestService5 TestService5) : ITestService4;

            public interface ITestService5;
            public sealed class TestService5 : ITestService5 {
                public required ITestService1 TestService1 { private get; init; }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }

    [Fact]
    public static Task CircularLazyServices() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider(CreationTime = CreationTiming.Lazy)]
            [Singleton<ITestService1, TestService1>]
            [Singleton<ITestService2, TestService2>]
            public sealed partial class TestProvider;
            
            public interface ITestService1;
            public sealed class TestService1(ITestService2 TestService2) : ITestService1;

            public interface ITestService2;
            public sealed class TestService2 : ITestService2 {
                public required ITestService1 TestService1 { private get; init; }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        return Verify(sourceTextClass);
    }
}
