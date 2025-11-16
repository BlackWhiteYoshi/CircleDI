using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Tests;

/// <summary>
/// Tests for the [Import] attribute
/// </summary>
public sealed class ImportTests {
    [Test]
    public async ValueTask Import() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<ITestModule>]
            public sealed partial class TestProvider;

            [Transient<ITestService, TestService>]
            public interface ITestModule;

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
    public async ValueTask TypeAsParameter() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import(typeof(ITestModule))]
            public sealed partial class TestProvider;

            [Transient<ITestService, TestService>]
            public interface ITestModule;

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
    public async ValueTask ClosedGeneric() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<ITestModule<int>>]
            public sealed partial class TTestProvider;

            [Transient<ITestService, TestService>]
            [Delegate<SomeAction>(nameof(MyAction))]
            public interface ITestModule<T> {
                public static void MyAction() { }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            public delegate void SomeAction();

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
    public async ValueTask UnboundGenericService() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import(typeof(ITestModule<>))]
            public sealed partial class TTestProvider;

            [Transient<ITestService, TestService>]
            [Delegate<SomeAction>(nameof(MyAction))]
            public interface ITestModule<T> {
                public static void MyAction() { }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            public delegate void SomeAction();

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
    public async ValueTask TypeAsParameterAndImportMode() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import(typeof(ITestModule), ImportMode.Static)]
            public sealed partial class TestProvider;

            [Transient<ITestService, TestService>(Implementation = nameof(TestService))]
            public interface ITestModule {
                public static TestService TestService => new();
            }

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
    public async ValueTask Static() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<ITestModule>(ImportMode.Static)]
            public sealed partial class TestProvider;

            [Transient<ITestService, TestService>(Implementation = nameof(TestService))]
            public interface ITestModule {
                public static TestService TestService => new();
            }

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
    public async ValueTask AutoStatic() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<ITestModule>]
            public sealed partial class TestProvider;

            [Transient<ITestService, TestService>(Implementation = nameof(TestService))]
            public interface ITestModule {
                public static TestService TestService => new();
            }

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
    public async ValueTask AsService() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<TestModule>(ImportMode.Service)]
            public sealed partial class TestProvider;

            [Transient<ITestService, TestService>(Implementation = nameof(TestService))]
            [Transient<ITestService2, TestService2>(Implementation = nameof(TestService2))]
            public class TestModule {
                public TestService TestService => new();

                public static TestService2 TestService2 => new();
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            public interface ITestService2;
            public sealed class TestService2 : ITestService2;

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
    public async ValueTask AutoAsService() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<TestModule>]
            public sealed partial class TestProvider;

            [Transient<ITestService, TestService>(Implementation = nameof(TestService))]
            [Transient<ITestService2, TestService2>(Implementation = nameof(TestService2))]
            public class TestModule {
                public TestService TestService => new();

                public static TestService2 TestService2 => new();
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            public interface ITestService2;
            public sealed class TestService2 : ITestService2;

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
    public async ValueTask Parameter() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<TestModule>(ImportMode.Parameter)]
            public sealed partial class TestProvider;

            [Transient<ITestService, TestService>(Implementation = nameof(TestService))]
            [Transient<ITestService2, TestService2>(Implementation = nameof(TestService2))]
            public class TestModule {
                public TestModule(string str) { }

                public TestService TestService => new();

                public static TestService2 TestService2 => new();
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            public interface ITestService2;
            public sealed class TestService2 : ITestService2;

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
    public async ValueTask AutoParameter() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<TestModule>]
            public sealed partial class TestProvider;

            [Transient<ITestService, TestService>(Implementation = nameof(TestService))]
            [Transient<ITestService2, TestService2>(Implementation = nameof(TestService2))]
            public class TestModule {
                public TestModule(string str) { }

                public TestService TestService => new();

                public static TestService2 TestService2 => new();
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            public interface ITestService2;
            public sealed class TestService2 : ITestService2;

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
    public async ValueTask StaticScope() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<ITestModule>]
            public sealed partial class TestProvider;

            public interface ITestModule {
                [Transient<ITestService, TestService>(Implementation = nameof(TestService))]
                public interface Scope {
                    public static TestService TestService => new();
                }
            }

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
    public async ValueTask AsServiceScope() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<TestModule>]
            public sealed partial class TestProvider;

            public class TestModule {
                [Transient<ITestService, TestService>(Implementation = nameof(TestService))]
                [Transient<ITestService2, TestService2>(Implementation = nameof(TestService2))]
                public class Scope {
                    public TestService TestService => new();

                    public static TestService2 TestService2 => new();
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            public interface ITestService2;
            public sealed class TestService2 : ITestService2;

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
    public async ValueTask ParameterScope() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<TestModule>]
            public sealed partial class TestProvider;

            public class TestModule {
                [Transient<ITestService, TestService>(Implementation = nameof(TestService))]
                [Transient<ITestService2, TestService2>(Implementation = nameof(TestService2))]
                public class Scope {
                    public Scope(string str) { }

                    public TestService TestService => new();

                    public static TestService2 TestService2 => new();
                }
            }

            public interface ITestService;
            public sealed class TestService : ITestService;

            public interface ITestService2;
            public sealed class TestService2 : ITestService2;

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
    public async ValueTask AsServiceSingletonWithSetsRequiredMembers() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<TestModule>]
            public sealed partial class TestProvider;

            [Transient<TestService>]
            [method: System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
            public sealed class TestModule() {
                public required string Str { private get; init; }
            }

            public sealed class TestService():

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
    public async ValueTask AsServiceScopeWithSetsRequiredMembers() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<TestModule>]
            public sealed partial class TestProvider;

            [Transient<TestService>]
            public class TestModule {
                [method: System.Diagnostics.CodeAnalysis.SetsRequiredMembers]
                public sealed class Scope() {
                    public required string Str { private get; init; }
                }
            }

            public sealed class TestService():

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
            [Import<ITestModule>]
            public sealed partial class TestProvider;

            public partial interface ITestProvider;

            [Singleton<ITestProvider, TestProvider>(Name = "Me", Implementation = "this")]
            public interface ITestModule;

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
    public async ValueTask OverwriteDefaultServiceSelfScope() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<ITestModule>]
            public sealed partial class TestProvider {
                public sealed partial class Scope;
            }

            public partial interface ITestProvider {
                public partial interface IScope;
            }

            [Scoped<ITestProvider.IScope, TestProvider.Scope>(Name = "Me", Implementation = "this")]
            public interface ITestModule;

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
    public async ValueTask Recursive() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<ITestModuleStatic>(ImportMode.Static)]
            [Import<TestModuleService>(ImportMode.Service)]
            public sealed partial class TestProvider;

            [Transient<ITestService1, TestService1>(Implementation = nameof(CreateService))]
            public interface ITestModuleStatic {
                public static TestService1 CreateService => new();
            }

            [Transient<ITestService2, TestService2>(Implementation = nameof(CreateService))]
            [Transient<ITestService3, TestService3>(Implementation = nameof(CreateServiceStatic))]
            [Import<TestModuleParameter>(ImportMode.Parameter)]
            public class TestModuleService {
                public TestService2 CreateService => new();

                public static TestService3 CreateServiceStatic => new();
            }

            [Transient<ITestService4, TestService4>(Implementation = nameof(CreateService))]
            [Transient<ITestService5, TestService5>(Implementation = nameof(CreateServiceStatic))]
            public class TestModuleParameter {
                public TestModule(string str) { }

                public TestService4 CreateService => new();

                public static TestService5 CreateServiceStatic => new();
            }


            public interface ITestService1;
            public sealed class TestService1 : ITestService1;

            public interface ITestService2;
            public sealed class TestService2 : ITestService2;

            public interface ITestService3;
            public sealed class TestService3 : ITestService3;

            public interface ITestService4;
            public sealed class TestService4 : ITestService4;

            public interface ITestService5;
            public sealed class TestService5 : ITestService5;

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
    public async ValueTask CycleError() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<MyModule>]
            public sealed partial class TestProvider;

            [Import<MyModule2>]
            public class MyModule;

            [Import<MyModule>]
            public class MyModule2;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI006");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Module cycle in ServiceProvider 'MyCode.TestProvider': ['MyCode.MyModule' -> 'MyCode.MyModule2' -> 'MyCode.MyModule']");
    }

    [Test]
    public async ValueTask OverwriteDefaultServiceSelfAsConstuctorCallFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<ITestModule>]
            public sealed partial class TestProvider;

            public partial interface ITestProvider;

            [Singleton<ITestProvider, TestProvider>(Name = "Me")]
            public interface ITestModule;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI007");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Endless recursive constructor call in ServiceProvider: Service 'Me' adds a constructor call to the constructor which results in an endless recursion. Did you mean to add 'Implementation = \"this\"'?");
    }

    [Test]
    public async ValueTask OverwriteDefaultServiceSelfScopeAsConstuctorCallFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Import<ITestModule>]
            public sealed partial class TestProvider {
                public sealed partial class Scope;
            }

            public partial interface ITestProvider {
                public partial interface IScope;
            }

            [Scoped<ITestProvider.IScope, TestProvider.Scope>(Name = "Me")]
            public interface ITestModule;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI008");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Endless recursive constructor call in ScopedProvider: Service 'Me' adds a constructor call to the constructor which results in an endless recursion. Did you mean to add 'Implementation = \"this\"'?");
    }
}
