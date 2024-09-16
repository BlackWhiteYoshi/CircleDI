using CircleDI.Defenitions;
using CircleDI.Extensions;
using CircleDI.Generation;
using CircleDI.MinimalAPI.Defenitions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.ObjectPool;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using ServiceProviderWithEndpointFlag = (CircleDI.Generation.ServiceProvider serviceProvider, bool endpointProvider);

namespace CircleDI.MinimalAPI.Generation;

[Generator(LanguageNames.CSharp)]
public sealed class CircleDIGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(static (IncrementalGeneratorPostInitializationContext context) => {
            // attributes
            context.AddSource("ServiceProviderAttribute.g.cs", CircleDI.MinimalAPI.Defenitions.Attributes.ServiceProviderAttribute);
            context.AddSource("ScopedProviderAttribute.g.cs", CircleDI.Defenitions.Attributes.ScopedProviderAttribute);
            context.AddSource("SingletonAttribute.g.cs", CircleDI.Defenitions.Attributes.SingletonAttribute);
            context.AddSource("ScopedAttribute.g.cs", CircleDI.Defenitions.Attributes.ScopedAttribute);
            context.AddSource("TransientAttribute.g.cs", CircleDI.Defenitions.Attributes.TransientAttribute);
            context.AddSource("DelegateAttribute.g.cs", CircleDI.Defenitions.Attributes.DelegateAttribute);
            context.AddSource("ImportAttribute.g.cs", CircleDI.Defenitions.Attributes.ImportAttribute);
            context.AddSource("DependencyAttribute.g.cs", CircleDI.Defenitions.Attributes.DependencyAttribute);
            context.AddSource("ConstructorAttribute.g.cs", CircleDI.Defenitions.Attributes.ConstructorAttribute);
            context.AddSource("EndpointrAttribute.g.cs", CircleDI.MinimalAPI.Defenitions.Attributes.EndpointAttribute);

            // enums
            context.AddSource("CreationTiming.g.cs", CircleDI.Defenitions.Attributes.CreationTimingEnum);
            context.AddSource("GetAccess.g.cs", CircleDI.Defenitions.Attributes.GetAccessEnum);
            context.AddSource("DisposeGeneration.g.cs", CircleDI.Defenitions.Attributes.DisposeGenerationEnum);
            context.AddSource("ImportMode.g.cs", CircleDI.Defenitions.Attributes.ImportModeEnum);
            context.AddSource("Http.g.cs", CircleDI.MinimalAPI.Defenitions.Attributes.HttpEnum);
        });


        ObjectPool<StringBuilder> stringBuilderPool = CircleDIBuilder.CreateStringBuilderPool();
        IncrementalValuesProvider<ServiceProviderWithEndpointFlag> serviceProviderList = context.RegisterServiceProviderAttribute("CircleDIAttributes.ServiceProviderAttribute", stringBuilderPool);
        IncrementalValuesProvider<ServiceProviderWithEndpointFlag> genericServiceProviderList = context.RegisterServiceProviderAttribute("CircleDIAttributes.ServiceProviderAttribute`1", stringBuilderPool);
        List<Diagnostic> endpointErrorList = [];


        // find all endpoints
        IncrementalValueProvider<ImmutableArray<Endpoint>> endpointList = context.SyntaxProvider.ForAttributeWithMetadataName(
            "CircleDIAttributes.EndpointAttribute",
            static (SyntaxNode syntaxNode, CancellationToken _) => syntaxNode is MethodDeclarationSyntax,
            (GeneratorAttributeSyntaxContext generatorAttributeSyntaxContext, CancellationToken _) => new Endpoint(generatorAttributeSyntaxContext, endpointErrorList)
        ).Collect().Select(CheckRouteConflicts);

        // all service providers with all endpoints
        IncrementalValueProvider<((ImmutableArray<ServiceProviderWithEndpointFlag> serviceProvider, ImmutableArray<ServiceProviderWithEndpointFlag> genericServiceProvider), ImmutableArray<Endpoint> endpointList)> serviceProviderListWithEndpointList =
            serviceProviderList.Collect()
            .Combine(genericServiceProviderList.Collect())
            .Combine(endpointList);

        // generate endpoint extension method
        context.RegisterSourceOutput(serviceProviderListWithEndpointList, (SourceProductionContext context, ((ImmutableArray<ServiceProviderWithEndpointFlag> serviceProvider, ImmutableArray<ServiceProviderWithEndpointFlag> genericServiceProvider) serviceProviders, ImmutableArray<Endpoint> endpointList) value)
            => GenerateEndpointMethod(context, value.serviceProviders.serviceProvider, value.serviceProviders.genericServiceProvider, value.endpointList, stringBuilderPool, endpointErrorList));
    }


    /// <summary>
    /// Checks if multiple (route + HTTP method) exists
    /// </summary>
    /// <param name="endpoints"></param>
    /// <param name="_"></param>
    /// <returns></returns>
    private static ImmutableArray<Endpoint> CheckRouteConflicts(ImmutableArray<Endpoint> endpoints, CancellationToken _) {
        foreach (IGrouping<string, Endpoint> endpointGroup in endpoints.GroupBy(endpoints => endpoints.Route)) {
            // Any = [0]
            // Get/Post/Put/Patch/Delete = [1..5]
            Span<int> httpMethodCount = stackalloc int[6];
            foreach (Endpoint endpoint in endpointGroup)
                httpMethodCount[(int)endpoint.HttpMethod]++;

            int sameEndpointCount = 2 - httpMethodCount[0];
            for (int i = 1; i < 6; i++)
                if (httpMethodCount[i] >= sameEndpointCount) {
                    Endpoint endpoint = endpointGroup.First();
                    endpoint.ErrorManager.AddMultipleSameEndpointError(endpointGroup.Key, (Http)i);
                    break;
                }
        }

        return endpoints;
    }

    /// <summary>
    /// <para>Init the endpoint methods and generates an endpoint extension method.</para>
    /// <para>
    /// If multiple ServiceProviders are "EndpointProvider", an error is reported instead.<br />
    /// If no ServiceProvider has "EndpointProvider" and one parameter has [Dependency], an error is reported instead.
    /// </para>
    /// </summary>
    /// <param name="context"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="genericServiceProvider"></param>
    /// <param name="endpointList"></param>
    /// <param name="stringBuilderPool"></param>
    /// <param name="endpointErrorList"></param>
    private static void GenerateEndpointMethod(
            SourceProductionContext context,
            ImmutableArray<ServiceProviderWithEndpointFlag> serviceProvider,
            ImmutableArray<ServiceProviderWithEndpointFlag> genericServiceProvider,
            ImmutableArray<Endpoint> endpointList,
            ObjectPool<StringBuilder> stringBuilderPool,
            List<Diagnostic> endpointErrorList) {

        foreach (Diagnostic error in endpointErrorList)
            context.ReportDiagnostic(error);

        ServiceProvider? endpointServiceProvider = null;
        foreach ((ServiceProvider listedProvider, bool endpointFlag) in serviceProvider.Concat(genericServiceProvider))
            if (endpointFlag)
                if (endpointServiceProvider is null)
                    endpointServiceProvider = listedProvider;
                else {
                    // many ServiceProviders
                    context.ReportDiagnostic(EndpointDiagnosticErrorManager.CreateMultipleEndpointServiceProviderError(endpointServiceProvider.ErrorManager.ServiceProviderAttribute, listedProvider.ErrorManager.ServiceProviderAttribute));
                    return;
                }

        TypeName? serviceTypeScopeProvider = null;
        if (endpointServiceProvider is not null) {
            // 1 ServiceProvider -> normal case
            if (endpointServiceProvider.ErrorManager.ErrorList.Count > 0)
                return;

            serviceTypeScopeProvider = endpointServiceProvider.HasInterface ? endpointServiceProvider.InterfaceIdentifierScope : endpointServiceProvider.IdentifierScope;

            endpointServiceProvider.CreateDependencyTree(endpointList.Select((Endpoint endpoint) => (endpoint.AsService, endpoint.ErrorManager.EndpointAttribute)));

            if (endpointServiceProvider.ErrorManager.ErrorList.Count > 0) {
                foreach (Diagnostic error in endpointServiceProvider.ErrorManager.ErrorList)
                    context.ReportDiagnostic(error);
                return;
            }
        }
        else {
            // 0 ServiceProviders -> allowed when no [Dependency]
            foreach (Endpoint endpoint in endpointList)
                foreach (ConstructorDependency dependency in endpoint.AsService.ConstructorDependencyList)
                    if (dependency.HasAttribute) {
                        context.ReportDiagnostic(endpoint.ErrorManager.AddEndpointDependencyWithoutServiceProviderError());
                        return;
                    }
        }

        if (endpointErrorList.Count > 0)
            return;


        StringBuilder builder = stringBuilderPool.Get();


        builder.Append("""
            // <auto-generated/>
            #pragma warning disable
            #nullable enable annotations


            namespace CircleDIAttributes;

            public static partial class EndpointExtension {
                public static void MapCircleDIEndpoints
            """);
        // type parameter
        if (serviceTypeScopeProvider != null) {
            int initalPosition = builder.Length;
            builder.Append('<');

            foreach (string typeParameter in serviceTypeScopeProvider.TypeParameterList)
                builder.AppendInterpolation($"{typeParameter}, ");
            foreach (TypeName typeName in serviceTypeScopeProvider.ContainingTypeList)
                foreach (string typeParameter in typeName.TypeParameterList)
                    builder.AppendInterpolation($"{typeParameter}, ");

            builder.Length--;
            if (builder.Length > initalPosition)
                builder[^1] = '>';
        }
        builder.Append("(this global::Microsoft.AspNetCore.Builder.WebApplication app) {\n");

        foreach (Endpoint endpoint in endpointList) {
            builder.Append("        "); // 8 spaces

            if (endpoint.MethodRouteBuilder is not null) {
                builder.Append("global::");
                endpoint.MethodRouteBuilder.AppendFullyQualifiedName(builder);
                builder.Append('(');
            }

            builder.AppendInterpolation($"""app.Map{endpoint.HttpMethod.AsString()}("{endpoint.Route}", (""");

            // lambda parameters
            if (endpoint.AsService.ConstructorDependencyList.Count > 0) {
                bool hasDependencyParameter = false;

                for (int i = 0; i < endpoint.AsService.ConstructorDependencyList.Count; i++) {
                    ConstructorDependency parameter = endpoint.AsService.ConstructorDependencyList[i];
                    if (parameter.HasAttribute) {
                        hasDependencyParameter = true;
                        continue;
                    }

                    if (endpoint.ParameterAttributesList[i]!.Length > 0) {
                        foreach (ParameterAttribute attribute in endpoint.ParameterAttributesList[i]!) {
                            builder.AppendInterpolation($"[global::{attribute.Name.AsClosedFullyQualified()}");

                            if (attribute.ParameterList.Length + attribute.PropertyList.Length > 0) {
                                builder.Append('(');

                                foreach (string attributeParameter in attribute.ParameterList)
                                    builder.AppendInterpolation($"{attributeParameter}, ");
                                foreach ((string attributeName, string attributeValue) in attribute.PropertyList)
                                    builder.AppendInterpolation($"{attributeName} = {attributeValue}, ");
                                builder.Length -= 2;

                                builder.Append(')');
                            }

                            builder.Append(']');
                        }
                        builder.Append(' ');
                    }
                    builder.AppendInterpolation($"global::{parameter.ServiceType.AsClosedFullyQualified()} {parameter.Name}, ");
                }

                if (hasDependencyParameter)
                    builder.AppendInterpolation($"global::{serviceTypeScopeProvider!.AsOpenFullyQualified()} {endpointServiceProvider!.Identifier.Name.AsFirstLower()}");
                else
                    builder.Length -= 2; // remove ", "
            }

            builder.Append(") => global::");
            endpoint.MethodHandler.AppendFullyQualifiedName(builder);
            builder.Append('(');

            // lambda agruments
            if (endpoint.AsService.ConstructorDependencyList.Count > 0) {
                foreach (ConstructorDependency parameter in endpoint.AsService.ConstructorDependencyList)
                    if (parameter.HasAttribute)
                        builder.AppendInterpolation($"{parameter.ByRef.AsString()}{endpointServiceProvider!.Identifier.Name.AsFirstLower()}.{parameter.Service!.AsServiceGetter()}, ");
                    else
                        builder.AppendInterpolation($"{parameter.Name}, ");
                builder.Length -= 2; // remove ", "
            }

            if (endpoint.MethodRouteBuilder is not null)
                builder.Append(')');
            builder.Append("));\n");
        }

        builder.Append($"    }}\n");
        builder.Append("}\n");


        context.AddSource("EndpointExtension.g.cs", builder.ToString());

        stringBuilderPool.Return(builder);
    }
}

file static class RegisterServiceProviderAttributeExtension {
    public static IncrementalValuesProvider<ServiceProviderWithEndpointFlag> RegisterServiceProviderAttribute(this IncrementalGeneratorInitializationContext context, string serviceProviderAttributeName, ObjectPool<StringBuilder> stringBuilderPool) {
        // all service providers
        IncrementalValuesProvider<ServiceProviderWithEndpointFlag> serviceProviderWithEndpointFlag = context.SyntaxProvider.ForAttributeWithMetadataName(
            serviceProviderAttributeName,
            static (SyntaxNode syntaxNode, CancellationToken _) => syntaxNode is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
            CreateServiceProviderWithEndpointFlag
        ).Select((ServiceProviderWithEndpointFlag providerAndFlag, CancellationToken _) => (providerAndFlag.serviceProvider.CreateDependencyTree(), providerAndFlag.endpointProvider)).WithComparer(NoComparison<ServiceProviderWithEndpointFlag>.Instance);

        context.RegisterSourceOutput(serviceProviderWithEndpointFlag, (SourceProductionContext context, ServiceProviderWithEndpointFlag value) => stringBuilderPool.GenerateClass(context, value.serviceProvider));
        context.RegisterSourceOutput(serviceProviderWithEndpointFlag, (SourceProductionContext context, ServiceProviderWithEndpointFlag value) => stringBuilderPool.GenerateInterface(context, value.serviceProvider));

        return serviceProviderWithEndpointFlag;
    }


    /// <summary>
    /// Creates a <see cref="ServiceProvider"/>,
    /// then reads the properties "AddRazorComponents" and "AddDefaultServices",
    /// if "AddDefaultServices" is true or null default services and a constructorDependency is added to the created service provider
    /// and then the service provider together with the property values are returned.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="_">CancellationToken is not used</param>
    /// <returns></returns>
    private static ServiceProviderWithEndpointFlag CreateServiceProviderWithEndpointFlag(GeneratorAttributeSyntaxContext context, CancellationToken _) {
        ServiceProvider serviceProvider = new(context);

        Debug.Assert(context.Attributes.Length > 0);
        bool endpointProvider = context.Attributes[0].NamedArguments.GetArgument<bool?>("EndpointProvider") != false;

        return (serviceProvider, endpointProvider);
    }
}
