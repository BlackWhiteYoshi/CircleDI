using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Tests;

/// <summary>
/// Tests for registering a [Delegate]
/// </summary>
public sealed class DelegateTests {
    [Test]
    public async ValueTask Delegate() {
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
            [Delegate(typeof(TestDelegate), nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                private string DelegateImpl(int number) => string.Empty;
            }

            public delegate string TestDelegate(int number);

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
            [Delegate<TestDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider {
                private static string DelegateImpl(int number) => string.Empty;
            }

            public delegate string TestDelegate(int number);

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
    public async ValueTask Named() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask WithGetAccessorMethod() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask StaticScoped() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask InjectionConstructor() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask InjectionProperty() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }


    [Test]
    public async ValueTask WrongTypeFails() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI019");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Delegate service 'MyCode.TestService' is not a Delegate type");
    }

    [Test]
    public async ValueTask NoImplementationFails() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Delegate<TestDelegate>(nameof(DelegateImpl))]
            public sealed partial class TestProvider;

            public delegate string TestDelegate(int number);

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI020");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("No method with the name 'DelegateImpl' in class 'MyCode.TestProvider' could be found");
    }

    [Test]
    public async ValueTask WrongNumberOfParametersFails() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI021");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Method 'DelegateImpl' has wrong number of parameters: '2' <-> '1' expected");
    }

    [Test]
    public async ValueTask WrongParameterTypeFails() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI022");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Method 'DelegateImpl' has wrong parameter type at position '1': 'string' <-> 'int' expected");
    }

    [Test]
    public async ValueTask ManyWrongParameterTypesFailingWithMultipleErrors() {
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

        await Assert.That(diagnostics.Length).IsEqualTo(2);
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI022");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Method 'DelegateImpl' has wrong parameter type at position '1': 'string' <-> 'int' expected");
        await Assert.That(diagnostics[1].Id).IsEqualTo("CDI022");
        await Assert.That(diagnostics[1].GetMessage()).IsEqualTo("Method 'DelegateImpl' has wrong parameter type at position '2': 'float' <-> 'double' expected");
    }

    [Test]
    public async ValueTask WrongReturnTypeFails() {
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

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI023");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Method 'DelegateImpl' has wrong return type: 'int' <-> 'string' expected");
    }

    [Test]
    public async ValueTask AllTypesWrongFailingWithMultipleErrors() {
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

        await Assert.That(diagnostics.Length).IsEqualTo(3);
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDI022");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Method 'DelegateImpl' has wrong parameter type at position '1': 'string' <-> 'int' expected");
        await Assert.That(diagnostics[1].Id).IsEqualTo("CDI022");
        await Assert.That(diagnostics[1].GetMessage()).IsEqualTo("Method 'DelegateImpl' has wrong parameter type at position '2': 'string' <-> 'int' expected");
        await Assert.That(diagnostics[2].Id).IsEqualTo("CDI023");
        await Assert.That(diagnostics[2].GetMessage()).IsEqualTo("Method 'DelegateImpl' has wrong return type: 'int' <-> 'string' expected");
    }
}
