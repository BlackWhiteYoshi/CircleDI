using CircleDI.Tests.GenerateSourceText;

namespace CircleDI.Tests;

/// <summary>
/// <para>Tests for the dispose logic:</para>
/// <para>
/// - Dispose list<br />
/// - AsyncDispose list<br />
/// - Dispose function<br />
/// - AsyncDispose function
/// </para>
/// </summary>
public sealed class DisposeTests {
    [Test]
    public async ValueTask Singleton() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService : System.IDisposable;
            public sealed class TestService : ITestService;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask Scope() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService : System.IDisposable;
            public sealed class TestService : ITestService;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask Transient() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService : System.IDisposable;
            public sealed class TestService : ITestService;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask AsyncSingleton() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService : System.IAsyncDisposable;
            public sealed class TestService : ITestService;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask AsyncScope() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService : System.IAsyncDisposable;
            public sealed class TestService : ITestService;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask AsyncTransient() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService : System.IAsyncDisposable;
            public sealed class TestService : ITestService;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask SyncAndAsyncSingleton() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService : System.IDisposable, System.IAsyncDisposable;
            public sealed class TestService : ITestService;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask SyncAndAsyncScope() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService : System.IDisposable, System.IAsyncDisposable;
            public sealed class TestService : ITestService;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask SyncAndAsyncTransient() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService : System.IDisposable, System.IAsyncDisposable;
            public sealed class TestService : ITestService;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask CustomDisposeMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider {
                public void Dispose() {
                    DisposeServices();
                }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask CustomDisposeAsyncMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider {
                public ValueTask DisposeAsync() {
                    return DisposeServicesAsync();
                }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask CustomDisposeMethodScope() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public void Dispose() {
                        DisposeServices();
                    }
                }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask CustomDisposeAsyncMethodScope() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public ValueTask DisposeAsync() {
                        return DisposeServicesAsync();
                    }
                }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask NoDisposableServices() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask NoDisposeProperty() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(NoDispose = true)]
            [Transient<ITestDisposable, TestDisposable>(NoDispose = true)]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

            public interface ITestDisposable;
            public sealed class TestDisposable : ITestDisposable, IDisposable;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask MultipleSingletons() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestService2, TestService2>]
            public sealed partial class TestProvider;

            public interface ITestService : System.IDisposable, System.IAsyncDisposable;
            public sealed class TestService : ITestService;

            public interface ITestService2 : System.IDisposable, System.IAsyncDisposable;
            public sealed class TestService2 : ITestService2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask SingletonAndTransient() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Transient<ITestService2, TestService2>]
            public sealed partial class TestProvider;

            public interface ITestService : System.IDisposable, System.IAsyncDisposable;
            public sealed class TestService : ITestService;

            public interface ITestService2 : System.IDisposable, System.IAsyncDisposable;
            public sealed class TestService2 : ITestService2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask LazySingleton() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(CreationTime = CreationTiming.Lazy)]
            public sealed partial class TestProvider;

            public interface ITestService : System.IAsyncDisposable;
            public sealed class TestService : ITestService;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask LazyMultipleSingleton() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(CreationTime = CreationTiming.Lazy)]
            [Singleton<ITestService2, TestService2>(CreationTime = CreationTiming.Lazy)]
            public sealed partial class TestProvider;

            public interface ITestService : System.IAsyncDisposable;
            public sealed class TestService : ITestService;

            public interface ITestService2 : System.IAsyncDisposable;
            public sealed class TestService2 : ITestService2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask TransientWithPropertyDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService1, TestService1>]
            [Transient<ITestService2, TestService2>]
            public sealed partial class TestProvider;

            public interface ITestService1 : System.IDisposable;
            public sealed class TestService1 : ITestService1 {
                public required ITestService2 TestService2 { private get; init; }
            }

            public interface ITestService2 : System.IDisposable;
            public sealed class TestService2 : ITestService2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask TransientWithPropertyDependencyAndGetAccessorMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService1, TestService1>(GetAccessor = GetAccess.Method)]
            [Transient<ITestService2, TestService2>]
            public sealed partial class TestProvider;

            public interface ITestService1 : System.IDisposable;
            public sealed class TestService1 : ITestService1 {
                public required ITestService2 TestService2 { private get; init; }
            }

            public interface ITestService2 : System.IDisposable;
            public sealed class TestService2 : ITestService2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask SyncAndAsyncNotThreadSafeHasList() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(ThreadSafe = false)]
            [Transient<ITestService, TestService>]
            [Transient<ITestService2, TestService2>]
            public sealed partial class TestProvider;

            public interface ITestService : System.IDisposable;
            public sealed class TestService : ITestService;

            public interface ITestService2 : System.IAsyncDisposable;
            public sealed class TestService2 : ITestService2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask TransientScopeGenerateListOnlyInScopedProvider() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(ThreadSafe = false)]
            [Transient<ITestService, TestService>]
            [Scoped<ITestService2, TestService2>]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService(ITestService2 testService2) : ITestService, System.IDisposable;

            public interface ITestService2;
            public sealed class TestService2 : ITestService2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask ManyServices() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]

            [Singleton<ITestService1, TestService1>]
            [Singleton<ITestService2, TestService2>]
            [Singleton<ITestService3, TestService3>]
            [Singleton<ITestService4, TestService4>]
            [Singleton<ITestService5, TestService5>]
            [Singleton<ITestService6, TestService6>]

            [Transient<ITestService7, TestService7>]
            [Transient<ITestService8, TestService8>]
            [Transient<ITestService9, TestService9>]
            [Transient<ITestService10, TestService10>]
            [Transient<ITestService11, TestService11>]
            [Transient<ITestService12, TestService12>]
            public sealed partial class TestProvider;


            public interface ITestService1 : System.IDisposable;
            public sealed class TestService1 : ITestService1;

            public interface ITestService2 : System.IDisposable;
            public sealed class TestService2 : ITestService2;

            public interface ITestService3 : System.IAsyncDisposable;
            public sealed class TestService3 : ITestService3;

            public interface ITestService4 : System.IAsyncDisposable;
            public sealed class TestService4 : ITestService4;

            public interface ITestService5 : System.IDisposable, System.IAsyncDisposable;
            public sealed class TestService5 : ITestService5;

            public interface ITestService6 : System.IDisposable, System.IAsyncDisposable;
            public sealed class TestService6 : ITestService6;


            public interface ITestService7 : System.IDisposable;
            public sealed class TestService7 : ITestService7;

            public interface ITestService8 : System.IDisposable;
            public sealed class TestService8 : ITestService8;

            public interface ITestService9 : System.IAsyncDisposable;
            public sealed class TestService9 : ITestService9;

            public interface ITestService10 : System.IAsyncDisposable;
            public sealed class TestService10 : ITestService10;

            public interface ITestService11 : System.IDisposable, System.IAsyncDisposable;
            public sealed class TestService11 : ITestService11;

            public interface ITestService12 : System.IDisposable, System.IAsyncDisposable;
            public sealed class TestService12 : ITestService12;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }
}
