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
using ServiceProviderWithExtra = (CircleDI.Generation.ServiceProvider serviceProvider, CircleDI.DefaultServiceGeneration.BlazorServiceGeneration defaultServiceGeneration, bool generateEndpointExtension);
using ServiceProviderWithErrorFlag = (CircleDI.Generation.ServiceProvider serviceProvider, bool generateEndpointExtension, bool hasErrors);
using CircleDI.DefaultServiceGeneration;

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
        ).Collect()
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
        IncrementalValuesProvider<ServiceProviderWithExtra> serviceProviderWithExtraList = context.SyntaxProvider.ForAttributeWithMetadataName(
            serviceProviderAttributeName,
            static (SyntaxNode syntaxNode, CancellationToken _) => syntaxNode is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
            CreateServiceProviderWithExtra
        );

        // init dependency tree
        IncrementalValuesProvider<ServiceProviderWithErrorFlag> serviceProviderTreeInit = serviceProviderWithExtraList.Select(CreateDependencyTree);


        // generate default service get-methods
        context.RegisterSourceOutput(serviceProviderWithExtraList, (SourceProductionContext context, ServiceProviderWithExtra serviceProviderWithExtra) => context.GenerateDefaultServiceMethods(stringBuilderPool, serviceProviderWithExtra.serviceProvider, serviceProviderWithExtra.defaultServiceGeneration));

        // generate endpoint extension method
        context.RegisterSourceOutput(serviceProviderTreeInit.Combine(endpointList), stringBuilderPool.GenerateEndpointMethod);

        // generate serviceProvider class
        context.RegisterSourceOutput(serviceProviderTreeInit, (SourceProductionContext context, ServiceProviderWithErrorFlag value) => {
            if (!value.hasErrors)
                stringBuilderPool.GenerateClass(context, value.serviceProvider);
            else
                OutputServiceProviderErrors(context, value.serviceProvider);
        });

        // generate serviceProvider interface
        context.RegisterSourceOutput(serviceProviderTreeInit, (SourceProductionContext context, ServiceProviderWithErrorFlag value) => {
            if (!value.hasErrors)
                stringBuilderPool.GenerateInterface(context, value.serviceProvider);
            else
                OutputServiceProviderErrors(context, value.serviceProvider);
        });
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
    private static ServiceProviderWithExtra CreateServiceProviderWithExtra(GeneratorAttributeSyntaxContext context, CancellationToken _) {
        ServiceProvider serviceProvider = new(context);

        Debug.Assert(context.Attributes.Length > 0);
        BlazorServiceGeneration defaultServiceGeneration = BlazorServiceGeneration.Server;
        bool generateEndpointExtension = true;
        if (context.Attributes[0].NamedArguments.Length > 0) {
            if (context.Attributes[0].NamedArguments.GetArgument<bool?>("GenerateDefaultServices") == false)
                defaultServiceGeneration = BlazorServiceGeneration.None;
            if (context.Attributes[0].NamedArguments.GetArgument<bool?>("GenerateEndpointExtension") == false)
                generateEndpointExtension = false;
        }

        serviceProvider.AddDefaultServices(defaultServiceGeneration);
        
        return (serviceProvider, defaultServiceGeneration, generateEndpointExtension);
    }

    /// <summary>
    /// Checks errors, if any returns with errorflag true.<br />
    /// Create SortedList and DependecyTree<br />
    /// Checks DependecyTree errors, if any returns with errorflag true.
    /// </summary>
    /// <param name="serviceProviderWithExtra"></param>
    /// <param name="_"></param>
    /// <returns></returns>
    private static ServiceProviderWithErrorFlag CreateDependencyTree(ServiceProviderWithExtra serviceProviderWithExtra, CancellationToken _) {
        ServiceProvider serviceProvider = serviceProviderWithExtra.serviceProvider;

        // check ErrorLists
        {
            if (serviceProvider.ErrorList.Count > 0)
                return (serviceProvider, serviceProviderWithExtra.generateEndpointExtension, true);

            // serviceProvider.SortedServiceList is still empty at this point
            foreach (Service service in serviceProvider.SingletonList.Concat(serviceProvider.ScopedList).Concat(serviceProvider.TransientList).Concat(serviceProvider.DelegateList))
                if (service.ErrorList.Count > 0)
                    return (serviceProvider, serviceProviderWithExtra.generateEndpointExtension, true);
        }

        // create list index
        serviceProvider.CreateSortedList();

        // create dependency tree
        serviceProvider.CreateDependencyTree();
        // check dependency tree errors
        if (serviceProvider.ErrorList.Count > 0)
            return (serviceProvider, serviceProviderWithExtra.generateEndpointExtension, true);

        return (serviceProvider, serviceProviderWithExtra.generateEndpointExtension, false);
    }


    /// <summary>
    /// Init the endpoint methods and generates an endpoint extension method (for each ServiceProvider)
    /// </summary>
    /// <param name="stringBuilderPool"></param>
    /// <param name="context"></param>
    /// <param name="value"></param>
    private static void GenerateEndpointMethod(this ObjectPool<StringBuilder> stringBuilderPool, SourceProductionContext context, (ServiceProviderWithErrorFlag serviceProviderTreeInit, ImmutableArray<Endpoint> endpointList) value) {
        ServiceProvider serviceProvider = value.serviceProviderTreeInit.serviceProvider;
        ImmutableArray<Endpoint> endpointList = value.endpointList;

        bool errorReported = false;
        foreach (Endpoint endpoint in endpointList)
            if (endpoint.ErrorList.Count > 0) {
                foreach (Diagnostic error in endpoint.ErrorList)
                    context.ReportDiagnostic(error);
                errorReported = true;
            }

        if (errorReported || value.serviceProviderTreeInit.hasErrors || !value.serviceProviderTreeInit.generateEndpointExtension)
            return;


        serviceProvider.InitServiceDependencyTree(endpointList.Select((Endpoint endpoint) => endpoint.AsService));


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


    /// <summary>
    /// Iterates <see cref="ServiceProvider.ErrorList"/> and every <see cref="Service.ErrorList"/> and reports these Diagnostics.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="serviceProvider"></param>
    private static void OutputServiceProviderErrors(SourceProductionContext context, ServiceProvider serviceProvider) {
        foreach (Diagnostic error in serviceProvider.ErrorList)
            context.ReportDiagnostic(error);

        // serviceProvider.SortedServiceList can be still empty at this point
        foreach (Service service in serviceProvider.SingletonList.Concat(serviceProvider.ScopedList).Concat(serviceProvider.TransientList).Concat(serviceProvider.DelegateList))
            foreach (Diagnostic error in service.ErrorList)
                context.ReportDiagnostic(error);
    }
}
