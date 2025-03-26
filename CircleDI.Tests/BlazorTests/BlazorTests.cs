using CircleDI.Tests.GenerateSourceText;

namespace CircleDI.Tests;

public sealed class BlazorTests {
    [Test]
    public async ValueTask GeneratesCircleDIComponentActivator() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider;

            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out _);

        await Assert.That(sourceTexts.SingleOrDefault((string sourceText) => sourceText.Contains("class CircleDIComponentActivator<TScopeProvider>"))).IsNotNull();
    }


    #region ComponentModuleAttribute

    [Test]
    public async ValueTask ComponentsGetGenerated() {
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

        await Verify(componentModuleAttributes);
    }

    [Test]
    public async ValueTask ComponentModuleWithHardTypeName() {
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

        await Verify(componentModuleAttributes);
    }

    #endregion


    #region DefaultServiceGeneration

    [Test]
    public async ValueTask GeneratesNoDefaultServices() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider(DefaultServiceGeneration = BlazorServiceGeneration.None)]
            public sealed partial class TestProvider;

            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out _);
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Assert.That(sourceTexts).DoesNotContain("private global::Microsoft.Extensions.Configuration.IConfiguration GetConfiguration()");
        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask GeneratesServerDefaultServices() {
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

        await Verify($"""
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

    [Test]
    public async ValueTask GeneratesWebassemblyDefaultServices() {
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

        await Verify($"""
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

    [Test]
    public async ValueTask GeneratesHybridServices() {
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

        await Verify($"""
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

    [Test]
    public async ValueTask GeneratesGenericServiceLogger() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode {
                [ServiceProvider]
                [Singleton<TestService>]
                public sealed partial class TestProvider;

                public sealed class TestService(Microsoft.Extensions.Logging.ILogger<TestService> logger);
            }

            namespace Microsoft.Extensions.Logging {
                public interface ILogger<TestService>;
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out var t);
        string defaultServicesPartial = sourceTexts.Single((string sourceText) => sourceText.Contains("private global::Microsoft.JSInterop.IJSRuntime GetJSRuntime()"));
        string sourceTextClass = sourceTexts[^2];
        string sourceTextInterface = sourceTexts[^1];

        await Verify($"""
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

    [Test]
    public async ValueTask NoComponentGetsIncluded() {
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

        await Verify($"""
            {sourceTextClass}

            ---------
            Interface
            ---------

            {sourceTextInterface}
            """);
    }

    [Test]
    public async ValueTask ComponentsOverride() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode {
                [ServiceProvider]
                [Scoped<MyComponent>()]
                [Scoped<MyComponent2>()]
                public sealed partial class TestProvider;

                public sealed class MyComponent : Microsoft.AspNetCore.Components.ComponentBase;
                public sealed class MyComponent2 : Microsoft.AspNetCore.Components.ComponentBase, System.IDisposable;

                public sealed class MyComponent3 : Microsoft.AspNetCore.Components.ComponentBase;
                public sealed class MyComponent4 : Microsoft.AspNetCore.Components.ComponentBase, System.IDisposable;
            }

            namespace Microsoft.AspNetCore.Components {
                public abstract class ComponentBase;
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out _);
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
    public async ValueTask ComponentModuleOverride() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode {                
                [Transient<MyComponent>(Name = "Overridden")]
                [ComponentModule]
                public readonly partial struct Module;

                public sealed class MyComponent : Microsoft.AspNetCore.Components.ComponentBase;
                public sealed class MyComponent2 : Microsoft.AspNetCore.Components.ComponentBase, IDisposable;
            }

            namespace Microsoft.AspNetCore.Components {
                public abstract class ComponentBase;
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextBlazor(out _, out _);
        string sourceTextComponentModule = sourceTexts[^1];

        await Verify(sourceTextComponentModule);
    }

    #endregion
}
