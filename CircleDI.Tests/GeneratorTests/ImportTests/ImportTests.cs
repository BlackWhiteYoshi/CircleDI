using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Tests;

/// <summary>
/// Tests for the [Import] attribute
/// </summary>
public static class ImportTests {
    [Fact]
    public static Task Import() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public static Task TypeAsParameter() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public static Task ClosedGeneric() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public static Task UnboundGenericService() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public static Task TypeAsParameterAndImportMode() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Fact]
    public static Task Static() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public static Task AutoStatic() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Fact]
    public static Task AsService() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public static Task AutoAsService() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Fact]
    public static Task Parameter() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public static Task AutoParameter() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Fact]
    public static Task StaticScope() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public static Task AsServiceScope() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public static Task ParameterScope() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Fact]
    public static Task Recursive() {
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

        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public static void CycleError() {
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

        Assert.Single(diagnostics);
        Assert.Equal("CDI036", diagnostics[0].Id);
        Assert.Equal("Module cycle in ServiceProvider 'MyCode.TestProvider': ['MyCode.MyModule' -> 'MyCode.MyModule2' -> 'MyCode.MyModule']", diagnostics[0].GetMessage());
    }
}
