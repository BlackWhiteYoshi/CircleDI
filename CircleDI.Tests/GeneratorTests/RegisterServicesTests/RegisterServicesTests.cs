﻿using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Tests;

/// <summary>
/// <para>Tests the ServiceProvider for registering services:</para>
/// <para>
/// Singleton<br />
/// Scoped<br />
/// Transient
/// </para>
/// </summary>
public static class RegisterServicesTests {
    [Fact]
    public static Task Singleton() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

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
    public static Task SingletonWithImplementationTypeOnly() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService>]
            public sealed partial class TestProvider;

            public sealed class TestService;

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
    public static Task SingletonWithTypesAsParameter() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(ITestService), typeof(TestService))]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

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
    public static Task SingletonWithImplementationTypeOnlyAsParameter() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton(typeof(TestService))]
            public sealed partial class TestProvider;

            public sealed class TestService;

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
    public static Task SingletonWithName() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Name = "Abc")]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

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
    public static Task SingletonWithCreationTimeLazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(CreationTime = CreationTiming.Lazy)]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

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
    public static Task SingletonWithGetAccessorMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(GetAccessor = GetAccess.Method)]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

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
    public static Task SingletonWithImplementationThis() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestProvider>(Implementation = "this")]
            [Singleton<Consumer>]
            public sealed partial class TestProvider;

            public sealed class Consumer(TestProvider testProvider);

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
    public static Task SingletonWithImplementationField() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(testField))]
            [Singleton<Consumer>]
            public sealed partial class TestProvider {
                private TestService testField;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static void SingletonWithImplementationFieldScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private TestService testField;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI013", diagnostics[0].Id);
        Assert.Equal("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found", diagnostics[0].GetMessage());
    }

    [Fact]
    public static Task SingletonWithImplementationFieldStatic() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(testField))]
            [Singleton<Consumer>]
            public sealed partial class TestProvider {
                private static TestService testField;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static void SingletonWithImplementationFieldStaticScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static TestService testField;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI013", diagnostics[0].Id);
        Assert.Equal("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found", diagnostics[0].GetMessage());
    }


    [Fact]
    public static Task SingletonWithImplementationProperty() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(TestProperty))]
            [Singleton<Consumer>]
            public sealed partial class TestProvider {
                private TestService TestProperty => default!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static void SingletonWithImplementationPropertyScoped() {
        const string input = """
            using CircleDIAttributes;
            F
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private TestService TestProperty => default!;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI013", diagnostics[0].Id);
        Assert.Equal("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found", diagnostics[0].GetMessage());
    }

    [Fact]
    public static Task SingletonWithImplementationPropertyStatic() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(TestProperty))]
            [Singleton<Consumer>]
            public sealed partial class TestProvider {
                private static TestService TestProperty => default!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static void SingletonWithImplementationPropertyStaticScoped() {
        const string input = """
            using CircleDIAttributes;
            F
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static TestService TestProperty => default!;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI013", diagnostics[0].Id);
        Assert.Equal("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found", diagnostics[0].GetMessage());
    }


    [Fact]
    public static Task SingletonWithImplementationMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(TestMethod))]
            [Singleton<Consumer>]
            public sealed partial class TestProvider {
                private TestService TestMethod() => default!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static void SingletonWithImplementationMethodScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private TestService TestMethod() => default!;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI013", diagnostics[0].Id);
        Assert.Equal("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found", diagnostics[0].GetMessage());
    }

    [Fact]
    public static Task SingletonWithImplementationMethodStatic() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(TestMethod))]
            [Singleton<Consumer>]
            public sealed partial class TestProvider {
                private static TestService TestMethod() => default!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static void SingletonWithImplementationMethodStaticScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static TestService TestMethod() => default!;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI013", diagnostics[0].Id);
        Assert.Equal("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found", diagnostics[0].GetMessage());
    }


    [Fact]
    public static Task MultipleSingletons() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestService, TestService>(Name = "TestService2")]
            [Singleton<ITestServiceM, TestServiceM>]
            [Singleton<Consumer>]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

            public interface ITestServiceM;
            public sealed class TestServiceM : ITestServiceM;
            
            public sealed class Consumer([Dependency(Name = "TestService")] ITestService testService, [Dependency(Name = "TestService2")] ITestService testService2, ITestServiceM testServiceM);

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
    public static Task Scoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

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
    public static Task ScopedWithImplementationTypeOnly() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<TestService>]
            public sealed partial class TestProvider;

            public sealed class TestService;

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
    public static Task ScopedWithTypesAsParameter() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped(typeof(ITestService), typeof(TestService))]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

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
    public static Task ScopedWithImplementationTypeOnlyAsParameter() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped(typeof(TestService))]
            public sealed partial class TestProvider;

            public sealed class TestService;

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
    public static Task ScopedWithName() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Name = "Abc")]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

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
    public static Task ScopedWithCreationTimeLazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(CreationTime = CreationTiming.Lazy)]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

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
    public static Task ScopedWithGetAccessorMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(GetAccessor = GetAccess.Method)]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

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
    public static Task ScopedWithImplementationThis() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<TestProvider.Scope>(Implementation = "this")]
            [Scoped<Consumer>]
            public sealed partial class TestProvider {
                public sealed partial class Scope;
            }

            public sealed class Consumer(TestProvider.Scope testProviderScope);

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
    public static Task ScopedWithImplementationField() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(testField))]
            [Scoped<Consumer>]
            public sealed partial class TestProvider {
                private TestService testField;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task ScopedWithImplementationFieldScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(testField))]
            [Scoped<Consumer>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private TestService testField;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task ScopedWithImplementationFieldStatic() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(testField))]
            [Scoped<Consumer>]
            public sealed partial class TestProvider {
                private static TestService testField;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task ScopedWithImplementationFieldStaticScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(testField))]
            [Scoped<Consumer>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static TestService testField;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task ScopedWithImplementationProperty() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestProperty))]
            [Scoped<Consumer>]
            public sealed partial class TestProvider {
                private TestService TestProperty => default!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task ScopedWithImplementationPropertyScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestProperty))]
            [Scoped<Consumer>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private TestService TestProperty => default!;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task ScopedWithImplementationPropertyStatic() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestProperty))]
            [Scoped<Consumer>]
            public sealed partial class TestProvider {
                private static TestService TestProperty => default!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task ScopedWithImplementationPropertyStaticScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestProperty))]
            [Scoped<Consumer>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static TestService TestProperty => default!;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task ScopedWithImplementationMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestMethod))]
            [Scoped<Consumer>]
            public sealed partial class TestProvider {
                private TestService TestMethod() => default!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task ScopedWithImplementationMethodScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestMethod))]
            [Scoped<Consumer>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private TestService TestMethod() => default!;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task ScopedWithImplementationMethodStatic() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestMethod))]
            [Scoped<Consumer>]
            public sealed partial class TestProvider {
                private static TestService TestMethod() => default!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task ScopedWithImplementationMethodStaticScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestMethod))]
            [Scoped<Consumer>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static TestService TestMethod() => default!;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task MultipleScopedServices() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>]
            [Scoped<ITestService, TestService>(Name = "TestService2")]
            [Scoped<ITestServiceM, TestServiceM>]
            [Scoped<Consumer>]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

            public interface ITestServiceM;
            public sealed class TestServiceM : ITestServiceM;
            
            public sealed class Consumer([Dependency(Name = "TestService")] ITestService testService, [Dependency(Name = "TestService2")] ITestService testService2, ITestServiceM testServiceM);

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
    public static Task Transient() {
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
    public static Task TransientWithImplementationTypeOnly() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<TestService>]
            public sealed partial class TestProvider;

            public sealed class TestService;

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
    public static Task TransientWithTypesAsParameter() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient(typeof(ITestService), typeof(TestService))]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

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
    public static Task TransientWithImplementationTypeOnlyAsParameter() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient(typeof(TestService))]
            public sealed partial class TestProvider;

            public sealed class TestService;

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
    public static Task TransientWithName() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Name = "Abc")]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

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
    public static Task TransientWithGetAccessorMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(GetAccessor = GetAccess.Method)]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

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
    public static void TransientWithImplementationThis() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<TestProvider>(Implementation = "this")]
            public sealed partial class TestProvider;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI012", diagnostics[0].Id);
        Assert.Equal("Transient + Implementation = 'this' is not allowed. Use Singleton or Scoped instead", diagnostics[0].GetMessage());
    }


    [Fact]
    public static void TransientWithImplementationField() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                private TestService testField;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI011", diagnostics[0].Id);
        Assert.Equal("Transient + Implementation field member is not allowed. Use Singleton or Scoped instead or use a property/method as Implementation", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void TransientWithImplementationFieldScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private TestService testField;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI011", diagnostics[0].Id);
        Assert.Equal("Transient + Implementation field member is not allowed. Use Singleton or Scoped instead or use a property/method as Implementation", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void TransientWithImplementationFieldStatic() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                private static TestService testField;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI011", diagnostics[0].Id);
        Assert.Equal("Transient + Implementation field member is not allowed. Use Singleton or Scoped instead or use a property/method as Implementation", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void TransientWithImplementationFieldStaticScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static TestService testField;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI011", diagnostics[0].Id);
        Assert.Equal("Transient + Implementation field member is not allowed. Use Singleton or Scoped instead or use a property/method as Implementation", diagnostics[0].GetMessage());
    }


    [Fact]
    public static Task TransientWithImplementationProperty() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestProperty))]
            [Transient<Consumer>]
            public sealed partial class TestProvider {
                private TestService TestProperty => default!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task TransientWithImplementationPropertyScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestProperty))]
            [Transient<Consumer>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private TestService TestProperty => default!;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task TransientWithImplementationPropertyStatic() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestProperty))]
            [Transient<Consumer>]
            public sealed partial class TestProvider {
                private static TestService TestProperty => default!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task TransientWithImplementationPropertyStaticScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestProperty))]
            [Transient<Consumer>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static TestService TestProperty => default!;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task TransientWithImplementationMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestMethod))]
            [Transient<Consumer>]
            public sealed partial class TestProvider {
                private TestService TestMethod() => default!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task TransientWithImplementationMethodScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestMethod))]
            [Transient<Consumer>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private TestService TestMethod() => default!;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task TransientWithImplementationMethodStatic() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestMethod))]
            [Transient<Consumer>]
            public sealed partial class TestProvider {
                private static TestService TestMethod() => default!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task TransientWithImplementationMethodStaticScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestMethod))]
            [Transient<Consumer>]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static TestService TestMethod() => default!;
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public sealed class Consumer(ITestService testService);

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
    public static Task MultipleTransientServices() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>]
            [Transient<ITestService, TestService>(Name = "TestService2")]
            [Transient<ITestServiceM, TestServiceM>]
            [Transient<Consumer>]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService;

            public interface ITestServiceM;
            public sealed class TestServiceM : ITestServiceM;
            
            public sealed class Consumer([Dependency(Name = "TestService")] ITestService testService, [Dependency(Name = "TestService2")] ITestService testService2, ITestServiceM testServiceM);

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
    public static void InvalidServiceRegistration() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestProvider.Scope>]
            public sealed partial class TestProvider;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI009", diagnostics[0].Id);
        Assert.Equal("Invalid type at service registration. If you are using a generated type like 'MyCode.TestProvider.Scope', 'MyCode.ITestProvider' or 'MyCode.ITestProvider.IScope', declare that type again, so it is available before generation.", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void RegisterServiceThatDoesNotExistsFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService;
            public interface TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI025", diagnostics[0].Id);
        Assert.Equal("ServiceImplementation 'MyCode.TestService' does not exist or has no accessible constructor", diagnostics[0].GetMessage());
    }


    [Fact]
    public static Task RegisterServiceThatDerivesFromBaseClass() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<DerivedClass>]
            public sealed partial class TestProvider;


            public abstract class BaseBaseClass {
                public required DerivedClass DerivedBaseBase { get; init; }
            }

            public abstract class BaseClass : BaseBaseClass {
                public required DerivedClass DerivedBase { get; init; }
            }

            public sealed class DerivedClass : BaseClass {
                public required DerivedClass Derived { get; init; }
            }

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
    public static Task RegisterServiceClosedGeneric() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService<string>>]
            public sealed partial class TestProvider;

            public sealed class TestService<T>;

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
    public static Task RegisterServiceClosedGenericWithImplementation() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService<string>>(Implementation = nameof(GetTestProvider))]
            public sealed partial class TestProvider {
                private static TestService<string> GetTestProvider() => new();
            }

            public sealed class TestService<T>;

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
    public static Task RegisterDelegateClosedGeneric() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<TestFunction<string>>(nameof(MyFunction))]
            public sealed partial class TestProvider {
                private static void MyFunction() { }
            }

            public delegate void TestFunction<string>();

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
    public static Task OverwriteDefaultServiceSelf() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestProvider, TestProvider>(Name = "Me", Implementation = "this")]
            public sealed partial class TestProvider;

            public partial interface ITestProvider;

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
    public static void OverwriteDefaultServiceSelfAsConstuctorCallFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestProvider, TestProvider>(Name = "Me")]
            public sealed partial class TestProvider;

            public partial interface ITestProvider;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI007", diagnostics[0].Id);
        Assert.Equal("Endless recursive constructor call in ServiceProvider: Service 'Me' adds a constructor call to the constructor which results in an endless recursion. Did you mean to add 'Implementation = \"this\"'?", diagnostics[0].GetMessage());
    }

    [Fact]
    public static Task OverwriteDefaultServiceSelfScope() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestProvider.IScope, TestProvider.Scope>(Name = "Me", Implementation = "this")]
            public sealed partial class TestProvider {
                public sealed partial class Scope;
            }

            public partial interface ITestProvider {
                public partial interface IScope;
            }

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
    public static void OverwriteDefaultServiceSelfScopeAsConstuctorCallFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestProvider.IScope, TestProvider.Scope>(Name = "Me")]
            public sealed partial class TestProvider {
                public sealed partial class Scope;
            }

            public partial interface ITestProvider {
                public partial interface IScope;
            }

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI008", diagnostics[0].Id);
        Assert.Equal("Endless recursive constructor call in ScopedProvider: Service 'Me' adds a constructor call to the constructor which results in an endless recursion. Did you mean to add 'Implementation = \"this\"'?", diagnostics[0].GetMessage());
    }


    [Fact]
    public static void RegisterServiceWithFieldImplementationOfWrongType() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(_singleton))]
            public sealed partial class TestProvider {
                private ITestService _singleton = null!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI014", diagnostics[0].Id);
        Assert.Equal("Wrong type of field '_singleton': 'MyCode.ITestService' <-> 'MyCode.TestService' expected", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void RegisterServiceWithPropertyImplementationOfWrongType() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(Singleton))]
            public sealed partial class TestProvider {
                private ITestService Singleton => null!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI015", diagnostics[0].Id);
        Assert.Equal("Wrong type of property 'Singleton': 'MyCode.ITestService' <-> 'MyCode.TestService' expected", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void RegisterServiceWithMethodImplementationOfWrongType() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(CreateSingleton))]
            public sealed partial class TestProvider {
                private ITestService CreateSingleton() => null!;
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI017", diagnostics[0].Id);
        Assert.Equal("Wrong return type of method 'CreateSingleton': 'MyCode.ITestService' <-> 'MyCode.TestService' expected", diagnostics[0].GetMessage());
    }
}
