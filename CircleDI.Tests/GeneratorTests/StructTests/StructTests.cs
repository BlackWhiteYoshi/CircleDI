using CircleDI.Tests.GenerateSourceText;

namespace CircleDI.Tests;

/// <summary>
/// Tests related to the struct type
/// </summary>
public static class StructTests {
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
    public static Task Lazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService>(CreationTime = CreationTiming.Lazy)]
            public sealed partial class TestProvider;

            public struct TestService;

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
    public static Task withInterfaceLazy() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<ITestService, TestService>(CreationTime = CreationTiming.Lazy)]
            public sealed partial class TestProvider;

            public interface ITestService;
            public struct TestService : ITestService;

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
    public static Task LazyIDisposable() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService>(CreationTime = CreationTiming.Lazy)]
            public sealed partial class TestProvider;

            public struct TestService : System.IDisposable;

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
    public static Task LazyIAsyncDisposable() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService>(CreationTime = CreationTiming.Lazy)]
            public sealed partial class TestProvider;

            public struct TestService : System.IAsyncDisposable;

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
    public static Task LazyMultipleIAsyncDisposable() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<TestService>(CreationTime = CreationTiming.Lazy)]
            [Singleton<AnotherService>]
            public sealed partial class TestProvider;

            public struct TestService : System.IAsyncDisposable;
            public sealed class AnotherService : System.IAsyncDisposable;

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
}
