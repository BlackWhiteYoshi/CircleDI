using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Tests;

/// <summary>
/// Tests registering generic services (types that have at least 1 unbound type parameter).
/// </summary>
public sealed class GenericServicesTests {
    [Test]
    public async ValueTask WithoutDependency_NotGenerated() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<>))]
            public sealed partial class TestProvider;

            public sealed class TestService<T>;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask GenericService() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<>))]
            [Singleton<TestService>]
            public sealed partial class TestProvider;

            public sealed class TestService<T>;
            public sealed class TestService(TestService<int> ti);

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask MultipleServices() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<>))]
            [Singleton<TestService>]
            public sealed partial class TestProvider;

            public sealed class TestService<T>;
            public sealed class TestService(TestService<int> ti, TestService<float> tf, TestService<string> ts);

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ServiceMultipleTypeParameters() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<,,>))]
            [Singleton<TestService>]
            public sealed partial class TestProvider;

            public sealed class TestService<T, U, V>;
            public sealed class TestService(TestService<int, float, string> t);

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask ServiceWithConstrucorDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<>))]
            [Singleton<TestService>]
            public sealed partial class TestProvider;


            public sealed class TestService<T>(TestService TestService);

            public sealed class TestService() {
                public required TestService<int> Ti { private get; init; }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ServiceWithPropertyDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<>))]
            [Singleton<TestService>]
            public sealed partial class TestProvider;


            public sealed class TestService<T> {
                public required TestService MyService { private get; init; }
            }

            public sealed class TestService(TestService<int> ti);

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ServiceWithClosedGenericDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<>))]
            [Singleton<TestService>]
            public sealed partial class TestProvider;


            public sealed class TestService<T> {
                public required TestService<int> MyService { private get; init; }
            }

            public sealed class TestService(TestService<int> ti);

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ServiceWithMultipleClosedGenericDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(GenericTestService<,,>))]
            [Singleton(typeof(TestService<>))]
            [Singleton<TestService>]
            public sealed partial class TestProvider;

            public sealed class GenericTestService<T, U, V>;
            public sealed class TestService<T>(GenericTestService<float, long, byte> genericTestService);
            public sealed class TestService(TestService<int> ti);

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ServiceWithOpenGenericDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<>))]
            [Singleton<TestService>]
            public sealed partial class TestProvider;


            public sealed class TestService<T> {
                public required TestService<T> MyService { private get; init; }
            }

            public sealed class TestService(TestService<int> ti);

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ServiceWithMultipleOpenGenericDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(GenericTestService<,,>))]
            [Singleton(typeof(TestService<,,>))]
            [Singleton<TestService>]
            public sealed partial class TestProvider;

            public sealed class GenericTestService<T, U, V>;
            public sealed class TestService<T, U, V>(GenericTestService<V, U, T> genericTestService);
            public sealed class TestService(TestService<int, float, string> ti);

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ServiceWithOpenGenericMismatchCount() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(ITestService<,>), typeof(TestService<,,>))]
            public sealed partial class TestProvider;

            public interface ITestService<T, U>;
            public sealed class TestService<T, U, V> : ITestService<T, U>;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI010");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Service registration type parameter mismatch at service 'MyCode.ITestService<T, U>' with implementation 'MyCode.TestService<T, U, V>'. The number of type parameters must match and the type parameters must be open/unbound.");
    }

    [Test]
    public async ValueTask ServiceWithOpenGenericMismatchType() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(ITestService<int, string,>), typeof(TestService<int, float,>))]
            public sealed partial class TestProvider;

            public interface ITestService<T, U, V>;
            public sealed class TestService<T, U, V> : ITestService<T, U, V>;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI010");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Service registration type parameter mismatch at service 'MyCode.ITestService<global::System.Int32, global::System.String, V>' with implementation 'MyCode.TestService<global::System.Int32, global::System.Single, V>'. The number of type parameters must match and the type parameters must be open/unbound.");
    }


    [Test]
    public async ValueTask ServiceWithImplementationBase() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<>))]
            [Singleton<TestService>]
            public sealed partial class TestProvider;

            public sealed class TestService<T> : MyBase;
            public sealed class TestService(TestService<int> ti);

            public abstract class MyBase {
                public required TestService MyService { private get; init; }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ServiceWithGenericImplementationBase() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<>))]
            [Singleton<TestService>]
            public sealed partial class TestProvider;

            public sealed class TestService<T> : MyBase<T>;
            public sealed class TestService(TestService<int> ti);

            public abstract class MyBase<T> {
                public required TestService MyService { private get; init; }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ServiceWithMultipleGenericsImplementationBase() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<,,>))]
            [Singleton<TestService>]
            public sealed partial class TestProvider;

            public sealed class TestService<T, U, V> : MyBase<V, U, T>;
            public sealed class TestService(TestService<int, float, string> ti);

            public abstract class MyBase<T, U, V> {
                public required TestService MyService { private get; init; }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask ServiceWithImplementation() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<>), Implementation = nameof(CreateGenericTestService))]
            [Singleton<TestService>]
            public sealed partial class TestProvider {
                private TestService<T> CreateGenericTestService<T>() => new TestService<T>();
            }

            public sealed class TestService<T>;
            public sealed class TestService(TestService<int> ti);

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ServiceDelegate() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate(typeof(GenericFunction<>), nameof(TheFunction))]
            [Singleton<TestService>]
            public sealed partial class TestProvider {
                private T TheFunction<T>(T t) => t;
            }

            public delegate T GenericFunction<T>(T t);
            public sealed class TestService(GenericFunction<int> function);

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask ServiceImplementationField() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<>), Implementation = nameof(testService))]
            public sealed partial class TestProvider<T> {
                private static TestService<T> testService = new();
            }

            public sealed class TestService<T>;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI016");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Implementation 'testService' for type 'MyCode.TestService<T>' must be a generic method with '1' type parameter.");
    }

    [Test]
    public async ValueTask ServiceImplementationProperty() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<,>), Implementation = nameof(TestService))]
            public sealed partial class TestProvider<T, U> {
                private static TestService<T, U> TestService => new();
            }

            public sealed class TestService<T, U>;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI016");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Implementation 'TestService' for type 'MyCode.TestService<T, U>' must be a generic method with '2' type parameters.");
    }

    [Test]
    public async ValueTask GenericServiceImplementationTypeParameterMismatch() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService<>), Implementation = nameof(CreateTestService))]
            public sealed partial class TestProvider {
                public static TestService<T> CreateTestService() => new();
            }

            public sealed class TestService<T>;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI018");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Implementation Method 'CreateTestService' has the wrong number of type parameters: '0' <-> '1' expected");
    }

    [Test]
    public async ValueTask GenericImplementationTypeParameterMismatch() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService>(Implementation = nameof(CreateTestService))]
            public sealed partial class TestProvider {
                public static TestService CreateTestService<T>() => new();
            }

            public sealed class TestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI018");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Implementation Method 'CreateTestService' has the wrong number of type parameters: '1' <-> '0' expected");
    }

    [Test]
    public async ValueTask GenericDelegateServiceTypeParameterMismatch() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate(typeof(MyMethod<>), nameof(CreateTestService))]
            public sealed partial class TestProvider {
                public static void CreateTestService() { }
            }

            public delegate void MyMethod<T>();

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI024");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Method 'CreateTestService' has the wrong number of type parameters: '0' <-> '1' expected");
    }

    [Test]
    public async ValueTask GenericDelegateMethodTypeParameterMismatch() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<MyMethod>(nameof(CreateTestService))]
            public sealed partial class TestProvider {
                public static void CreateTestService<T>() { }
            }

            public delegate void MyMethod();

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI024");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Method 'CreateTestService' has the wrong number of type parameters: '1' <-> '0' expected");
    }
}
