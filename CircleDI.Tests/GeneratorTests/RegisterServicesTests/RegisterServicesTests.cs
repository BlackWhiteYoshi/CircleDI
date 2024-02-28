using CircleDI.Tests.GenerateSourceText;
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
public sealed class RegisterServicesTests {
    [Fact]
    public Task Singleton() {
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
    public Task SingletonWithImplementationTypeOnly() {
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
    public Task SingletonWithName() {
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
    public Task SingletonWithCreationTimeLazy() {
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
    public Task SingletonWithGetAccessorMethod() {
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
    public Task SingletonWithImplementationThis() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestProvider>(Implementation = "this")]
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
    public Task SingletonWithImplementationField() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                private TestService testField;
            }

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
    public void SingletonWithImplementationFieldScoped() {
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
        Assert.Equal("CDI008", diagnostics[0].Id);
        Assert.Equal("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found", diagnostics[0].GetMessage());
    }

    [Fact]
    public Task SingletonWithImplementationFieldStatic() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                private static TestService testField;
            }

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
    public void SingletonWithImplementationFieldStaticScoped() {
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
        Assert.Equal("CDI008", diagnostics[0].Id);
        Assert.Equal("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found", diagnostics[0].GetMessage());
    }


    [Fact]
    public Task SingletonWithImplementationProperty() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(TestProperty))]
            public sealed partial class TestProvider {
                private TestService TestProperty => default!;
            }

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
    public void SingletonWithImplementationPropertyScoped() {
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
        Assert.Equal("CDI008", diagnostics[0].Id);
        Assert.Equal("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found", diagnostics[0].GetMessage());
    }

    [Fact]
    public Task SingletonWithImplementationPropertyStatic() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(TestProperty))]
            public sealed partial class TestProvider {
                private static TestService TestProperty => default!;
            }

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
    public void SingletonWithImplementationPropertyStaticScoped() {
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
        Assert.Equal("CDI008", diagnostics[0].Id);
        Assert.Equal("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found", diagnostics[0].GetMessage());
    }


    [Fact]
    public Task SingletonWithImplementationMethod() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(TestMethod))]
            public sealed partial class TestProvider {
                private TestService TestMethod() => default!;
            }

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
    public void SingletonWithImplementationMethodScoped() {
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
        Assert.Equal("CDI008", diagnostics[0].Id);
        Assert.Equal("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found", diagnostics[0].GetMessage());
    }

    [Fact]
    public Task SingletonWithImplementationMethodStatic() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(TestMethod))]
            public sealed partial class TestProvider {
                private static TestService TestMethod() => default!;
            }

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
    public void SingletonWithImplementationMethodStaticScoped() {
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
        Assert.Equal("CDI008", diagnostics[0].Id);
        Assert.Equal("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found", diagnostics[0].GetMessage());
    }


    [Fact]
    public Task MultipleSingletons() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestService, TestService>(Name = "TestService2")]
            [Singleton<ITestServiceM, TestServiceM>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public interface ITestServiceM;
            public sealed class TestServiceM : ITestServiceM;

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
    public Task Scoped() {
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
    public Task ScopedWithImplementationTypeOnly() {
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
    public Task ScopedWithName() {
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
    public Task ScopedWithCreationTimeLazy() {
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
    public Task ScopedWithGetAccessorMethod() {
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
    public Task ScopedWithImplementationThis() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<TestProvider.Scope>(Implementation = "this")]
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
    public Task ScopedWithImplementationField() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                private TestService testField;
            }

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
    public Task ScopedWithImplementationFieldScoped() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private TestService testField;
                }
            }

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
    public Task ScopedWithImplementationFieldStatic() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                private static TestService testField;
            }

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
    public Task ScopedWithImplementationFieldStaticScoped() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(testField))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static TestService testField;
                }
            }

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
    public Task ScopedWithImplementationProperty() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestProperty))]
            public sealed partial class TestProvider {
                private TestService TestProperty => default!;
            }

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
    public Task ScopedWithImplementationPropertyScoped() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestProperty))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private TestService TestProperty => default!;
                }
            }

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
    public Task ScopedWithImplementationPropertyStatic() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestProperty))]
            public sealed partial class TestProvider {
                private static TestService TestProperty => default!;
            }

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
    public Task ScopedWithImplementationPropertyStaticScoped() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestProperty))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static TestService TestProperty => default!;
                }
            }

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
    public Task ScopedWithImplementationMethod() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestMethod))]
            public sealed partial class TestProvider {
                private TestService TestMethod() => default!;
            }

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
    public Task ScopedWithImplementationMethodScoped() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestMethod))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private TestService TestMethod() => default!;
                }
            }

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
    public Task ScopedWithImplementationMethodStatic() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestMethod))]
            public sealed partial class TestProvider {
                private static TestService TestMethod() => default!;
            }

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
    public Task ScopedWithImplementationMethodStaticScoped() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>(Implementation = nameof(TestMethod))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static TestService TestMethod() => default!;
                }
            }

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
    public Task MultipleScopedServices() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestService, TestService>]
            [Scoped<ITestService, TestService>(Name = "TestService2")]
            [Scoped<ITestServiceM, TestServiceM>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public interface ITestServiceM;
            public sealed class TestServiceM : ITestServiceM;

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
    public Task Transient() {
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
    public Task TransientWithImplementationTypeOnly() {
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
    public Task TransientWithName() {
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
    public Task TransientWithGetAccessorMethod() {
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
    public void TransientWithImplementationThis() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Transient<TestProvider>(Implementation = "this")]
            public sealed partial class TestProvider;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI007", diagnostics[0].Id);
        Assert.Equal("Transient + Implementation = 'this' is not allowed. Use Singleton or Scoped instead", diagnostics[0].GetMessage());
    }

    [Fact]
    public void TransientWithImplementationField() {
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
        Assert.Equal("CDI006", diagnostics[0].Id);
        Assert.Equal("Transient + Implementation field member is not allowed. Use Singleton or Scoped instead or use a property/method as Implementation", diagnostics[0].GetMessage());
    }

    [Fact]
    public void TransientWithImplementationFieldScoped() {
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
        Assert.Equal("CDI006", diagnostics[0].Id);
        Assert.Equal("Transient + Implementation field member is not allowed. Use Singleton or Scoped instead or use a property/method as Implementation", diagnostics[0].GetMessage());
    }

    [Fact]
    public void TransientWithImplementationFieldStatic() {
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
        Assert.Equal("CDI006", diagnostics[0].Id);
        Assert.Equal("Transient + Implementation field member is not allowed. Use Singleton or Scoped instead or use a property/method as Implementation", diagnostics[0].GetMessage());
    }

    [Fact]
    public void TransientWithImplementationFieldStaticScoped() {
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
        Assert.Equal("CDI006", diagnostics[0].Id);
        Assert.Equal("Transient + Implementation field member is not allowed. Use Singleton or Scoped instead or use a property/method as Implementation", diagnostics[0].GetMessage());
    }


    [Fact]
    public Task TransientWithImplementationProperty() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestProperty))]
            public sealed partial class TestProvider {
                private TestService TestProperty => default!;
            }

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
    public Task TransientWithImplementationPropertyScoped() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestProperty))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private TestService TestProperty => default!;
                }
            }

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
    public Task TransientWithImplementationPropertyStatic() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestProperty))]
            public sealed partial class TestProvider {
                private static TestService TestProperty => default!;
            }

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
    public Task TransientWithImplementationPropertyStaticScoped() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestProperty))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static TestService TestProperty => default!;
                }
            }

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
    public Task TransientWithImplementationMethod() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestMethod))]
            public sealed partial class TestProvider {
                private TestService TestMethod() => default!;
            }

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
    public Task TransientWithImplementationMethodScoped() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestMethod))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private TestService TestMethod() => default!;
                }
            }

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
    public Task TransientWithImplementationMethodStatic() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestMethod))]
            public sealed partial class TestProvider {
                private static TestService TestMethod() => default!;
            }

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
    public Task TransientWithImplementationMethodStaticScoped() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>(Implementation = nameof(TestMethod))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static TestService TestMethod() => default!;
                }
            }

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
    public Task MultipleTransientServices() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Transient<ITestService, TestService>]
            [Transient<ITestService, TestService>(Name = "TestService2")]
            [Transient<ITestServiceM, TestServiceM>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            public sealed class TestService : ITestService;
            
            public interface ITestServiceM;
            public sealed class TestServiceM : ITestServiceM;

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
    public void RegisterServiceThatDoesNotExistsFails() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            public sealed partial class TestProvider;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI017", diagnostics[0].Id);
        Assert.Equal("ServiceImplementation 'TestService' does not exist or has no accessible constructor", diagnostics[0].GetMessage());
    }


    [Fact]
    public Task OverwriteDefaultServiceSelf() {
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
    public void OverwriteDefaultServiceSelfAsConstuctorCallFails() {
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
        Assert.Equal("CDI004", diagnostics[0].Id);
        Assert.Equal("Endless recursive constructor call in ServiceProvider: Service 'Me' adds a constructor call to the constructor which results in an endless recursion. Did you mean to add 'Implementation = \"this\"'?", diagnostics[0].GetMessage());
    }

    [Fact]
    public Task OverwriteDefaultServiceSelfScope() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestProvider.IScope, TestProvider.Scope>(Name = "Me", Implementation = "this")]
            public sealed partial class TestProvider;

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
    public void OverwriteDefaultServiceSelfScopeAsConstuctorCallFails() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Scoped<ITestProvider.IScope, TestProvider.Scope>(Name = "Me")]
            public sealed partial class TestProvider {
                public sealed partial class Scope;
            }

            public partial interface ITestProvider.IScope;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI005", diagnostics[0].Id);
        Assert.Equal("Endless recursive constructor call in ScopedProvider: Service 'Me' adds a constructor call to the constructor which results in an endless recursion. Did you mean to add 'Implementation = \"this\"'?", diagnostics[0].GetMessage());
    }


    [Fact]
    public void RegisterServiceWithFieldImplementationOfWrongType() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(_singleton))]
            public sealed partial class TestProvider {
                private ITestService _singleton = null!;
            }

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI009", diagnostics[0].Id);
        Assert.Equal("Wrong type of field '_singleton': 'ITestService' <-> 'TestService' expected", diagnostics[0].GetMessage());
    }

    [Fact]
    public void RegisterServiceWithPropertyImplementationOfWrongType() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(Singleton))]
            public sealed partial class TestProvider {
                private ITestService Singleton => null!;
            }

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI010", diagnostics[0].Id);
        Assert.Equal("Wrong type of property 'Singleton': 'ITestService' <-> 'TestService' expected", diagnostics[0].GetMessage());
    }

    [Fact]
    public void RegisterServiceWithMethodImplementationOfWrongType() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(Implementation = nameof(CreateSingleton))]
            public sealed partial class TestProvider {
                private ITestService CreateSingleton() => null!;
            }

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI011", diagnostics[0].Id);
        Assert.Equal("Wrong return type of method 'CreateSingleton': 'ITestService' <-> 'TestService' expected", diagnostics[0].GetMessage());
    }
}
