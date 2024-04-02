using CircleDI.MinimalAPI.Defenitions;
using CircleDI.Tests.GenerateSourceText;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using System.Text;

namespace CircleDI.Tests;

public static class MinimalAPITests {
    #region EndpointExtension

    [Fact]
    public static Task EndpointParameterless() {
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

        return Verify(endpointExtensionSourceText);
    }

    [Fact]
    public static Task EndpointOneParameter() {
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

        return Verify(endpointExtensionSourceText);
    }

    [Fact]
    public static Task EndpointMultipleParameter() {
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

        return Verify(endpointExtensionSourceText);
    }

    [Fact]
    public static Task EndpointOneDependency() {
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

        return Verify(endpointExtensionSourceText);
    }

    [Fact]
    public static Task EndpointMultipleDependencies() {
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

        return Verify(endpointExtensionSourceText);
    }

    [Fact]
    public static Task EndpointMultipleParameterAndDependencies() {
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

        return Verify(endpointExtensionSourceText);
    }

    [Fact]
    public static Task EndpointMultiple() {
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

        return Verify(endpointExtensionSourceText);
    }

    [Fact]
    public static Task EndpointParameterWithOneAttribute() {
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

        return Verify(endpointExtensionSourceText);
    }

    [Fact]
    public static Task EndpointParameterWithMultipleAttributes() {
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

        return Verify(endpointExtensionSourceText);
    }


    [Fact]
    public static Task EndpointAttributePattern() {
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

        return Verify(endpointExtensionSourceText);
    }

    [Fact]
    public static Task EndpointAttributeHttpMethod() {
        int httpCount = Enum.GetValues(typeof(Http)).Length;

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

            builder.Append($"""
                

                -------------
                HTTP method: {((Http)i).ToString()}
                -------------

           
                """);
            builder.Append(sourceTexts.First((string sourceText) => sourceText.Contains("partial class EndpointExtension")));
        }

        return Verify(builder.ToString());
    }

    [Fact]
    public static Task EndpointAttributeRouteHandler() {
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

        return Verify(endpointExtensionSourceText);
    }


    [Fact]
    public static Task EndpointScopeProviderGeneric() {
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

        return Verify(endpointExtensionSourceText);
    }

    [Fact]
    public static Task EndpointScopeProviderMultipleGenerics() {
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

        return Verify(endpointExtensionSourceText);
    }

    #endregion


    #region Error

    [Fact]
    public static void ErrorNonStaticEEndpoint() {
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

        Assert.Single(diagnostics);
        Assert.Equal("CDIM01", diagnostics[0].Id);
        Assert.Equal("The endpoint method 'MyCode.Endpoints.MyHandler' must be static", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void ErrorGenericEEndpoint() {
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

        Assert.Single(diagnostics);
        Assert.Equal("CDIM02", diagnostics[0].Id);
        Assert.Equal("The endpoint method 'MyCode.Endpoints.MyHandler' must be non generic", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void ErrorMissingRouteBuilderMethod() {
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

        Assert.Single(diagnostics);
        Assert.Equal("CDIM03", diagnostics[0].Id);
        Assert.Equal("No method with the name 'MyHandlerBuilder' in class 'MyCode.Endpoints' could be found", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void ErrorRouteBuilderNonStatic() {
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

        Assert.Single(diagnostics);
        Assert.Equal("CDIM04", diagnostics[0].Id);
        Assert.Equal("The RouteBuilder method 'MyHandlerBuilder' must be static", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void ErrorRouteBuilderGeneric() {
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

        Assert.Single(diagnostics);
        Assert.Equal("CDIM05", diagnostics[0].Id);
        Assert.Equal("The RouteBuilder method 'MyHandlerBuilder' must non generic", diagnostics[0].GetMessage());
    }

    [Theory]
    [InlineData("string str")]
    [InlineData("RouteHandlerBuilder builder")]
    [InlineData("RouteHandlerBuilder builder, string str")]
    [InlineData("")]
    public static void ErrorRouteBuilderParameterList(string parameter) {
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

        Assert.Single(diagnostics);
        Assert.Equal("CDIM06", diagnostics[0].Id);
        Assert.Equal("The RouteBuilder method 'MyHandlerBuilder' must have only one parameter of type 'Microsoft.AspNetCore.Builder.RouteHandlerBuilder'", diagnostics[0].GetMessage());
    }

    [Fact]
    public static void ErrorMultipleSameEndpoint() {
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

        Assert.Single(diagnostics);
        Assert.Equal("CDIM07", diagnostics[0].Id);
        Assert.Equal("The endpoint \"Hello\" with HTTP method 'Get' has multiple registrations", diagnostics[0].GetMessage());
    }

    #endregion
}
