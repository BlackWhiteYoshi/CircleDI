using CircleDI.MinimalAPI.Defenitions;
using Microsoft.CodeAnalysis;
using System.Text;

namespace CircleDI.MinimalAPI;

/// <summary>
/// Collection of global <see cref="DiagnosticDescriptor"/> objects and methods to create <see cref="Diagnostic"/> objects from them.
/// </summary>
public static class DiagnosticErrors {
    #region Endpoint

    public static Diagnostic CreateEndpointMethodNonStaticError(this AttributeData endpointAttribute, MethodName methodName)
        => Diagnostic.Create(EndpointMethodNonStatic, endpointAttribute.ToLocation(), [methodName.CreateFullyQualifiedName()]);

    private static DiagnosticDescriptor EndpointMethodNonStatic { get; } = new(
        id: "CDIM01",
        title: "Endpoint method must be static",
        messageFormat: "The endpoint method '{0}' must be static",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateEndpointMethodGenericError(this AttributeData endpointAttribute, MethodName methodName)
        => Diagnostic.Create(EndpointMethodGeneric, endpointAttribute.ToLocation(), [methodName.CreateFullyQualifiedName()]);

    private static DiagnosticDescriptor EndpointMethodGeneric { get; } = new(
        id: "CDIM02",
        title: "Endpoint method must be non generic",
        messageFormat: "The endpoint method '{0}' must be non generic",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateMissingRouteBuilderMethodError(this AttributeData endpointAttribute, INamedTypeSymbol container, string methodName)
       => Diagnostic.Create(MissingRouteBuilderMethod, endpointAttribute.ToLocation(), [container.ToDisplayString(), methodName]);

    private static DiagnosticDescriptor MissingRouteBuilderMethod { get; } = new(
        id: "CDIM03",
        title: "Missing RouteBuilder method",
        messageFormat: "No method with the name '{1}' in class '{0}' could be found",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateRouteBuilderNonStaticError(this AttributeData endpointAttribute, string methodName)
           => Diagnostic.Create(RouteBuilderNonStatic, endpointAttribute.ToLocation(), [methodName]);

    private static DiagnosticDescriptor RouteBuilderNonStatic { get; } = new(
        id: "CDIM04",
        title: "RouteBuilder method must be static",
        messageFormat: "The RouteBuilder method '{0}' must be static",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateRouteBuilderGenericError(this AttributeData endpointAttribute, string methodName)
           => Diagnostic.Create(RouteBuilderGeneric, endpointAttribute.ToLocation(), [methodName]);

    private static DiagnosticDescriptor RouteBuilderGeneric { get; } = new(
        id: "CDIM05",
        title: "RouteBuilder method must be non generic",
        messageFormat: "The RouteBuilder method '{0}' must non generic",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateRouteBuilderParameterListError(this AttributeData endpointAttribute, string methodName)
           => Diagnostic.Create(RouteBuilderParameterList, endpointAttribute.ToLocation(), [methodName]);

    private static DiagnosticDescriptor RouteBuilderParameterList { get; } = new(
        id: "CDIM06",
        title: "RouteBuilder parameter must be only one RouteHandlerBuilder",
        messageFormat: "The RouteBuilder method '{0}' must have only one parameter of type 'Microsoft.AspNetCore.Builder.RouteHandlerBuilder'",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateMultipleSameEndpointError(this AttributeData endpointAttribute, string route, Http HttpMethod)
        => Diagnostic.Create(MultipleSameEndpoint, endpointAttribute.ToLocation(), [route, HttpMethod]);

    private static DiagnosticDescriptor MultipleSameEndpoint { get; } = new(
        id: "CDIM07",
        title: "Same Endpoint has multiple handlers",
        messageFormat: "The endpoint \"{0}\" with HTTP method '{1}' has multiple registrations",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateMultipleEndpointServiceProviderError(this AttributeData serviceProviderAttribute, AttributeData otherServiceProviderAttribute)
        => Diagnostic.Create(MultipleEndpointServiceProvider, serviceProviderAttribute.ToLocation(), additionalLocations: otherServiceProviderAttribute.ToLocationList());

    private static DiagnosticDescriptor MultipleEndpointServiceProvider { get; } = new(
        id: "CDIM08",
        title: "Multiple Endpoint ServiceProviders",
        messageFormat: "Multiple Endpoint ServiceProviders, at most one is allowed. Change the property \"EndpointProvider\" to false to change the ServiceProvider to a normal provider.",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateEndpointDependencyWithoutServiceProviderError(this AttributeData endpointAttribute)
        => Diagnostic.Create(EndpointDependencyWithoutServiceProvider, endpointAttribute.ToLocation());

    private static DiagnosticDescriptor EndpointDependencyWithoutServiceProvider { get; } = new(
        id: "CDIM09",
        title: "Endpoint has Dependency without ServiceProvider",
        messageFormat: "Endpoint has dependency without ServiceProvider. Either remove the [Dependency]-attribute or create a ServiceProvider with \"EndpointProvider\" set to default or true.",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    #endregion



    private static Location? ToLocation(this AttributeData attributeData)
        => attributeData.ApplicationSyntaxReference switch {
            SyntaxReference reference => Location.Create(reference.SyntaxTree, reference.Span),
            _ => null
        };

    private static Location[] ToLocationList(this AttributeData attributeData)
        => attributeData.ApplicationSyntaxReference switch {
            SyntaxReference reference => [Location.Create(reference.SyntaxTree, reference.Span)],
            _ => []
        };

    private static string CreateFullyQualifiedName(this MethodName methodName) {
        StringBuilder builder = new();
        methodName.AppendFullyQualifiedName(builder);
        return builder.ToString();
    }
}
