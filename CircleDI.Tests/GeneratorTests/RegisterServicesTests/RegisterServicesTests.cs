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
    [Test]
    public async ValueTask Singleton() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask SingletonWithImplementationTypeOnly() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask SingletonWithTypesAsParameter() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask SingletonWithImplementationTypeOnlyAsParameter() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask SingletonWithName() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask SingletonWithCreationTimeLazy() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask SingletonWithGetAccessorMethod() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask SingletonWithImplementationThis() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask SingletonWithImplementationField() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask SingletonWithImplementationFieldScoped() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI013");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found");
    }

    [Test]
    public async ValueTask SingletonWithImplementationFieldStatic() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """
        );
    }

    [Test]
    public async ValueTask SingletonWithImplementationFieldStaticScoped() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI013");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found");
    }


    [Test]
    public async ValueTask SingletonWithImplementationProperty() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask SingletonWithImplementationPropertyScoped() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI013");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found");
    }

    [Test]
    public async ValueTask SingletonWithImplementationPropertyStatic() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask SingletonWithImplementationPropertyStaticScoped() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI013");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found");
    }


    [Test]
    public async ValueTask SingletonWithImplementationMethod() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask SingletonWithImplementationMethodScoped() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI013");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found");
    }

    [Test]
    public async ValueTask SingletonWithImplementationMethodStatic() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask SingletonWithImplementationMethodStaticScoped() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI013");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("No field, property or method with the name 'testField' in class 'MyCode.TestProvider' could be found");
    }


    [Test]
    public async ValueTask MultipleSingletons() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }



    [Test]
    public async ValueTask Scoped() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithImplementationTypeOnly() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithTypesAsParameter() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithImplementationTypeOnlyAsParameter() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithName() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithCreationTimeLazy() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithGetAccessorMethod() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask ScopedWithImplementationThis() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask ScopedWithImplementationField() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithImplementationFieldScoped() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithImplementationFieldStatic() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithImplementationFieldStaticScoped() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask ScopedWithImplementationProperty() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithImplementationPropertyScoped() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithImplementationPropertyStatic() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithImplementationPropertyStaticScoped() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask ScopedWithImplementationMethod() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithImplementationMethodScoped() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithImplementationMethodStatic() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ScopedWithImplementationMethodStaticScoped() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask MultipleScopedServices() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }



    [Test]
    public async ValueTask Transient() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask TransientWithImplementationTypeOnly() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask TransientWithTypesAsParameter() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask TransientWithImplementationTypeOnlyAsParameter() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask TransientWithName() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask TransientWithGetAccessorMethod() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask TransientWithImplementationThis() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Transient<TestProvider>(Implementation = "this")]
            public sealed partial class TestProvider;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI012");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Transient + Implementation = 'this' is not allowed. Use Singleton or Scoped instead");
    }


    [Test]
    public async ValueTask TransientWithImplementationField() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI011");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Transient + Implementation field member is not allowed. Use Singleton or Scoped instead or use a property/method as Implementation");
    }

    [Test]
    public async ValueTask TransientWithImplementationFieldScoped() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI011");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Transient + Implementation field member is not allowed. Use Singleton or Scoped instead or use a property/method as Implementation");
    }

    [Test]
    public async ValueTask TransientWithImplementationFieldStatic() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI011");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Transient + Implementation field member is not allowed. Use Singleton or Scoped instead or use a property/method as Implementation");
    }

    [Test]
    public async ValueTask TransientWithImplementationFieldStaticScoped() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI011");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Transient + Implementation field member is not allowed. Use Singleton or Scoped instead or use a property/method as Implementation");
    }


    [Test]
    public async ValueTask TransientWithImplementationProperty() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask TransientWithImplementationPropertyScoped() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask TransientWithImplementationPropertyStatic() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask TransientWithImplementationPropertyStaticScoped() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask TransientWithImplementationMethod() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask TransientWithImplementationMethodScoped() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask TransientWithImplementationMethodStatic() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask TransientWithImplementationMethodStaticScoped() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask MultipleTransientServices() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }



    [Test]
    public async ValueTask InvalidServiceRegistration() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestProvider.Scope>]
            public sealed partial class TestProvider;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI009");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Invalid type at service registration. If you are using a generated type like 'MyCode.TestProvider.Scope', 'MyCode.ITestProvider' or 'MyCode.ITestProvider.IScope', declare that type again, so it is available before generation.");
    }

    [Test]
    public async ValueTask RegisterServiceThatDoesNotExistsFails() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI025");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("ServiceImplementation 'MyCode.TestService' does not exist or has no accessible constructor");
    }


    [Test]
    public async ValueTask RegisterServiceWithSetsRequiredMembers() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService;
            public sealed class TestService : ITestService {
                public required string Str { private get; init; }
                
                [System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
                public TestService() { }
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
    public async ValueTask RegisterServiceConstructorDependenciesWithSetsRequiredMembers() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<int>(Implementation = nameof(_myInt))]
            public sealed partial class TestProvider {
                private readonly int _myInt = 5;
            }

            public interface ITestService;
            [method: System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
            public sealed class TestService(int number) : ITestService {
                public required string Str { private get; init; }
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
    public async ValueTask RegisterServicePropertyDependencyWithSetsRequiredMembers() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            public sealed partial class TestProvider;

            public interface ITestService;
            [method: System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
            public sealed class TestService() : ITestService {
                [Dependency]
                public required ITestService Self { private get; init; }
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
    public async ValueTask RegisterServiceThatDerivesFromBaseClass() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask RegisterServiceClosedGeneric() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask RegisterServiceClosedGenericWithImplementation() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask RegisterDelegateClosedGeneric() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask OverwriteDefaultServiceSelf() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask OverwriteDefaultServiceSelfAsConstuctorCallFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestProvider, TestProvider>(Name = "Me")]
            public sealed partial class TestProvider;

            public partial interface ITestProvider;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI007");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Endless recursive constructor call in ServiceProvider: Service 'Me' adds a constructor call to the constructor which results in an endless recursion. Did you mean to add 'Implementation = \"this\"'?");
    }

    [Test]
    public async ValueTask OverwriteDefaultServiceSelfScope() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask OverwriteDefaultServiceSelfScopeAsConstuctorCallFails() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI008");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Endless recursive constructor call in ScopedProvider: Service 'Me' adds a constructor call to the constructor which results in an endless recursion. Did you mean to add 'Implementation = \"this\"'?");
    }


    [Test]
    public async ValueTask RegisterServiceWithFieldImplementationOfWrongType() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI014");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Wrong type of field '_singleton': 'MyCode.ITestService' <-> 'MyCode.TestService' expected");
    }

    [Test]
    public async ValueTask RegisterServiceWithPropertyImplementationOfWrongType() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI015");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Wrong type of property 'Singleton': 'MyCode.ITestService' <-> 'MyCode.TestService' expected");
    }

    [Test]
    public async ValueTask RegisterServiceWithMethodImplementationOfWrongType() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI017");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Wrong return type of method 'CreateSingleton': 'MyCode.ITestService' <-> 'MyCode.TestService' expected");
    }
}
