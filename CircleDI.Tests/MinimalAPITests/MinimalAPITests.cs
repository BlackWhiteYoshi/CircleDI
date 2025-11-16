using CircleDI.Extensions;
using CircleDI.MinimalAPI.Defenitions;
using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Text;

namespace CircleDI.Tests;

public sealed class MinimalAPITests {
    #region EndpointExtension

    [Test]
    public async ValueTask EndpointParameterless() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get)]
                public static void MyHandler() { }
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
        string endpointExtensionSourceText = sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"));

        await Verify(endpointExtensionSourceText);
    }

    [Test]
    public async ValueTask EndpointOneParameter() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get)]
                public static void MyHandler(string str) { }
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
        string endpointExtensionSourceText = sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"));

        await Verify(endpointExtensionSourceText);
    }

    [Test]
    public async ValueTask EndpointMultipleParameter() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get)]
                public static void MyHandler(string str, int number, float value) { }
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
        string endpointExtensionSourceText = sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"));

        await Verify(endpointExtensionSourceText);
    }

    [Test]
    public async ValueTask EndpointOneDependency() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            [Singleton<IMyService, MyService>]
            public sealed partial class TestProvider;

            public interface IMyService;
            public sealed class MyService : IMyService;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get)]
                public static void MyHandler([Dependency] IMyService myService) { }
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
        string endpointExtensionSourceText = sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"));

        await Verify(endpointExtensionSourceText);
    }

    [Test]
    public async ValueTask EndpointMultipleDependencies() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            public partial interface ITestProvider;

            [ServiceProvider]
            [Singleton<IMyService, MyService>]
            [Singleton<MyService>(Name = "TheService")]
            public sealed partial class TestProvider;

            public interface IMyService;
            public sealed class MyService : IMyService;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get)]
                public static void MyHandler([Dependency] IMyService myService, [Dependency] MyService theService, [Dependency] ITestProvider testProvider) { }
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
        string endpointExtensionSourceText = sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"));

        await Verify(endpointExtensionSourceText);
    }

    [Test]
    public async ValueTask EndpointMultipleParameterAndDependencies() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            public partial interface ITestProvider;

            [ServiceProvider]
            [Singleton<IMyService, MyService>]
            [Singleton<MyService>(Name = "TheService")]
            public sealed partial class TestProvider;

            public interface IMyService;
            public sealed class MyService : IMyService;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get)]
                public static void MyHandler([Dependency] IMyService myService, string str, [Dependency] MyService theService, int number, [Dependency] ITestProvider testProvider, float value) { }
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
        string endpointExtensionSourceText = sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"));

        await Verify(endpointExtensionSourceText);
    }

    [Test]
    public async ValueTask EndpointMultiple() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get)]
                public static void MyHandlerGet() { }

                [Endpoint("Hello", Http.Post)]
                public static void MyHandlerPost() { }

                [Endpoint("Hello2", Http.Get)]
                public static void MyHandler2() { }
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
        string endpointExtensionSourceText = sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"));

        await Verify(endpointExtensionSourceText);
    }

    [Test]
    public async ValueTask EndpointParameterWithOneAttribute() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode {
                [ServiceProvider]
                public sealed partial class TestProvider;

                public static class Endpoints {
                    [Endpoint("Hello", Http.Get)]
                    public static void MyHandler([Microsoft.AspNetCore.Mvc.FromHeader(Name = "X-CUSTOM-HEADER")] string str) { }
                }
            }

            namespace Microsoft.AspNetCore.Mvc {
                public class FromHeaderAttribute : System.Attribute { public string? Name { get; set; } }
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
        string endpointExtensionSourceText = sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"));

        await Verify(endpointExtensionSourceText);
    }

    [Test]
    public async ValueTask EndpointParameterWithMultipleAttributes() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode {
                using Microsoft.AspNetCore.Mvc;
                using Microsoft.Extensions.DependencyInjection;

                [ServiceProvider]
                public sealed partial class TestProvider;

                public static class Endpoints {
                    [Endpoint("Hello", Http.Post)]
                    public static void MyHandler([FromBody][FromHeader(Name = "X-CUSTOM-HEADER")][FromQuery(Name = "p")][FromKeyedServices("test")] string str) { }
                }
            }

            namespace Microsoft.AspNetCore.Mvc {
                public class FromBodyAttribute : System.Attribute;
                public class FromHeaderAttribute : System.Attribute { public string? Name { get; set; } }
                public class FromQueryAttribute : System.Attribute { public string? Name { get; set; } }
            }

            namespace Microsoft.Extensions.DependencyInjection {
                public class FromKeyedServicesAttribute(object key) : System.Attribute;
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
        string endpointExtensionSourceText = sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"));

        await Verify(endpointExtensionSourceText);
    }


    [Test]
    public async ValueTask EndpointAttributePattern() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider;

            public static class Endpoints {
                [Endpoint("/users/{userId}/books/{bookId}", Http.Get)]
                public static void MyHandler() { }
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
        string endpointExtensionSourceText = sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"));

        await Verify(endpointExtensionSourceText);
    }

    [Test]
    public async ValueTask EndpointAttributeHttpMethod() {
        int httpCount = Enum.GetValues<Http>().Length;

        StringBuilder builder = new(1024);
        for (int i = 0; i <= httpCount; i++) {
            Http httpMethod = (Http)i;

            string input = $$"""
                using CircleDIAttributes;

                namespace MyCode;

                [ServiceProvider]
                public sealed partial class TestProvider;

                public static class Endpoints {
                    [Endpoint("Hello", Http.{{httpMethod}})]
                    public static void MyHandler() { }
                }

                """;

            string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
            builder.AppendInterpolation($"""


                -------------
                HTTP method: {(Http)i}
                -------------

                {sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"))}
                """);
        }

        await Verify(builder.ToString());
    }

    [Test]
    public async ValueTask EndpointAttributeRouteHandler() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode {
                [ServiceProvider]
                public sealed partial class TestProvider;

                public static class Endpoints {
                    public static void MyHandlerBuilder(Microsoft.AspNetCore.Builder.RouteHandlerBuilder builder) { }

                    [Endpoint("Hello", Http.Get, RouteBuilder = nameof(MyHandlerBuilder))]
                    public static void MyHandler() { }
                }
            }

            namespace Microsoft.AspNetCore.Builder {
                public class RouteHandlerBuilder;
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
        string endpointExtensionSourceText = sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"));

        await Verify(endpointExtensionSourceText);
    }


    [Test]
    public async ValueTask EndpointWithoutProvider() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get)]
                public static void MyHandler() { }
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
        string endpointExtensionSourceText = sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"));

        await Verify(endpointExtensionSourceText);
    }

    [Test]
    public async ValueTask EndpointScopeProviderGeneric() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            public partial interface ITestProvider {
                public partial interface IScope<T>;
            }

            [ServiceProvider<ITestProvider>]
            public sealed partial class TestProvider;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get)]
                public static void MyHandler([Dependency] ITestProvider testProvider) { }
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
        string endpointExtensionSourceText = sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"));

        await Verify(endpointExtensionSourceText);
    }

    [Test]
    public async ValueTask EndpointScopeProviderMultipleGenerics() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            public sealed class Wrapper<T> {
                public partial interface ITestProvider {
                    public partial interface IScope<U, V>;
                }
            }

            [ServiceProvider<Wrapper<T>.ITestProvider>]
            public sealed partial class TestProvider;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get)]
                public static void MyHandler([Dependency] Wrapper<T>.ITestProvider testProvider) { }
            }

            """;

        string[] sourceTexts = input.GenerateSourceTextMinimalAPI(out _, out _);
        string endpointExtensionSourceText = sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension"));

        await Verify(endpointExtensionSourceText);
    }

    #endregion


    #region Error

    [Test]
    public async ValueTask ErrorNonStaticEEndpoint() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get)]
                public void MyHandler() { }
            }

            """;

        _ = input.GenerateSourceTextMinimalAPI(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDIM01");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("The endpoint method 'MyCode.Endpoints.MyHandler' must be static");
    }

    [Test]
    public async ValueTask ErrorGenericEEndpoint() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get)]
                public static void MyHandler<T>() { }
            }

            """;

        _ = input.GenerateSourceTextMinimalAPI(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDIM02");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("The endpoint method 'MyCode.Endpoints.MyHandler' must be non generic");
    }

    [Test]
    public async ValueTask ErrorMissingRouteBuilderMethod() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get, RouteBuilder = "MyHandlerBuilder")]
                public static void MyHandler() { }
            }

            """;

        _ = input.GenerateSourceTextMinimalAPI(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDIM03");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("No method with the name 'MyHandlerBuilder' in class 'MyCode.Endpoints' could be found");
    }

    [Test]
    public async ValueTask ErrorRouteBuilderNonStatic() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode {
                [ServiceProvider]
                public sealed partial class TestProvider;

                public static class Endpoints {
                    public void MyHandlerBuilder(Microsoft.AspNetCore.Builder.RouteHandlerBuilder builder) { }

                    [Endpoint("Hello", Http.Get, RouteBuilder = nameof(MyHandlerBuilder))]
                    public static void MyHandler() { }
                }
            }

            namespace Microsoft.AspNetCore.Builder {
                public class RouteHandlerBuilder;
            }

            """;

        _ = input.GenerateSourceTextMinimalAPI(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDIM04");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("The RouteBuilder method 'MyHandlerBuilder' must be static");
    }

    [Test]
    public async ValueTask ErrorRouteBuilderGeneric() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode {
                [ServiceProvider]
                public sealed partial class TestProvider;

                public static class Endpoints {
                    public static void MyHandlerBuilder<T>(Microsoft.AspNetCore.Builder.RouteHandlerBuilder builder) { }

                    [Endpoint("Hello", Http.Get, RouteBuilder = nameof(MyHandlerBuilder))]
                    public static void MyHandler() { }
                }
            }

            namespace Microsoft.AspNetCore.Builder {
                public class RouteHandlerBuilder;
            }

            """;

        _ = input.GenerateSourceTextMinimalAPI(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDIM05");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("The RouteBuilder method 'MyHandlerBuilder' must non generic");
    }

    [Test]
    [Arguments("string str")]
    [Arguments("RouteHandlerBuilder builder")]
    [Arguments("RouteHandlerBuilder builder, string str")]
    [Arguments("")]
    public async ValueTask ErrorRouteBuilderParameterList(string parameter) {
        string input = $$"""
            using CircleDIAttributes;

            namespace MyCode {
                [ServiceProvider]
                public sealed partial class TestProvider;

                public static class Endpoints {
                    public static void MyHandlerBuilder({{parameter}}) { }

                    [Endpoint("Hello", Http.Get, RouteBuilder = nameof(MyHandlerBuilder))]
                    public static void MyHandler() { }
                }
            }

            namespace Microsoft.AspNetCore.Builder {
                public class RouteHandlerBuilder;
            }

            """;

        _ = input.GenerateSourceTextMinimalAPI(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDIM06");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("The RouteBuilder method 'MyHandlerBuilder' must have only one parameter of type 'Microsoft.AspNetCore.Builder.RouteHandlerBuilder'");
    }

    [Test]
    public async ValueTask ErrorMultipleSameEndpoint() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get)]
                public static void MyHandler() { }

                [Endpoint("Hello", Http.Get)]
                public static void MyHandler2() { }
            }

            """;

        _ = input.GenerateSourceTextMinimalAPI(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDIM07");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("The endpoint \"Hello\" with HTTP method 'Get' has multiple registrations");
    }

    [Test]
    public async ValueTask ErrorMultipleEndpointServiceProvider() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            [ServiceProvider]
            public sealed partial class TestProvider;

            [ServiceProvider]
            public sealed partial class TestProvider2;

            """;

        _ = input.GenerateSourceTextMinimalAPI(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDIM08");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Multiple Endpoint ServiceProviders, at most one is allowed. Change the property \"EndpointProvider\" to false to change the ServiceProvider to a normal provider.");
    }

    [Test]
    public async ValueTask ErrorEndpointDependencyWithoutServiceProvider() {
        const string input = """
            using CircleDIAttributes;

            namespace MyCode;

            public static class Endpoints {
                [Endpoint("Hello", Http.Get)]
                public static void MyHandler([Dependency] int number) { }
            }

            """;

        _ = input.GenerateSourceTextMinimalAPI(out _, out ImmutableArray<Diagnostic> diagnostics);

        await Assert.That(diagnostics).HasSingleItem();
        await Assert.That(diagnostics[0].Id).IsEqualTo("CDIM09");
        await Assert.That(diagnostics[0].GetMessage()).IsEqualTo("Endpoint has dependency without ServiceProvider. Either remove the [Dependency]-attribute or create a ServiceProvider with \"EndpointProvider\" set to default or true.");
    }

    #endregion
}
