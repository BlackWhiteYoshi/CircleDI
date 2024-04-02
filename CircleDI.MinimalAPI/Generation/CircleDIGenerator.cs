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
using ServiceProviderWithEndpointFlag = (CircleDI.Generation.ServiceProvider serviceProvider, bool generateEndpointExtension);

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


        // find all endpoints
        IncrementalValueProvider<ImmutableArray<Endpoint>> endpointList = context.SyntaxProvider.ForAttributeWithMetadataName(
            "CircleDIAttributes.EndpointAttribute",
            static (SyntaxNode syntaxNode, CancellationToken _) => syntaxNode is MethodDeclarationSyntax,
            static (GeneratorAttributeSyntaxContext generatorAttributeSyntaxContext, CancellationToken _) => new Endpoint(generatorAttributeSyntaxContext)
        ).WithComparer(NoComparison<Endpoint>.Instance)
        .Collect().WithComparer(NoComparison<ImmutableArray<Endpoint>>.Instance)
        .Select(CheckRouteConflicts);


        ObjectPool<StringBuilder> stringBuilderPool = CircleDIBuilder.CreateStringBuilderPool();
        context.RegisterServiceProviderAttribute("CircleDIAttributes.ServiceProviderAttribute", endpointList, stringBuilderPool);
        context.RegisterServiceProviderAttribute("CircleDIAttributes.ServiceProviderAttribute`1", endpointList, stringBuilderPool);
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
    public static void RegisterServiceProviderAttribute(this IncrementalGeneratorInitializationContext context, string serviceProviderAttributeName, IncrementalValueProvider<ImmutableArray<Endpoint>> endpointList, ObjectPool<StringBuilder> stringBuilderPool) {
        // all service providers
        IncrementalValuesProvider<ServiceProviderWithEndpointFlag> serviceProviderWithEndpointFlag = context.SyntaxProvider.ForAttributeWithMetadataName(
            serviceProviderAttributeName,
            static (SyntaxNode syntaxNode, CancellationToken _) => syntaxNode is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
            CreateServiceProviderWithEndpointFlag
        );

        // init dependency tree
        IncrementalValuesProvider<ServiceProviderWithEndpointFlag> serviceProviderTreeInit = serviceProviderWithEndpointFlag.WithComparer(NoComparison<ServiceProviderWithEndpointFlag>.Instance)
            .Select((ServiceProviderWithEndpointFlag providerAndFlag, CancellationToken _) => (providerAndFlag.serviceProvider.InitDependencyTree(), providerAndFlag.generateEndpointExtension));


        // generate endpoint extension method
        context.RegisterSourceOutput(serviceProviderTreeInit.WithComparer(NoComparison<ServiceProviderWithEndpointFlag>.Instance).Combine(endpointList), stringBuilderPool.GenerateEndpointMethod);

        context.RegisterSourceOutput(serviceProviderTreeInit, (SourceProductionContext context, ServiceProviderWithEndpointFlag value) => stringBuilderPool.GenerateClass(context, value.serviceProvider));
        context.RegisterSourceOutput(serviceProviderTreeInit, (SourceProductionContext context, ServiceProviderWithEndpointFlag value) => stringBuilderPool.GenerateInterface(context, value.serviceProvider));
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
        bool generateEndpointExtension = context.Attributes[0].NamedArguments.GetArgument<bool?>("GenerateEndpointExtension") != false;

        return (serviceProvider, generateEndpointExtension);
    }


    /// <summary>
    /// Init the endpoint methods and generates an endpoint extension method (for each ServiceProvider)
    /// </summary>
    /// <param name="stringBuilderPool"></param>
    /// <param name="context"></param>
    /// <param name="value"></param>
    private static void GenerateEndpointMethod(this ObjectPool<StringBuilder> stringBuilderPool, SourceProductionContext context, (ServiceProviderWithEndpointFlag serviceProviderTreeInit, ImmutableArray<Endpoint> endpointList) value) {
        ServiceProvider serviceProvider = value.serviceProviderTreeInit.serviceProvider;
        ImmutableArray<Endpoint> endpointList = value.endpointList;
        bool generateEndpointExtension = value.serviceProviderTreeInit.generateEndpointExtension;

        bool errorReported = false;
        foreach (Endpoint endpoint in endpointList)
            if (endpoint.ErrorList.Count > 0) {
                foreach (Diagnostic error in endpoint.ErrorList)
                    context.ReportDiagnostic(error);
                errorReported = true;
            }

        if (errorReported || serviceProvider.HasError || !generateEndpointExtension)
            return;


        serviceProvider.InitServiceDependencyTree(endpointList.Select((Endpoint endpoint) => endpoint.AsService));

        if (serviceProvider.ErrorList.Count > 0) {
            foreach (Diagnostic error in serviceProvider.ErrorList)
                context.ReportDiagnostic(error);
            return;
        }


        StringBuilder builder = stringBuilderPool.Get();
        const string SP4 = "    ";
        const string SP8 = "        ";

        TypeName serviceTypeScope = serviceProvider.HasInterface ? serviceProvider.InterfaceIdentifierScope : serviceProvider.IdentifierScope;
        string providerName = serviceProvider.Identifier.Name;


        builder.Append("""
            // <auto-generated/>
            #pragma warning disable
            #nullable enable annotations


            
            """);

        builder.AppendNamespace(serviceProvider.Identifier.NameSpaceList);


        builder.Append("public static partial class EndpointExtension {\n");

        builder.Append($"{SP4}public static void MapEndpointsWith");
        builder.Append(serviceProvider.Identifier.Name);

        // type parameter
        {
            int initalPosition = builder.Length;
            builder.Append('<');

            foreach (string typeParameter in serviceTypeScope.TypeParameterList) {
                builder.Append(typeParameter);
                builder.Append(", ");
            }
            foreach (TypeName typeName in serviceTypeScope.ContainingTypeList)
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
                    builder.AppendOpenFullyQualified(serviceTypeScope);
                    builder.Append(' ');
                    builder.AppendFirstLower(providerName);
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
                        builder.AppendFirstLower(providerName);
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


        string source = builder.ToString();
        string hintName = builder.CreateHintName(serviceProvider.Identifier, ".EndpointExtension.g.cs");
        context.AddSource(hintName, source);

        stringBuilderPool.Return(builder);
    }
}
