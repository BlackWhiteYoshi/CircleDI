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
            context.AddSource("DependencyAttribute.g.cs", CircleDI.Defenitions.Attributes.DependencyAttribute);
            context.AddSource("ConstructorAttribute.g.cs", CircleDI.Defenitions.Attributes.ConstructorAttribute);
            context.AddSource("EndpointrAttribute.g.cs", CircleDI.MinimalAPI.Defenitions.Attributes.EndpointAttribute);

            // enums
            context.AddSource("CreationTiming.g.cs", CircleDI.Defenitions.Attributes.CreationTimingEnum);
            context.AddSource("GetAccess.g.cs", CircleDI.Defenitions.Attributes.GetAccessEnum);
            context.AddSource("DisposeGeneration.g.cs", CircleDI.Defenitions.Attributes.DisposeGenerationEnum);
            context.AddSource("Http.g.cs", CircleDI.MinimalAPI.Defenitions.Attributes.HttpEnum);
        });


        ObjectPool<StringBuilder> stringBuilderPool = CircleDIBuilder.CreateStringBuilderPool();
        IncrementalValuesProvider<ServiceProviderWithEndpointFlag> serviceProviderList = context.RegisterServiceProviderAttribute("CircleDIAttributes.ServiceProviderAttribute", stringBuilderPool);
        IncrementalValuesProvider<ServiceProviderWithEndpointFlag> genericServiceProviderList = context.RegisterServiceProviderAttribute("CircleDIAttributes.ServiceProviderAttribute`1", stringBuilderPool);


        // find all endpoints
        IncrementalValueProvider<ImmutableArray<Endpoint>> endpointList = context.SyntaxProvider.ForAttributeWithMetadataName(
            "CircleDIAttributes.EndpointAttribute",
            static (SyntaxNode syntaxNode, CancellationToken _) => syntaxNode is MethodDeclarationSyntax,
            static (GeneratorAttributeSyntaxContext generatorAttributeSyntaxContext, CancellationToken _) => new Endpoint(generatorAttributeSyntaxContext)
        ).Collect().Select(CheckRouteConflicts);

        // all service providers with all endpoints
        IncrementalValueProvider<((ImmutableArray<ServiceProviderWithEndpointFlag> serviceProvider, ImmutableArray<ServiceProviderWithEndpointFlag> genericServiceProvider), ImmutableArray<Endpoint> endpointList)> serviceProviderListWithEndpointList =
            serviceProviderList.Collect()
            .Combine(genericServiceProviderList.Collect())
            .Combine(endpointList);

        // generate endpoint extension method
        context.RegisterSourceOutput(serviceProviderListWithEndpointList, stringBuilderPool.GenerateEndpointMethod);
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
                    endpoint.ErrorList.Add(endpoint.Attribute.CreateMultipleSameEndpointError(endpointGroup.Key, (Http)i));
                    break;
                }
        }

        return endpoints;
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


    /// <summary>
    /// <para>Init the endpoint methods and generates an endpoint extension method.</para>
    /// <para>
    /// If multiple ServiceProviders are "EndpointProvider", an error is reported instead.<br />
    /// If no ServiceProvider has "EndpointProvider" and one parameter has [Dependency], an error is reported instead.
    /// </para>
    /// </summary>
    /// <param name="stringBuilderPool"></param>
    /// <param name="context"></param>
    /// <param name="value"></param>
    public static void GenerateEndpointMethod(this ObjectPool<StringBuilder> stringBuilderPool, SourceProductionContext context, ((ImmutableArray<ServiceProviderWithEndpointFlag> serviceProvider, ImmutableArray<ServiceProviderWithEndpointFlag> genericServiceProvider) serviceProviders, ImmutableArray<Endpoint> endpointList) value) {
        ImmutableArray<Endpoint> endpointList = value.endpointList;
        bool errorReported = false;
        foreach (Endpoint endpoint in endpointList)
            if (endpoint.ErrorList.Count > 0) {
                foreach (Diagnostic error in endpoint.ErrorList)
                    context.ReportDiagnostic(error);
                errorReported = true;
            }


        ServiceProvider? serviceProvider = null;
        foreach ((ServiceProvider listedProvider, bool endpointFlag) in value.serviceProviders.serviceProvider.Concat(value.serviceProviders.genericServiceProvider))
            if (endpointFlag)
                if (serviceProvider is null)
                    serviceProvider = listedProvider;
                else {
                    // many ServiceProviders
                    context.ReportDiagnostic(serviceProvider.Attribute.CreateMultipleEndpointServiceProviderError(listedProvider.Attribute));
                    return;
                }

        TypeName? serviceTypeScopeProvider = null;
        if (serviceProvider is not null) {
            // 1 ServiceProvider -> normal case
            if (serviceProvider.HasError)
                return;

            serviceTypeScopeProvider = serviceProvider.HasInterface ? serviceProvider.InterfaceIdentifierScope : serviceProvider.IdentifierScope;

            serviceProvider.CreateDependencyTree(endpointList.Select((Endpoint endpoint) => (endpoint.AsService, endpoint.Attribute)));

            if (serviceProvider.ErrorList.Count > 0) {
                foreach (Diagnostic error in serviceProvider.ErrorList)
                    context.ReportDiagnostic(error);
                return;
            }
        }
        else {
            // 0 ServiceProviders -> allowed when no [Dependency]
            foreach (Endpoint endpoint in endpointList)
                foreach (ConstructorDependency dependency in endpoint.AsService.ConstructorDependencyList)
                    if (dependency.HasAttribute) {
                        context.ReportDiagnostic(endpoint.Attribute.CreateEndpointDependencyWithoutServiceProviderError());
                        return;
                    }
        }


        if (errorReported)
            return;



        StringBuilder builder = stringBuilderPool.Get();
        const string SP4 = "    ";
        const string SP8 = "        ";


        builder.Append("""
            // <auto-generated/>
            #pragma warning disable
            #nullable enable annotations


            namespace CircleDIAttributes;

            public static partial class EndpointExtension {

            """);

        builder.Append($"{SP4}public static void MapCircleDIEndpoints");
        // type parameter
        if (serviceTypeScopeProvider != null)
        {
            int initalPosition = builder.Length;
            builder.Append('<');

            foreach (string typeParameter in serviceTypeScopeProvider.TypeParameterList) {
                builder.Append(typeParameter);
                builder.Append(", ");
            }
            foreach (TypeName typeName in serviceTypeScopeProvider.ContainingTypeList)
                foreach (string typeParameter in typeName.TypeParameterList) {
                    builder.Append(typeParameter);
                    builder.Append(", ");
                }

            builder.Length--;
            if (builder.Length > initalPosition)
                builder[^1] = '>';
        }
        builder.Append("(this global::Microsoft.AspNetCore.Builder.WebApplication app) {\n");

        foreach (Endpoint endpoint in endpointList) {
            builder.Append(SP8);

            string closeBracket = string.Empty;
            if (endpoint.MethodRouteBuilder is not null) {
                builder.Append("global::");
                endpoint.MethodRouteBuilder.AppendFullyQualifiedName(builder);
                builder.Append('(');
                closeBracket = ")";
            }

            builder.Append("app.Map");
            builder.Append(endpoint.HttpMethod.AsString());
            builder.Append("(\"");
            builder.Append(endpoint.Route);
            builder.Append("\", (");

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
                            builder.Append("[global::");
                            builder.AppendClosedFullyQualified(attribute.Name);

                            if (attribute.ParameterList.Length + attribute.PropertyList.Length > 0) {
                                builder.Append('(');
                                foreach (string attributeParameter in attribute.ParameterList) {
                                    builder.Append(attributeParameter);
                                    builder.Append(", ");
                                }
                                foreach ((string attributeName, string attributeValue) in attribute.PropertyList) {
                                    builder.Append(attributeName);
                                    builder.Append(" = ");
                                    builder.Append(attributeValue);
                                    builder.Append(", ");
                                }
                                builder.Length--;
                                builder[^1] = ')';
                            }

                            builder.Append(']');
                        }
                        builder.Append(' ');
                    }
                    builder.Append("global::");
                    builder.AppendClosedFullyQualified(parameter.HasAttribute ? parameter.Service!.ServiceType : parameter.ServiceType!);
                    builder.Append(' ');
                    builder.Append(parameter.Name);
                    builder.Append(", ");
                }

                if (hasDependencyParameter) {
                    builder.Append("global::");
                    builder.AppendOpenFullyQualified(serviceTypeScopeProvider!);
                    builder.Append(' ');
                    builder.AppendFirstLower(serviceProvider!.Identifier.Name);
                }
                else
                    builder.Length -= 2;
            }

            builder.Append(") => global::");
            endpoint.MethodHandler.AppendFullyQualifiedName(builder);
            builder.Append('(');

            if (endpoint.AsService.ConstructorDependencyList.Count > 0) {
                foreach (ConstructorDependency parameter in endpoint.AsService.ConstructorDependencyList) {
                    if (parameter.HasAttribute) {
                        builder.Append(parameter.ByRef.AsString());
                        builder.AppendFirstLower(serviceProvider!.Identifier.Name);
                        builder.Append('.');
                        builder.AppendServiceGetter(parameter.Service!);
                    }
                    else
                        builder.Append(parameter.Name);
                    builder.Append(", ");
                }

                builder.Length -= 2;
            }

            builder.Append(closeBracket);
            builder.Append("));\n");
        }

        builder.Append($"{SP4}}}\n");
        builder.Append("}\n");


        context.AddSource("EndpointExtension.g.cs", builder.ToString());

        stringBuilderPool.Return(builder);
    }
}
