using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Tests;

/// <summary>
/// <para>Tests for other Types:</para>
/// <para>
/// - struct<br />
/// - record<br />
/// - native types
/// </para>
/// </summary>
public static class OtherTypesTests {
    [Fact]
    public static Task ServiceProviderStruct() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial struct TestProvider {
                public sealed partial struct Scope;
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
    public static Task ServiceProviderRecord() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial record TestProvider {
                public sealed partial record Scope;
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
    public static Task ServiceProviderRecordClass() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial record class TestProvider {
                public sealed partial record class Scope;
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
    public static Task ServiceProviderRecordStruct() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial record struct TestProvider {
                public sealed partial record struct Scope;
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
    public static Task Generic() {
        const string input = """
            using CircleDIAttributes;
            using System;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService<TestParameter>>]
            [Singleton<TestService<int>>(Name = $"{nameof(TestService<int>)}Int")]
            [Singleton<TestService<string>>(Name = $"{nameof(TestService<string>)}String")]
            [Singleton<IComparable, int>]
            public sealed partial class TestProvider;


            public sealed class TestService<T>;
            public sealed class TestParameter;

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
    public static Task RecordClass() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency, TestDependency>]
            public sealed partial class TestProvider;

            public interface ITestService;
            public record class TestService(ITestDependency testDependency) : ITestService;

            public interface ITestDependency;
            public record class TestDependency : ITestDependency;

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
    public static Task RecordStruct() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>]
            [Singleton<ITestDependency, TestDependency>]
            public sealed partial class TestProvider;

            public interface ITestService;
            [method: Constructor]
            public record struct TestService(ITestDependency testDependency) : ITestService;

            public interface ITestDependency;
            public record struct TestDependency : ITestDependency;

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
    public static Task NativeType() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestClass>]
            [Singleton<ITestStruct, TestStruct>]
            [Singleton<int>]
            public sealed partial class TestProvider;


            public class TestClass(int number);

            public interface ITestStruct;
            [method: Constructor]
            public struct TestStruct(int number);

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
    public static Task RefInClassProviderAndNoRefInStructProvider() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestStruct>]
            [Scoped<TestStruct>(Name = $"{nameof(TestStruct)}Scoped")]
            public partial struct TestProvider;

            public struct TestStruct;

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
    public static Task RefInOutInjection() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestStruct>]
            [Singleton<RefInject>]
            [Singleton<InInject>]
            [Singleton<OutInject>]
            [Singleton<RefReadonlyInject>]
            public sealed partial class TestProvider;

            public struct TestStruct;

            public sealed class RefInject(ref TestStruct testStruct);
            public sealed class InInject(in TestStruct testStruct);
            public sealed class OutInject(out TestStruct testStruct);
            public sealed class RefReadonlyInject(ref readonly TestStruct testStruct);

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
    public static Task DelegateTypeAsParameter() {
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
