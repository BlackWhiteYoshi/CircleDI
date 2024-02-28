using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Tests;

/// <summary>
/// Tests the ServiceProviderAttribute and ScopedProviderAttribute.
/// </summary>
public sealed class ServiceProviderTests {
    [Fact]
    public void NoServiceProviderAttributeGeneratesNoProvider() {
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

        foreach (string sourceText in sourceTexts)
            Assert.Contains("namespace CircleDIAttributes;\n", sourceText);
    }

    [Fact]
    public Task EmptyServiceProviderAttributeGeneratesDefaultProvider() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Fact]
    public void MissingPartialOnServiceProviderReportsError() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            public sealed class TestProvider;
            
            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI001", diagnostics[0].Id);
        Assert.Equal("Missing partial keyword", diagnostics[0].GetMessage());
    }

    [Fact]
    public void MissingPartialOnScopeProviderReportsError() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider {
                public sealed class Scope;
            }
            
            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);
        Assert.Single(diagnostics);
        Assert.Equal("CDI002", diagnostics[0].Id);
        Assert.Equal("Missing partial keyword", diagnostics[0].GetMessage());
    }


    [Fact]
    public void ServiceProviderGlobalNamespace() {
        const string input = """
            using CircleDIAttributes;
            
            [ServiceProvider]
            public sealed partial class TestProvider;

            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        Assert.Contains("public sealed partial class TestProvider", sourceTextClass);
        Assert.DoesNotContain("namespace", sourceTextClass);
        Assert.Contains("public partial interface ITestProvider", sourceTextInterface);
        Assert.DoesNotContain("namespace", sourceTextInterface);
    }

    [Fact]
    public void ServiceProviderNestedNamespace() {
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

        Assert.Contains("namespace MyCode.Nested;", sourceTextClass);
        Assert.Contains("namespace MyCode.Nested;", sourceTextInterface);
    }


    [Fact]
    public Task ServiceProviderInitServicesMethod() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task ServiceProviderInitServicesMethodLazy() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task ScopedProviderInitServicesMethod() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task ScopedProviderInitServicesMethodLazy() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task ScopedProviderRequiredPropertiesWithoutCustomConstructor() {
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

        return Verify(sourceTextClass);
    }


    [Fact]
    public Task ScopedProviderDependencyInjectionParameter() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task ScopedProviderDependencyInjectionProperty() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task ScopedProviderDependencyInjectionParameterProperty() {
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

        return Verify(sourceTextClass);
    }


    [Fact]
    public void ScopedProviderDependencyInjectionNotRegisteredFails() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;
            
            [ServiceProvider]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope(ITestService testService) {
                        InitServices();
                    }
                }
            }
            
            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI030", diagnostics[0].Id);
        Assert.Equal("Unregistered dependency at 'MyCode.TestProvider.Scope' with type 'MyCode.ITestService'", diagnostics[0].GetMessage());
    }

    [Fact]
    public void ScopedProviderDependencyInjectionAmbiguousFails() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;
            
            [ServiceProvider]
            [Singleton<ITestService, TestService>(Name = "Single")]
            [Transient<ITestService, TestService>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope(ITestService testService) {
                        InitServices();
                    }
                }
            }
            
            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI031", diagnostics[0].Id);
        Assert.Equal("Ambiguous dependency at 'MyCode.TestProvider.Scope' with type 'MyCode.ITestService': There are multiple Services registered for this type: [\"Single\", \"TestService\"]. Use the '[Dependency(Name=\"...\")]'-attribute on the parameter to choose one specific service", diagnostics[0].GetMessage());
    }

    [Fact]
    public void ScopedProviderDependencyInjectionNotNamedRegisteredFails() {
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

        Assert.Single(diagnostics);
        Assert.Equal("CDI032", diagnostics[0].Id);
        Assert.Equal("Unregistered named dependency at 'MyCode.TestProvider.Scope' with name \"Single\"", diagnostics[0].GetMessage());
    }

    [Fact]
    public void ScopedProviderDependencyInjectionScopedFails() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;
            
            [ServiceProvider]
            [Scoped<ITestService, TestService>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public Scope(ITestService testService) {
                        InitServices();
                    }
                }
            }
            
            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI033", diagnostics[0].Id);
        Assert.Equal("Lifetime Violation: ScopedProvider 'MyCode.TestProvider.Scope' has Scoped dependency 'MyCode.ITestService'", diagnostics[0].GetMessage());
    }

    [Fact]
    public void ScopedProviderDependencyInjectionTransientScopedFails() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;
            
            [ServiceProvider]
            [Transient<ITestService1, TestService1>]
            [Scoped<ITestService2, TestService2>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    public required TestProvider Asdf { private get; init; }

                    public required ITestService1 TestService1 { private get; init; }
                    public required ITestService2 TestService2 { private get; init; }

                    public Scope(ITestService1 testService1, ITestService2 testService2) {
                        InitServices();
                    }
                }
            }
            
            public interface ITestService1;
            public sealed class TestService1(ITestService2 testService2) : ITestService1;
            
            public interface ITestService2;
            public sealed class TestService2 : ITestService2;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI034", diagnostics[0].Id);
        Assert.Equal("Lifetime Violation: ScopedProvider 'MyCode.TestProvider.Scope' has Transient-Scoped dependency 'MyCode.ITestService1'. \"Transient-Scoped\" means the service itself is transient, but it has at least one dependency or one dependency of the dependencies that is Scoped", diagnostics[0].GetMessage());
    }


    [Fact]
    public Task AttributeServiceProviderWithDifferentInterfaceName() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider(InterfaceName = "IOther")]
            public sealed partial class TestProvider;
            
            """;

        string[] sourceTexts = input.GenerateSourceText(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public Task AttributeServiceProviderWithCreationTimeLazy() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task AttributeServiceProviderWithGetAccessorMethod() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task AttributeServiceProviderWithGetAccessorMethodAndLazy() {
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

        return Verify(sourceTextClass);
    }


    [Fact]
    public Task AttributeServiceProviderWithThreadSafeFalse() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task AttributeServiceProviderWithThreadSafeFalseAndCreationTimeLazy() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task AttributeServiceProviderWithThreadSafeFalseAndGetAccessorMethod() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task AttributeServiceProviderWithThreadSafeFalseAndGetAccessorMethodAndLazy() {
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

        return Verify(sourceTextClass);
    }


    [Fact]
    public Task AttributeScopeProviderNotGenerated() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public Task AttributeScopeProviderWithCreationTimeLazy() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task AttributeScopeProviderWithGetAccessorMethod() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task AttributeScopeProviderWithGetAccessorMethodAndLazy() {
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

        return Verify(sourceTextClass);
    }


    [Fact]
    public Task AttributeScopeProviderWithThreadSafeFalse() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task AttributeScopeProviderWithThreadSafeFalseAndCreationTimeLazy() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task AttributeScopeProviderWithThreadSafeFalseAndGetAccessorMethod() {
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

        return Verify(sourceTextClass);
    }

    [Fact]
    public Task AttributeScopeProviderWithThreadSafeFalseAndGetAccessorMethodAndLazy() {
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

        return Verify(sourceTextClass);
    }


    [Fact]
    public Task AttributeServiceProviderWithNoDiposeGeneration() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public Task AttributeServiceProviderWithDiposeOnlyGeneration() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider(GenerateDisposeMethods = DisposeGeneration.DisposeOnly)]
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public Task AttributeServiceProviderWithDiposeAsyncOnlyGeneration() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider(GenerateDisposeMethods = DisposeGeneration.DisposeAsyncOnly)]
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public Task AttributeServiceProviderWithDiposeBothGeneration() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider(GenerateDisposeMethods = DisposeGeneration.GenerateBoth)]
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Fact]
    public Task AttributeScopeProviderWithNoDiposeGeneration() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(GenerateDisposeMethods = DisposeGeneration.NoDisposing)]
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public Task AttributeScopeProviderWithDiposeOnlyGeneration() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(GenerateDisposeMethods = DisposeGeneration.DisposeOnly)]
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public Task AttributeScopeProviderWithDiposeAsyncOnlyGeneration() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(GenerateDisposeMethods = DisposeGeneration.DisposeAsyncOnly)]
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public Task AttributeScopeProviderWithDiposeBothGeneration() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [ScopedProvider(GenerateDisposeMethods = DisposeGeneration.GenerateBoth)]
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Fact]
    public void AttributeScopeProviderAlsoWorkingOnScopeClass() {
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


        Assert.Contains("public sealed partial class TestProvider", sourceTextClass);
        Assert.DoesNotContain("Scope ", sourceTextClass);
        Assert.DoesNotContain("CreateScope", sourceTextClass);

        Assert.Contains("public partial interface ITestProvider", sourceTextInterface);
        Assert.DoesNotContain("IScope ", sourceTextInterface);
        Assert.DoesNotContain("CreateScope", sourceTextInterface);
    }

    [Fact]
    public void AttributeScopeProviderReportsErrorWhenUsedTwice() {
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

        Assert.Single(diagnostics);
        Assert.Equal("CDI003", diagnostics[0].Id);
        Assert.Equal("Double ScopedProviderAttribute is not allowed, put either one on the ServiceProvider or ScopedProvider, but not both", diagnostics[0].GetMessage());
    }


    [Fact]
    public Task FullExample() {
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
            public sealed class SingletonDisposable : ISingletonDisposable, IDisposable;
            
            public interface ISingletonAsyncDisposable : IAsyncDisposable;
            public sealed class SingletonAsyncDisposable : ISingletonAsyncDisposable;


            public interface IScoped;
            public sealed class Scoped : IScoped;
            
            public interface IScopedDisposable : IDisposable;
            public sealed class ScopedDisposable : IScopedDisposable;
            
            public interface IScopedAsyncDisposable : IAsyncDisposable;
            public sealed class ScopedAsyncDisposable : IScopedAsyncDisposable;


            public interface ITransient;
            public sealed class Transient : ITransient;
            
            public interface ITransientDisposable : IDisposable;
            public sealed class TransientDisposable : ITransientDisposable;
            
            public interface ITransientAsyncDisposable : IAsyncDisposable;
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }
}
