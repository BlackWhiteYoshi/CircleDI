using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Tests;

/// <summary>
/// Tests for dependency injection and corresponding attributes (DependencyAttribute, Constructorttribute).
/// </summary>
public sealed class DependencyTests {
    [Test]
    public async ValueTask SingleSingleton() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask SingleScoped() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask SingleTransient() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask SingleTransientScoped() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ScopedServiceOnLazySingleton() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<IScopedService, ScopedService>]
            [Singleton<ISingletonDependency, SingletonDependency>(CreationTime = CreationTiming.Lazy)]
            public sealed partial class TestProvider;

            public interface IScopedService;
            public sealed class ScopedService(ISingletonDependency singletonDependency) : IScopedService;

            public interface ISingletonDependency;
            public sealed class SingletonDependency : ISingletonDependency;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask MultipleSingleton() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask MultipleScoped() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask MultipleTransient() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask NamedDependency() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask SingleProperty() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask TransientProperty() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask MultipleProperties() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask PropertyWithAttribute() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask PropertyAsNamedDependency() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask NormalPropertyIsIgnored() {
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

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask ServiceProviderDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            public partial interface ITestProvider;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Scoped<ITestService, TestService>(Name = "TestServiceScoped")]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService(ITestProvider testProvider) : ITestService;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ScopeProviderDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            public partial interface ITestProvider {
                public partial interface IScope;
            }

            [ServiceProvider]
            [Scoped<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService(ITestProvider.IScope testProvider) : ITestService;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask MultipleConstructorWithConstructorAttribute() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask MultipleConstructorWithConstructorAttributeOnPrimary() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask MultipleConstructorWithoutAttributeFails() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI026");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("No ConstructorAttribute at ServiceImplementation 'MyCode.TestService', but there are multiple constructors");
    }

    [Test]
    public async ValueTask MultipleConstructorWithMultipleAttributesFails() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI027");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Multiple ConstructorAttributes at ServiceImplementation 'MyCode.TestService', there must be exactly one when there are multiple constructors");
    }


    [Test]
    public async ValueTask DeepTreeConstructor() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]

            [Scoped<IRoot, Root>]

            [Scoped<S1>]
            [Transient<T1>]
            [Transient<T15>]

            [Transient<T2>]
            [Singleton<S2>(CreationTime = CreationTiming.Lazy)]

            [Scoped<S3_1>]
            [Scoped<S3_2>]
            [Transient<T3>]
            public sealed partial class TestProvider;


            public interface IRoot;
            public sealed class Root(T1 t1, S1 s1) : IRoot;

            public sealed class S1(T2 t2, S2 s2);
            public sealed class T1(T15 t15);
            public sealed class T15(T2 t2) {
                public required IRoot Root { private get; init; }
            }

            public sealed class T2(S3_1 s3_1, S3_2 s3_2, T3 t3);
            public sealed class S2(T3 t3);

            public sealed class S3_1;
            public sealed class S3_2;
            public sealed class T3 : System.IDisposable, System.IAsyncDisposable;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask DeepTreeLazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(CreationTime = CreationTiming.Lazy)]

            [Scoped<IRoot, Root>]

            [Scoped<S1>]
            [Transient<T1>]
            [Transient<T15>]

            [Transient<T2>]
            [Singleton<S2>(CreationTime = CreationTiming.Lazy)]

            [Scoped<S3_1>]
            [Scoped<S3_2>]
            [Transient<T3>]
            public sealed partial class TestProvider;


            public interface IRoot;
            public sealed class Root(T1 t1, S1 s1) : IRoot;

            public sealed class S1(T2 t2, S2 s2);
            public sealed class T1(T15 t15);
            public sealed class T15(T2 t2) {
                public required IRoot Root { private get; init; }
            }

            public sealed class T2(S3_1 s3_1, S3_2 s3_2, T3 t3);
            public sealed class S2(T3 t3);

            public sealed class S3_1;
            public sealed class S3_2;
            public sealed class T3 : System.IDisposable, System.IAsyncDisposable;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask CircularSelfSetAccessor() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask CircularSelfInitAccessor() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask CircularSelfNoAccessorFails() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI028");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("No set/init accessor at Property 'MyCode.TestService.Self'");
    }

    [Test]
    public async ValueTask CircularServices() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask CircularTransient() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService1, TestService1>]
            [Singleton<ITestService2, TestService2>]
            public sealed partial class TestProvider;

            public interface ITestService1;
            public sealed class TestService1 : ITestService1 {
                public required ITestService2 TestService2 { private get; init; }
            }

            public interface ITestService2;
            public sealed class TestService2(ITestService1 testService1) : ITestService2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask CircularManyServices() {
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

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask CircularLazyServices() {
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

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask FullTangle3() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<Service1>]
            [Singleton<Service2>]
            [Singleton<Service3>]
            public sealed partial class TestProvider;


            public sealed class Service1 {
                public required Service2 Service2 { private get; init; }
                public required Service3 Service3 { private get; init; }
            }

            public sealed class Service2 {
                public required Service1 Service1 { private get; init; }
                public required Service3 Service3 { private get; init; }
            }

            public sealed class Service3 {
                public required Service1 Service1 { private get; init; }
                public required Service2 Service2 { private get; init; }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask FullTangle4() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<Service1>]
            [Singleton<Service2>]
            [Singleton<Service3>]
            [Singleton<Service4>]
            public sealed partial class TestProvider;


            public sealed class Service1 {
                public required Service2 Service2 { private get; init; }
                public required Service3 Service3 { private get; init; }
                public required Service4 Service4 { private get; init; }
            }

            public sealed class Service2 {
                public required Service1 Service1 { private get; init; }
                public required Service3 Service3 { private get; init; }
                public required Service4 Service4 { private get; init; }
            }

            public sealed class Service3 {
                public required Service1 Service1 { private get; init; }
                public required Service2 Service2 { private get; init; }
                public required Service4 Service4 { private get; init; }
            }

            public sealed class Service4 {
                public required Service1 Service1 { private get; init; }
                public required Service2 Service2 { private get; init; }
                public required Service3 Service3 { private get; init; }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask FullTangle5() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<Service1>]
            [Singleton<Service2>]
            [Singleton<Service3>]
            [Singleton<Service4>]
            [Singleton<Service5>]
            public sealed partial class TestProvider;


            public sealed class Service1 {
                public required Service2 Service2 { private get; init; }
                public required Service3 Service3 { private get; init; }
                public required Service4 Service4 { private get; init; }
                public required Service5 Service5 { private get; init; }
            }

            public sealed class Service2 {
                public required Service1 Service1 { private get; init; }
                public required Service3 Service3 { private get; init; }
                public required Service4 Service4 { private get; init; }
                public required Service5 Service5 { private get; init; }
            }

            public sealed class Service3 {
                public required Service1 Service1 { private get; init; }
                public required Service2 Service2 { private get; init; }
                public required Service4 Service4 { private get; init; }
                public required Service5 Service5 { private get; init; }
            }

            public sealed class Service4 {
                public required Service1 Service1 { private get; init; }
                public required Service2 Service2 { private get; init; }
                public required Service3 Service3 { private get; init; }
                public required Service5 Service5 { private get; init; }
            }

            public sealed class Service5 {
                public required Service1 Service1 { private get; init; }
                public required Service2 Service2 { private get; init; }
                public required Service3 Service3 { private get; init; }
                public required Service4 Service4 { private get; init; }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask CircularDependencyShortCircuit2() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<Service1>]
            [Singleton<Service2>]
            public sealed partial class TestProvider;


            public sealed class Service1 {
                public required Service2 Service2_1 { private get; init; }
                public required Service2 Service2_2 { private get; init; }
            }

            public sealed class Service2(Service1 Service1);

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask CircularDependencyShortCircuit3() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<Service1>]
            [Singleton<Service2>]
            [Singleton<Service3>]
            public sealed partial class TestProvider;


            public sealed class Service1 {
                public required Service2 Service2 { private get; init; }
                public required Service3 Service3 { private get; init; }
            }

            public sealed class Service2(Service3 Service3);

            public sealed class Service3(Service1 Service1);

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask CircularDependencyShortCircuitError() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<Service1>]
            [Singleton<Service2>]
            [Transient<Service3>]
            [Transient<Service4>]
            public sealed partial class TestProvider;


            public sealed class Service1 {
                public required Service2 Service2 { private get; init; }
                public required Service3 Service3 { private get; init; }
            }

            public sealed class Service2(Service3 Service3);

            public sealed class Service3(Service4 Service4);

            public sealed class Service4(Service1 Service1);

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI033");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Circular dependency unresolvable: ['Service1' -> 'Service3' -> 'Service4' -> 'Service1']. Only singleton and scoped dependencies injected as properties can be resolved circular");
    }
}
