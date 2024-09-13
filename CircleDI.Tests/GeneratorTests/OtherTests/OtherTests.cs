using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Tests;

/// <summary>
/// <para>Tests for extraordinary things:</para>
/// <para>
/// - record<br />
/// - native type<br />
/// - generic<br />
/// - ref
/// </para>
/// </summary>
public static class OtherTests {
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
