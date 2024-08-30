using CircleDI.Tests.GenerateSourceText;

namespace CircleDI.Tests;

public static class BlazorTests {
    [Fact]
    public static void GeneratesCircleDIComponentActivator() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider;

            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out _);
        
        _ = sourceTexts.Single((string sourceText) => sourceText.Contains("class CircleDIComponentActivator<TScopeProvider>"));
    }


    #region ComponentModuleAttribute

    [Fact]
    public static Task ComponentsGetGenerated() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode {
                [ComponentModule]
                public partial interface TestModule;

                public sealed class MyComponent : Microsoft.AspNetCore.Components.ComponentBase;
                public sealed class MyComponent2 : Microsoft.AspNetCore.Components.ComponentBase, IDisposable;
            }

            namespace Microsoft.AspNetCore.Components {
                public abstract class ComponentBase;
            }
            
            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out _);
        string componentModuleAttributes = sourceTexts.First((string sourceText) => sourceText.Contains("[global::CircleDIAttributes.TransientAttribute<"));

        return Verify(componentModuleAttributes);
    }

    [Fact]
    public static Task ComponentModuleWithHardTypeName() {
        const string input = """
            using CircleDIAttributes;
            
            public sealed partial class OuterWrapper<T> {
                public partial interface InnerWarpper {
                    [ComponentModule]
                    public partial record NestedModule<U>;
                }
            }

            namespace MyCode {
                public sealed class MyComponent : Microsoft.AspNetCore.Components.ComponentBase;
                public sealed class MyComponent2 : Microsoft.AspNetCore.Components.ComponentBase, IDisposable;
            }

            namespace Microsoft.AspNetCore.Components {
                public abstract class ComponentBase;
            }
            
            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out _);
        string componentModuleAttributes = sourceTexts.First((string sourceText) => sourceText.Contains("[global::CircleDIAttributes.TransientAttribute<"));

        return Verify(componentModuleAttributes);
    }

    #endregion


    #region DefaultServiceGeneration

    [Fact]
    public static Task GeneratesNoDefaultServices() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider(DefaultServiceGeneration = BlazorServiceGeneration.None)]
            public sealed partial class TestProvider;

            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        Assert.DoesNotContain("private global::Microsoft.Extensions.Configuration.IConfiguration GetConfiguration()", sourceTexts);
        return Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public static Task GeneratesServerDefaultServices() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider(DefaultServiceGeneration = BlazorServiceGeneration.Server)]
            public sealed partial class TestProvider;

            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out _);
        string defaultServicesPartial = sourceTexts.Single((string sourceText) => sourceText.Contains("private global::Microsoft.JSInterop.IJSRuntime GetJSRuntime()"));
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        return Verify($"""
            {defaultServicesPartial}

            -----
            Class
            -----

            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public static Task GeneratesWebassemblyDefaultServices() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider(DefaultServiceGeneration = BlazorServiceGeneration.Webassembly)]
            public sealed partial class TestProvider;

            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out _);
        string defaultServicesPartial = sourceTexts.Single((string sourceText) => sourceText.Contains("private global::Microsoft.JSInterop.IJSRuntime GetJSRuntime()"));
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        return Verify($"""
            {defaultServicesPartial}

            -----
            Class
            -----

            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public static Task GeneratesHybridServices() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider(DefaultServiceGeneration = BlazorServiceGeneration.Hybrid)]
            public sealed partial class TestProvider;

            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out _);
        string defaultServicesPartial = sourceTexts.Single((string sourceText) => sourceText.Contains("private global::Microsoft.JSInterop.IJSRuntime GetJSRuntime()"));
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        return Verify($"""
            {defaultServicesPartial}

            -----
            Class
            -----

            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Fact]
    public static Task GeneratesServerAndWebassemblyDefaultServices() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode;

            [ServiceProvider(DefaultServiceGeneration = BlazorServiceGeneration.ServerAndWebassembly)]
            public sealed partial class TestProvider;

            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out _);
        string defaultServicesPartial = sourceTexts.Single((string sourceText) => sourceText.Contains("private global::Microsoft.JSInterop.IJSRuntime GetJSRuntime()"));
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        return Verify($"""
            {defaultServicesPartial}

            -----
            Class
            -----

            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    #endregion


    #region AddRazorComponents

    [Fact]
    public static Task NoComponentGetsIncluded() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode {
                [ServiceProvider(AddRazorComponents = false)]
                public sealed partial class TestProvider;

                public sealed class MyComponent : Microsoft.AspNetCore.Components.ComponentBase;
                public sealed class MyComponent2 : Microsoft.AspNetCore.Components.ComponentBase, IDisposable;
            }

            namespace Microsoft.AspNetCore.Components {
                public abstract class ComponentBase;
            }
            
            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out _);
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
    public static Task ComponentsOverride() {
        const string input = """
            using CircleDIAttributes;
            
            namespace MyCode {
                [ServiceProvider]
                [Scoped<MyComponent>()]
                [Scoped<MyComponent2>()]
                public sealed partial class TestProvider;

                public sealed class MyComponent : Microsoft.AspNetCore.Components.ComponentBase;
                public sealed class MyComponent2 : Microsoft.AspNetCore.Components.ComponentBase, IDisposable;

                public sealed class MyComponent3 : Microsoft.AspNetCore.Components.ComponentBase;
                public sealed class MyComponent4 : Microsoft.AspNetCore.Components.ComponentBase, IDisposable;
            }

            namespace Microsoft.AspNetCore.Components {
                public abstract class ComponentBase;
            }
            
            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out _);
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

    #endregion
}
