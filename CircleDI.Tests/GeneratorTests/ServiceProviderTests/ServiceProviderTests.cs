using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Tests;

/// <summary>
/// Tests the ServiceProviderAttribute and ScopedProviderAttribute.
/// </summary>
public sealed class ServiceProviderTests {
    [Test]
    public async ValueTask NoServiceProviderAttributeGeneratesNoProvider() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            public sealed partial class Test;

            [ScopedProvider]
            public sealed partial class Test2;

            [Singleton<Test>]
            public sealed partial class Test3;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);

        foreach (string sourceText in sourceTexts.Where((string sourceText) => !sourceText.Contains("namespace System.Threading;\n")))
            await Assert.That(sourceText).Contains("namespace CircleDIAttributes;\n");
    }

    [Test]
    public async ValueTask EmptyServiceProviderAttributeGeneratesDefaultProvider() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask MissingPartialOnServiceProviderReportsError() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed class TestProvider;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI001");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Missing partial keyword");
    }

    [Test]
    public async ValueTask MissingPartialOnScopeProviderReportsError() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider {
                public sealed class Scope;
            }

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI002");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Missing partial keyword");
    }

    [Test]
    public async ValueTask InterfaceNameIServiceProviderError() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(InterfaceName = "IServiceProvider")]
            public sealed partial class TestProvider;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI005");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("InterfaceName 'IServiceProvider' is not allowed, it collides with 'System.IServiceProvider'");
    }

    [Test]
    public async ValueTask NameServiceProviderHasInterfaceNameIServiceprovider() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class ServiceProvider;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextInterface = sourceTexts[^1];

        await Assert.That(sourceTextInterface).Contains("public partial interface IServiceprovider");
    }


    [Test]
    public async ValueTask ServiceProviderGlobalNamespace() {
        const string input = """
            using CircleDIAttributes;

            [ServiceProvider]
            public sealed partial class TestProvider;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Assert.That(sourceTextClass).Contains("public sealed partial class TestProvider");
        await Assert.That(sourceTextClass).DoesNotContain("namespace");
        await Assert.That(sourceTextInterface).Contains("public partial interface ITestProvider");
        await Assert.That(sourceTextInterface).DoesNotContain("namespace");
    }

    [Test]
    public async ValueTask ServiceProviderNestedNamespace() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode {
                namespace Nested {
                    [ServiceProvider]
                    public sealed partial class TestProvider;
                }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Assert.That(sourceTextClass).Contains("namespace MyCode.Nested;");
        await Assert.That(sourceTextInterface).Contains("namespace MyCode.Nested;");
    }


    [Test]
    public async ValueTask ServiceProviderNestedType() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            public sealed class Wrapper {
                [ServiceProvider]
                public sealed partial class TestProvider;
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ServiceProviderManyNestedTypes() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            public sealed partial class Wrapper {
                private partial struct Data {
                    public partial interface Api {
                        [ServiceProvider]
                        public sealed partial class TestProvider;
                    }
                }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask ServiceProviderGeneric() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;
            [ServiceProvider]
            public sealed partial class TestProvider<T>;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopeProviderGeneric() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;
            [ServiceProvider]
            public sealed partial class TestProvider {
                public sealed partial class Scope<T>;
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask InterfaceGeneric() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;
            [ServiceProvider(InterfaceType = typeof(ITestProvider<>))]
            public sealed partial class TestProvider<T>;

            public partial interface ITestProvider<T>;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask InterfaceScopeGeneric() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;
            [ServiceProvider<ITestProvider>]
            public sealed partial class TestProvider {
                public sealed partial class Scope<T>;
            }

            public partial interface ITestProvider {
                public partial interface IScope<T>;
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask AllGeneric() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;
            [ServiceProvider(InterfaceType = typeof(ITestProvider<>))]
            public sealed partial class TestProvider<T1> {
                public sealed partial class Scope<T2>;
            }

            public partial interface ITestProvider<T1> {
                public partial interface IScope<T2>;
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask ServiceProviderInitServicesMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ISingleton, Singleton>]
            [Singleton<Singleton2>]
            public sealed partial class TestProvider {
                public TestProvider() {
                    InitServices();
                }
            }

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public sealed class Singleton2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ServiceProviderInitServicesMethodLazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(CreationTime = CreationTiming.Lazy)]
            [Singleton<ISingleton, Singleton>]
            [Singleton<Singleton2>]
            public sealed partial class TestProvider {
                public TestProvider() {
                    InitServices();
                }
            }

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public sealed class Singleton2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ScopedProviderInitServicesMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<IScoped, Scoped>]
            [Scoped<Scoped2>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope(ITestProvider testProvider) {
                        InitServices(testProvider);
                    }
                }
            }
            public partial interface ITestProvider;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public sealed class Scoped2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ScopedProviderInitServicesMethodLazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(CreationTime = CreationTiming.Lazy)]
            [Scoped<IScoped, Scoped>]
            [Scoped<Scoped2>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope(ITestProvider testProvider) {
                        InitServices(testProvider);
                    }
                }
            }
            public partial interface ITestProvider;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public sealed class Scoped2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask ScopedProviderParameterDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<IScoped, Scoped>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope(IScoped scoped) { }
                }
            }
            public partial interface ITestProvider;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ScopedProviderParameterProviderDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<IScoped, Scoped>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope([Dependency] IScoped scoped) { }
                }
            }
            public partial interface ITestProvider;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ScopedProviderPropertyDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<IScoped, Scoped>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public required IScoped Scoped { private get; init; }
                }
            }
            public partial interface ITestProvider;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ScopedProviderPropertyWithSetsRequiredMembers() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<IScoped, Scoped>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public required IScoped Scoped { private get; init; }

                    [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
                    public Scope() { }
                }
            }
            public partial interface ITestProvider;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ScopedProviderPropertyProviderDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<IScoped, Scoped>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    [Dependency]
                    public required IScoped Scoped { private get; init; }
                }
            }
            public partial interface ITestProvider;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ScopedProviderParameterPropertyAndProviderDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<IService1, Service1>]
            [Transient<IService2, Service2>]
            [Transient<IService3, Service3>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    [Dependency]
                    public required IService2 Service2 { private get; init; }

                    public required IService3 Service3 { private get; init; }

                    public Scope([Dependency] ITestProvider serviceProvider, IService1 service1) {
                        InitServices(serviceProvider);
                    }
                }
            }
            public partial interface ITestProvider;

            public interface IService1;
            public sealed class Service1 : IService1;

            public interface IService2;
            public sealed class Service2 : IService2;

            public interface IService3;
            public sealed class Service3 : IService3;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask ScopedProviderDependencyInjectionParameter() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService1, TestService1>]
            [Transient<ITestService2, TestService2>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope(ITestProvider testProvider, ITestService1 testService1, ITestService2 testService2) {
                        InitServices(testProvider);
                    }
                }
            }
            public partial interface ITestProvider;

            public interface ITestService1;
            public sealed class TestService1 : ITestService1;

            public interface ITestService2;
            public sealed class TestService2 : ITestService2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ScopedProviderDependencyInjectionProperty() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService1, TestService1>]
            [Transient<ITestService2, TestService2>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public required ITestService1 TestService1 { private get; init; }
                    public required ITestService2 TestService2 { private get; init; }

                    public Scope(ITestProvider testProvider) {
                        InitServices(testProvider);
                    }
                }
            }
            public partial interface ITestProvider;

            public interface ITestService1;
            public sealed class TestService1 : ITestService1;

            public interface ITestService2;
            public sealed class TestService2 : ITestService2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask ScopedProviderDependencyInjectionParameterProperty() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService1, TestService1>]
            [Transient<ITestService2, TestService2>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public required ITestService1 TestService3 { private get; init; }
                    public required ITestService2 TestService4 { private get; init; }

                    public Scope(ITestProvider testProvider, ITestService1 testService1, ITestService2 testService2) {
                        InitServices(testProvider);
                    }
                }
            }
            public partial interface ITestProvider;

            public interface ITestService1;
            public sealed class TestService1 : ITestService1;

            public interface ITestService2;
            public sealed class TestService2 : ITestService2;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask ScopedProviderDependencyInjectionNotRegisteredFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope([Dependency] ITestService testService) {
                        InitServices();
                    }
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI038");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Unregistered dependency at 'MyCode.TestProvider.Scope' with type 'MyCode.ITestService'");
    }

    [Test]
    public async ValueTask ScopedProviderDependencyInjectionAmbiguousFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Name = "Single")]
            [Transient<ITestService, TestService>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope([Dependency] ITestService testService) {
                        InitServices();
                    }
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI039");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Ambiguous dependency at 'MyCode.TestProvider.Scope' with type 'MyCode.ITestService': There are multiple Services registered for this type: [\"Single\", \"TestService\"]. Use the '[Dependency(Name=\"...\")]'-attribute on the parameter to choose one specific service");
    }

    [Test]
    public async ValueTask ScopedProviderDependencyInjectionNotNamedRegisteredFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope([Dependency(Name = "Single")] ITestService testService) {
                        InitServices();
                    }
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI040");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Unregistered named dependency at 'MyCode.TestProvider.Scope' with name \"Single\"");
    }

    [Test]
    public async ValueTask ScopedProviderDependencyInjectionScopedFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope([Dependency] ITestService testService) {
                        InitServices(testService);
                    }
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI041");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Lifetime Violation: ScopedProvider 'MyCode.TestProvider.Scope' has Scoped dependency 'MyCode.ITestService'");
    }

    [Test]
    public async ValueTask ScopedProviderDependencyInjectionTransientScopedFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService1, TestService1>]
            [Scoped<ITestService2, TestService2>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope([Dependency] ITestService1 testService1) { }
                }
            }

            public interface ITestService1;
            public sealed class TestService1(ITestService2 testService2) : ITestService1;

            public interface ITestService2;
            public sealed class TestService2 : ITestService2;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI042");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Lifetime Violation: ScopedProvider 'MyCode.TestProvider.Scope' has Transient-Scoped dependency 'MyCode.ITestService1'. \"Transient-Scoped\" means the service itself is transient, but it has at least one dependency or one dependency of the dependencies that is Scoped");
    }

    [Test]
    public async ValueTask ScopedProviderDependencyInjectionDelegateScopedFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<System.Action>(nameof(ScopedMethod))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope([Dependency] System.Action scopedMethod) { }

                    private static void ScopedMethod() { }
                }
            }

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI043");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Lifetime Violation: ScopedProvider 'MyCode.TestProvider.Scope' has Delegate-Scoped dependency 'System.Action'. \"Delegate-Scoped\" means the method is declared inside Scope and therefore only available for scoped services.");
    }


    [Test]
    public async ValueTask AttributeServiceProviderWithDifferentInterfaceName() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(InterfaceName = "IOther")]
            public sealed partial class TestProvider;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask AttributeServiceProviderWithEmptyInterfaceName() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(InterfaceName = "")]
            [Singleton<TestProvider>(Name = "Me", Implementation = "this")]
            [Scoped<TestProvider.Scope>(Name = "MeScope", Implementation = "this")]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope([Dependency] TestProvider testProvider) {
                        InitServices(testProvider);
                    }
                }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^1];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask AttributeServiceProviderWithInterfaceType() {
        const string input = """
            using CircleDIAttributes;

            namespace MySpace {
                [ServiceProvider(InterfaceType = typeof(Interface.IWrapper.IProvider))]
                public sealed partial class MyProvider;
            }

            namespace MySpace.Interface {
                public partial interface IWrapper {
                    internal partial interface IProvider {
                        protected internal partial interface IScope;
                    }
                }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask AttributeServiceProviderWithInterfaceTypeParameter() {
        const string input = """
            using CircleDIAttributes;

            namespace MySpace {
                [ServiceProvider<Interface.IWrapper.IProvider>]
                public sealed partial class MyProvider;
            }

            namespace MySpace.Interface {
                public partial interface IWrapper {
                    internal partial interface IProvider {
                        public partial interface IScope;
                    }
                }
            }

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask AttributeServiceProviderWithInterfaceTypeAndInterfaceNameError() {
        const string input = """
            using CircleDIAttributes;

            namespace MySpace {
                [ServiceProvider(InterfaceType = typeof(Interface.IWrapper.IProvider), InterfaceName = "IProvider")]
                public sealed partial class MyProvider;
            }

            namespace MySpace.Interface {
                public partial interface IWrapper {
                    internal partial interface IProvider {
                        protected internal partial interface IScope;
                    }
                }
            }

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI004");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("InterfaceType and InterfaceName are not compatible, at most one property must be set.");
    }


    [Test]
    public async ValueTask AttributeServiceProviderWithCreationTimeLazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(CreationTime = CreationTiming.Lazy)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask AttributeServiceProviderWithGetAccessorMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(GetAccessor = GetAccess.Method)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask AttributeServiceProviderWithGetAccessorMethodAndLazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(CreationTime = CreationTiming.Lazy, GetAccessor = GetAccess.Method)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask AttributeServiceProviderWithThreadSafeFalse() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(ThreadSafe = false)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask AttributeServiceProviderWithThreadSafeFalseAndCreationTimeLazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(ThreadSafe = false, CreationTime = CreationTiming.Lazy)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask AttributeServiceProviderWithThreadSafeFalseAndGetAccessorMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(ThreadSafe = false, GetAccessor = GetAccess.Method)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask AttributeServiceProviderWithThreadSafeFalseAndGetAccessorMethodAndLazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(ThreadSafe = false, GetAccessor = GetAccess.Method, CreationTime = CreationTiming.Lazy)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask AttributeScopeProviderNotGenerated() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(Generate = false)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask AttributeScopeProviderWithCreationTimeLazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(CreationTime = CreationTiming.Lazy)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask AttributeScopeProviderWithGetAccessorMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(GetAccessor = GetAccess.Method)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask AttributeScopeProviderWithGetAccessorMethodAndLazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(CreationTime = CreationTiming.Lazy, GetAccessor = GetAccess.Method)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask AttributeScopeProviderWithThreadSafeFalse() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(ThreadSafe = false)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask AttributeScopeProviderWithThreadSafeFalseAndCreationTimeLazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(ThreadSafe = false, CreationTime = CreationTiming.Lazy)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask AttributeScopeProviderWithThreadSafeFalseAndGetAccessorMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(ThreadSafe = false, GetAccessor = GetAccess.Method)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }

    [Test]
    public async ValueTask AttributeScopeProviderWithThreadSafeFalseAndGetAccessorMethodAndLazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(ThreadSafe = false, GetAccessor = GetAccess.Method, CreationTime = CreationTiming.Lazy)]
            [Singleton<ISingleton, Singleton>]
            [Scoped<IScoped, Scoped>]
            [Transient<ITransient, Transient>]
            public sealed partial class TestProvider;

            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface ITransient;
            public sealed class Transient : ITransient;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];

        await Verify(sourceTextClass);
    }


    [Test]
    public async ValueTask AttributeServiceProviderWithNoDiposeGeneration() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(GenerateDisposeMethods = DisposeGeneration.NoDisposing)]
            [Transient<IServiceDispose, ServiceDispose>]
            [Transient<IServiceDisposeAsync, ServiceDisposeAsync>]
            public sealed partial class TestProvider;

            public interface IServiceDispose;
            public sealed class ServiceDispose : IServiceDispose, IDisposable;

            public interface IServiceDisposeAsync;
            public sealed class ServiceDisposeAsync : IServiceDisposeAsync, IAsyncDisposable;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask AttributeServiceProviderWithDiposeOnlyGeneration() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(GenerateDisposeMethods = DisposeGeneration.DisposeOnly)]
            [Transient<IServiceDispose, ServiceDispose>]
            [Transient<IServiceDisposeAsync, ServiceDisposeAsync>]
            public sealed partial class TestProvider;

            public interface IServiceDispose;
            public sealed class ServiceDispose : IServiceDispose, System.IDisposable;

            public interface IServiceDisposeAsync;
            public sealed class ServiceDisposeAsync : IServiceDisposeAsync, System.IAsyncDisposable;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask AttributeServiceProviderWithDiposeAsyncOnlyGeneration() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(GenerateDisposeMethods = DisposeGeneration.DisposeAsyncOnly)]
            [Transient<IServiceDispose, ServiceDispose>]
            [Transient<IServiceDisposeAsync, ServiceDisposeAsync>]
            public sealed partial class TestProvider;

            public interface IServiceDispose;
            public sealed class ServiceDispose : IServiceDispose, System.IDisposable;

            public interface IServiceDisposeAsync;
            public sealed class ServiceDisposeAsync : IServiceDisposeAsync, System.IAsyncDisposable;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask AttributeServiceProviderWithDiposeBothGeneration() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(GenerateDisposeMethods = DisposeGeneration.GenerateBoth)]
            [Transient<IServiceDispose, ServiceDispose>]
            [Transient<IServiceDisposeAsync, ServiceDisposeAsync>]
            public sealed partial class TestProvider;

            public interface IServiceDispose;
            public sealed class ServiceDispose : IServiceDispose, System.IDisposable;

            public interface IServiceDisposeAsync;
            public sealed class ServiceDisposeAsync : IServiceDisposeAsync, System.IAsyncDisposable;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask AttributeScopeProviderWithNoDiposeGeneration() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(GenerateDisposeMethods = DisposeGeneration.NoDisposing)]
            [Transient<IServiceDispose, ServiceDispose>]
            [Transient<IServiceDisposeAsync, ServiceDisposeAsync>]
            public sealed partial class TestProvider;

            public interface IServiceDispose;
            public sealed class ServiceDispose : IServiceDispose, System.IDisposable;

            public interface IServiceDisposeAsync;
            public sealed class ServiceDisposeAsync : IServiceDisposeAsync, System.IAsyncDisposable;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask AttributeScopeProviderWithDiposeOnlyGeneration() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(GenerateDisposeMethods = DisposeGeneration.DisposeOnly)]
            [Transient<IServiceDispose, ServiceDispose>]
            [Transient<IServiceDisposeAsync, ServiceDisposeAsync>]
            public sealed partial class TestProvider;

            public interface IServiceDispose;
            public sealed class ServiceDispose : IServiceDispose, System.IDisposable;

            public interface IServiceDisposeAsync;
            public sealed class ServiceDisposeAsync : IServiceDisposeAsync, System.IAsyncDisposable;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask AttributeScopeProviderWithDiposeAsyncOnlyGeneration() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(GenerateDisposeMethods = DisposeGeneration.DisposeAsyncOnly)]
            [Transient<IServiceDispose, ServiceDispose>]
            [Transient<IServiceDisposeAsync, ServiceDisposeAsync>]
            public sealed partial class TestProvider;

            public interface IServiceDispose;
            public sealed class ServiceDispose : IServiceDispose, System.IDisposable;

            public interface IServiceDisposeAsync;
            public sealed class ServiceDisposeAsync : IServiceDisposeAsync, System.IAsyncDisposable;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask AttributeScopeProviderWithDiposeBothGeneration() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(GenerateDisposeMethods = DisposeGeneration.GenerateBoth)]
            [Transient<IServiceDispose, ServiceDispose>]
            [Transient<IServiceDisposeAsync, ServiceDisposeAsync>]
            public sealed partial class TestProvider;

            public interface IServiceDispose;
            public sealed class ServiceDispose : IServiceDispose, System.IDisposable;

            public interface IServiceDisposeAsync;
            public sealed class ServiceDisposeAsync : IServiceDisposeAsync, System.IAsyncDisposable;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask AttributeScopeProviderAlsoWorkingOnScopeClass() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>]
            public sealed partial class TestProvider {
                [ScopedProvider(Generate = false)]
                public sealed partial class Scope;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];


        await Assert.That(sourceTextClass).Contains("public sealed partial class TestProvider");
        await Assert.That(sourceTextClass).DoesNotContain("Scope ");
        await Assert.That(sourceTextClass).DoesNotContain("CreateScope");

        await Assert.That(sourceTextInterface).Contains("public partial interface ITestProvider");
        await Assert.That(sourceTextInterface).DoesNotContain("IScope ");
        await Assert.That(sourceTextInterface).DoesNotContain("CreateScope");
    }

    [Test]
    public async ValueTask AttributeScopeProviderReportsErrorWhenUsedTwice() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(Generate = false)]
            [Scoped<ITestService, TestService>]
            public sealed partial class TestProvider {
                [ScopedProvider(Generate = false)]
                public sealed partial class Scope;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI003");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Double ScopedProviderAttribute is not allowed, put either one on the ServiceProvider or ScopedProvider, but not both");
    }


    [Test]
    public async ValueTask FullExample() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ISingleton, Singleton>]
            [Singleton<ISingletonDisposable, SingletonDisposable>]
            [Singleton<ISingletonAsyncDisposable, SingletonAsyncDisposable>]
            [Scoped<IScoped, Scoped>]
            [Scoped<IScopedDisposable, ScopedDisposable>]
            [Scoped<IScopedAsyncDisposable, ScopedAsyncDisposable>]
            [Transient<ITransient, Transient>]
            [Transient<ITransientDisposable, TransientDisposable>]
            [Transient<ITransientAsyncDisposable, TransientAsyncDisposable>]
            [Singleton<ISingletonDependency1, SingletonDependency1>]
            [Singleton<ISingletonDependency2, SingletonDependency2>]
            [Scoped<IScopedDependency1, ScopedDependency1>]
            [Scoped<IScopedDependency2, ScopedDependency2>]
            [Delegate<MyDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                private static void DelegateImpl() { }
            }


            public interface ISingleton;
            public sealed class Singleton : ISingleton;

            public interface ISingletonDisposable;
            public sealed class SingletonDisposable : ISingletonDisposable, System.IDisposable;

            public interface ISingletonAsyncDisposable : System.IAsyncDisposable;
            public sealed class SingletonAsyncDisposable : ISingletonAsyncDisposable;


            public interface IScoped;
            public sealed class Scoped : IScoped;

            public interface IScopedDisposable : System.IDisposable;
            public sealed class ScopedDisposable : IScopedDisposable;

            public interface IScopedAsyncDisposable : System.IAsyncDisposable;
            public sealed class ScopedAsyncDisposable : IScopedAsyncDisposable;


            public interface ITransient;
            public sealed class Transient : ITransient;

            public interface ITransientDisposable : System.IDisposable;
            public sealed class TransientDisposable : ITransientDisposable;

            public interface ITransientAsyncDisposable : System.IAsyncDisposable;
            public sealed class TransientAsyncDisposable : ITransientAsyncDisposable;


            public interface ISingletonDependency1;
            public sealed class SingletonDependency1(ISingletonDependency2 singletonDependency2) : ISingletonDependency1;

            public interface ISingletonDependency2;
            public sealed class SingletonDependency2 : ISingletonDependency2 {
                public required ISingletonDependency1 SingletonDependency1 { private get; init; }
            }


            public interface IScopedDependency1;
            public sealed class ScopedDependency1(IScopedDependency2 scopedDependency2) : IScopedDependency1;

            public interface IScopedDependency2;
            public sealed class ScopedDependency2 : IScopedDependency2 {
                public required IScopedDependency1 ScopedDependency1 { private get; init; }
            }


            public delegate void MyDelegate();

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }
}
