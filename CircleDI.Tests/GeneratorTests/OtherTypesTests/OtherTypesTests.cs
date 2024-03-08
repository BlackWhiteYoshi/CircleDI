using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Data.Common;
using System.Reflection.Metadata;

namespace CircleDI.Tests;

/// <summary>
/// <para>Tests for Services that are ValueTypes:</para>
/// <para>
/// - struct<br />
/// - native types
/// </para>
/// </summary>
public static class OtherTypesTests {
    [Fact]
    public static Task Struct() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency, TestDependency>]
            public sealed partial class TestProvider;
            
            public interface ITestService;
            [method: Constructor]
            public struct TestService(ITestDependency testDependency) : ITestService;

            public interface ITestDependency;
            public struct TestDependency : ITestDependency;

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
    public static Task DelegateStatic() {
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
    public static Task DelegateNamed() {
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
    public static Task DelegateWithGetAccessorMethod() {
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
    public static Task DelegateScoped() {
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
    public static Task DelegateStaticScoped() {
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
    public static Task DelegateInjectionConstructor() {
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
    public static Task DelegateInjectionProperty() {
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
    public static void DelegateWrongTypeFails() {
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
    public static void DelegateNoImplementationFails() {
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
    public static void DelegateWrongNumberOfParametersFails() {
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
    public static void DelegateWrongParameterTypeFails() {
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
    public static void DelegateManyWrongParameterTypesFailingWithMultipleErrors() {
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
    public static void DelegateWrongReturnTypeFails() {
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
    public static void DelegateAllTypesWrongFailingWithMultipleErrors() {
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


    [Fact]
    public static Task GenericClass() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService<string>, TestService<string>>(Name = "TestServiceString")]
            [Singleton<ITestService<int>, TestService<int>>(Name = "TestServiceInt")]
            public sealed partial class TestProvider;

            public interface ITestService<T>;
            public sealed class TestService<T> : ITestService<T>;

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
    public static void InterfaceFails() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITest>]
            public sealed partial class TestProvider;
            
            public interface ITest;

            """;

        _ = input.GenerateSourceText(out _, out ImmutableArray<Diagnostic> diagnostics);

        Assert.Single(diagnostics);
        Assert.Equal("CDI017", diagnostics[0].Id);
        Assert.Equal("ServiceImplementation 'MyCode.ITest' does not exist or has no accessible constructor", diagnostics[0].GetMessage());
    }
}
