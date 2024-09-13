using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Tests;

/// <summary>
/// Tests for registering a [Delegate]
/// </summary>
public static class DelegateTests {
    [Fact]
    public static Task Delegate() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<TestDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                private string DelegateImpl(int number) => string.Empty;
            }

            public delegate string TestDelegate(int number);

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
            [Delegate(typeof(TestDelegate), nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                private string DelegateImpl(int number) => string.Empty;
            }

            public delegate string TestDelegate(int number);

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
            [Delegate<TestDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                private static string DelegateImpl(int number) => string.Empty;
            }

            public delegate string TestDelegate(int number);

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
    public static Task Named() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<TestDelegate>(nameof(DelegateImpl), Name = "Asdf")]
            public sealed partial class TestProvider {
                private string DelegateImpl(int number) => string.Empty;
            }

            public delegate string TestDelegate(int number);

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
    public static Task WithGetAccessorMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<TestDelegate>(nameof(DelegateImpl), GetAccessor = GetAccess.Method)]
            public sealed partial class TestProvider {
                private string DelegateImpl(int number) => string.Empty;
            }

            public delegate string TestDelegate(int number);

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
            [Delegate<TestDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private string DelegateImpl(int number) => string.Empty;
                }
            }

            public delegate string TestDelegate(int number);

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
    public static Task StaticScoped() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<TestDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                public sealed partial class Scope {
                    private static string DelegateImpl(int number) => string.Empty;
                }
            }

            public delegate string TestDelegate(int number);

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
    public static Task InjectionConstructor() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Delegate<TestDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                private string DelegateImpl(int number) => string.Empty;
            }

            public interface ITestService;
            public sealed class TestService(TestDelegate testDelegate) : ITestService;

            public delegate string TestDelegate(int number);

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
    public static Task InjectionProperty() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Delegate<TestDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                private string DelegateImpl(int number) => string.Empty;
            }

            public interface ITestService;
            public sealed class TestService : ITestService {
                public required TestDelegate TestDelegate { private get; init; }
            }

            public delegate string TestDelegate(int number);

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
    public static void WrongTypeFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<TestService>(nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                private string DelegateImpl(int number) => string.Empty;
            }

            public sealed class TestService;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI012", diagnostics[0].Id);
        Assert.Equal("Delegate service 'MyCode.TestService' is not a Delegate type", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void NoImplementationFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<TestDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider;

            public delegate string TestDelegate(int number);

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI013", diagnostics[0].Id);
        Assert.Equal("No method with the name 'DelegateImpl' in class 'MyCode.TestProvider' could be found", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void WrongNumberOfParametersFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<TestDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                private string DelegateImpl(int number, int number2) => string.Empty;
            }

            public delegate string TestDelegate(int number);

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI014", diagnostics[0].Id);
        Assert.Equal("Method 'DelegateImpl' has wrong number of parameters: '2' <-> '1' expected", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void WrongParameterTypeFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<TestDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                private string DelegateImpl(string number) => string.Empty;
            }

            public delegate string TestDelegate(int number);

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI015", diagnostics[0].Id);
        Assert.Equal("Method 'DelegateImpl' has wrong parameter type at position '1': 'string' <-> 'int' expected", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void ManyWrongParameterTypesFailingWithMultipleErrors() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<TestDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                private string DelegateImpl(string number, float number2) => string.Empty;
            }

            public delegate string TestDelegate(int number, double number2);

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Equal(2, diagnostics.Length);
        Assert.Equal("CDI015", diagnostics[0].Id);
        Assert.Equal("Method 'DelegateImpl' has wrong parameter type at position '1': 'string' <-> 'int' expected", diagnostics[0].GetMessage());
        Assert.Equal("CDI015", diagnostics[1].Id);
        Assert.Equal("Method 'DelegateImpl' has wrong parameter type at position '2': 'float' <-> 'double' expected", diagnostics[1].GetMessage());
    }

    [Fact]
    public static void WrongReturnTypeFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<TestDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                private int DelegateImpl(int number) => number;
            }

            public delegate string TestDelegate(int number);

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI016", diagnostics[0].Id);
        Assert.Equal("Method 'DelegateImpl' has wrong return type: 'int' <-> 'string' expected", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void AllTypesWrongFailingWithMultipleErrors() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<TestDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                private int DelegateImpl(string number, string number2) => number + number2;
            }

            public delegate string TestDelegate(int number, int number2);

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Equal(3, diagnostics.Length);
        Assert.Equal("CDI015", diagnostics[0].Id);
        Assert.Equal("Method 'DelegateImpl' has wrong parameter type at position '1': 'string' <-> 'int' expected", diagnostics[0].GetMessage());
        Assert.Equal("CDI015", diagnostics[1].Id);
        Assert.Equal("Method 'DelegateImpl' has wrong parameter type at position '2': 'string' <-> 'int' expected", diagnostics[1].GetMessage());
        Assert.Equal("CDI016", diagnostics[2].Id);
        Assert.Equal("Method 'DelegateImpl' has wrong return type: 'int' <-> 'string' expected", diagnostics[2].GetMessage());
    }
}
