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
using ServiceProviderWithExtra = (CircleDI.Generation.ServiceProvider serviceProvider, CircleDI.MinimalAPI.Defenitions.BlazorServiceGeneration defaultServiceGeneration, bool generateEndpointExtension);
using ServiceProviderWithErrorFlag = (CircleDI.Generation.ServiceProvider serviceProvider, bool generateEndpointExtension, bool hasErrors);

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
            context.AddSource("BlazorServiceGeneration.g.cs", CircleDI.MinimalAPI.Defenitions.Attributes.BlazorServiceGenerationEnum);
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
        context.RegisterSourceOutput(serviceProviderWithExtraList, stringBuilderPool.GenerateDefaultServiceMethods);

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
        BlazorServiceGeneration blazorServiceGeneration = BlazorServiceGeneration.ServerAndWebassembly;
        bool generateEndpointExtension = true;
        if (context.Attributes[0].NamedArguments.Length > 0) {
            if (context.Attributes[0].NamedArguments.GetArgument<int?>("DefaultServiceGeneration") is int serviceGeneration)
                blazorServiceGeneration = (BlazorServiceGeneration)serviceGeneration;
            if (context.Attributes[0].NamedArguments.GetArgument<bool?>("GenerateEndpointExtension") == false)
                generateEndpointExtension = false;
        }

        if (blazorServiceGeneration == BlazorServiceGeneration.None)
            return (serviceProvider, blazorServiceGeneration, generateEndpointExtension);

        // constructor parameter IServiceProvider
        ConstructorDependency constructorDependency = new() {
            Name = "builtinServiceProvider",
            ServiceName = string.Empty,
            ServiceType = new TypeName("IServiceProvider", TypeKeyword.Interface, ["System"], [], [], []),
            HasAttribute = false,
            ByRef = RefKind.None
        };
        serviceProvider.ConstructorParameterList.Add(constructorDependency);
        serviceProvider.ConstructorParameterListScope.Add(constructorDependency);
        if (!serviceProvider.HasConstructorScope)
            serviceProvider.CreateScope?.ConstructorDependencyList.Add(constructorDependency);

        // default services
        {
            AddSingletonService(serviceProvider, "LoggerFactory", "GetLoggerFactory", new TypeName("ILoggerFactory", TypeKeyword.Interface, ["Logging", "Extensions", "Microsoft"], [], [], []));
            AddScopedService(serviceProvider, "JSRuntime", "GetJSRuntime", new TypeName("IJSRuntime", TypeKeyword.Interface, ["JSInterop", "Microsoft"], [], [], []));
            AddScopedService(serviceProvider, "NavigationManager", "GetNavigationManager", new TypeName("NavigationManager", TypeKeyword.Class, ["Components", "AspNetCore", "Microsoft"], [], [], []));
            AddScopedService(serviceProvider, "NavigationInterception", "GetNavigationInterception", new TypeName("INavigationInterception", TypeKeyword.Interface, ["Routing", "Components", "AspNetCore", "Microsoft"], [], [], []));
            AddScopedService(serviceProvider, "ScrollToLocationHash", "GetScrollToLocationHash", new TypeName("IScrollToLocationHash", TypeKeyword.Interface, ["Routing", "Components", "AspNetCore", "Microsoft"], [], [], []));
            AddScopedService(serviceProvider, "ErrorBoundaryLogger", "GetErrorBoundaryLogger", new TypeName("IErrorBoundaryLogger", TypeKeyword.Interface, ["Web", "Components", "AspNetCore", "Microsoft"], [], [], []));
            // when support for unbound/open generics
            // AddSingletonService(serviceProvider, "Logger", "GetLogger<T>", "Microsoft.Extensions.Logging.ILogger<>")

            if (!blazorServiceGeneration.HasFlag(BlazorServiceGeneration.Hybrid)) {
                // server or webassembly
                AddSingletonService(serviceProvider, "Configuration", "GetConfiguration", new TypeName("IConfiguration", TypeKeyword.Interface, ["Configuration", "Extensions", "Microsoft"], [], [], []));
                AddScopedService(serviceProvider, "ComponentStatePersistenceManager", "GetComponentStatePersistenceManager", new TypeName("ComponentStatePersistenceManager", TypeKeyword.Class, ["Infrastructure", "Components", "AspNetCore", "Microsoft"], [], [], []));
                AddScopedService(serviceProvider, "PersistentComponentState", "GetPersistentComponentState", new TypeName("PersistentComponentState", TypeKeyword.Class, ["Components", "AspNetCore", "Microsoft"], [], [], []));
                AddScopedService(serviceProvider, "AntiforgeryStateProvider", "GetAntiforgeryStateProvider", new TypeName("AntiforgeryStateProvider", TypeKeyword.Class, ["Forms", "Components", "AspNetCore", "Microsoft"], [], [], []));
            }

            switch (blazorServiceGeneration) {
                case BlazorServiceGeneration.Webassembly: // webassembly only
                    AddSingletonService(serviceProvider, "LazyAssemblyLoader", "GetLazyAssemblyLoader", new TypeName("LazyAssemblyLoader", TypeKeyword.Class, ["Services", "WebAssembly", "Components", "AspNetCore", "Microsoft"], [], [], []));
                    AddSingletonService(serviceProvider, "WebAssemblyHostEnvironment", "GetWebAssemblyHostEnvironment", new TypeName("IWebAssemblyHostEnvironment", TypeKeyword.Interface, ["Hosting", "WebAssembly", "Components", "AspNetCore", "Microsoft"], [], [], []));
                    break;
                case BlazorServiceGeneration.Server: // server only
                    AddSingletonService(serviceProvider, "WebHostEnvironment", "GetWebHostEnvironment", new TypeName("IWebHostEnvironment", TypeKeyword.Interface, ["Hosting", "AspNetCore", "Microsoft"], [], [], []));
                    break;
            }
        }

        return (serviceProvider, blazorServiceGeneration, generateEndpointExtension);


        static void AddSingletonService(ServiceProvider serviceProvider, string name, string methodName, TypeName type) {
            if (!serviceProvider.SingletonList.Any((Service service) => service.ServiceType == type))
                serviceProvider.SingletonList.Add(new Service() {
                    Name = name,
                    Lifetime = ServiceLifetime.Singleton,
                    ServiceType = type,
                    IsRefable = false,
                    ImplementationType = type,
                    CreationTime = CreationTiming.Constructor,
                    CreationTimeTransitive = CreationTiming.Constructor,
                    GetAccessor = GetAccess.Property,
                    Implementation = new ImplementationMember(MemberType.Method, methodName, IsStatic: false, IsScoped: false),
                    ConstructorDependencyList = [],
                    PropertyDependencyList = [],
                    IsDisposable = false,
                    IsAsyncDisposable = false,
                    Dependencies = [],
                });

        }

        static void AddScopedService(ServiceProvider serviceProvider, string name, string methodName, TypeName type) {
            if (!serviceProvider.ScopedList.Any((Service service) => service.ServiceType == type))
                serviceProvider.ScopedList.Add(new Service() {
                    Name = name,
                    Lifetime = ServiceLifetime.Scoped,
                    ServiceType = type,
                    IsRefable = false,
                    ImplementationType = type,
                    CreationTime = CreationTiming.Lazy,
                    CreationTimeTransitive = CreationTiming.Lazy,
                    GetAccessor = GetAccess.Property,
                    Implementation = new ImplementationMember(MemberType.Method, methodName, IsStatic: false, IsScoped: true),
                    ConstructorDependencyList = [],
                    PropertyDependencyList = [],
                    IsDisposable = false,
                    IsAsyncDisposable = false,
                    Dependencies = [],
                });
        }
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
    /// <para>Generates the methods to get services from the buit-in service provider.</para>
    /// <para>e.g. private global::Microsoft.Extensions.Configuration.IConfiguration GetConfiguration() => _builtinServiceProvider.GetRequiredService&lt;global::Microsoft.Extensions.Configuration.IConfiguration&gt;();</para>
    /// </summary>
    /// <param name="stringBuilderPool"></param>
    /// <param name="context"></param>
    /// <param name="serviceProviderWithExtra"></param>
    private static void GenerateDefaultServiceMethods(this ObjectPool<StringBuilder> stringBuilderPool, SourceProductionContext context, ServiceProviderWithExtra serviceProviderWithExtra) {
        BlazorServiceGeneration blazorServiceGeneration = serviceProviderWithExtra.defaultServiceGeneration;
        if (blazorServiceGeneration == BlazorServiceGeneration.None)
            return;

        ServiceProvider serviceProvider = serviceProviderWithExtra.serviceProvider;

        StringBuilder builder = stringBuilderPool.Get();
        Indent indent = new();

        builder.Append("""
            // <auto-generated/>
            #pragma warning disable
            #nullable enable annotations


            
            """);

        builder.AppendNamespace(serviceProvider.Identifier.NameSpaceList);

        // containing types
        for (int i = serviceProvider.Identifier.ContainingTypeList.Count - 1; i >= 0; i--) {
            builder.Append(indent.Sp0);
            builder.Append("partial ");
            builder.Append(serviceProvider.InterfaceIdentifier.ContainingTypeList[i].Keyword.AsString());
            builder.Append(' ');
            builder.AppendOpenContainingType(serviceProvider.Identifier.ContainingTypeList[i]);
            builder.Append(" {\n");
            indent.IncreaseLevel();
        }

        // class head
        builder.Append(indent.Sp0);
        builder.Append("partial ");
        builder.Append(serviceProvider.Keyword.AsString());
        builder.Append(' ');
        builder.Append(serviceProvider.Identifier.Name);
        builder.Append(" {\n");

        // singletons getter
        if (!blazorServiceGeneration.HasFlag(BlazorServiceGeneration.Hybrid))
            AppendGetMethod(builder, indent.Sp4, "GetConfiguration", "Microsoft.Extensions.Configuration.IConfiguration");
        AppendGetMethod(builder, indent.Sp4, "GetLoggerFactory", "Microsoft.Extensions.Logging.ILoggerFactory");
        // when unbound/open generics support
        // serviceProvider.SingletonList.Add(new Service() { ServiceType = "Microsoft.Extensions.Logging.ILogger<>" }
        switch (blazorServiceGeneration) {
            case BlazorServiceGeneration.Webassembly:
                AppendGetMethod(builder, indent.Sp4, "GetLazyAssemblyLoader", "Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader");
                AppendGetMethod(builder, indent.Sp4, "GetWebAssemblyHostEnvironment", "Microsoft.AspNetCore.Components.WebAssembly.Hosting.IWebAssemblyHostEnvironment");
                break;
            case BlazorServiceGeneration.Server:
                AppendGetMethod(builder, indent.Sp4, "GetWebHostEnvironment", "Microsoft.AspNetCore.Hosting.IWebHostEnvironment");
                break;
        }

        // scope head
        builder.Append('\n');
        builder.Append(indent.Sp4);
        builder.Append("partial ");
        builder.Append(serviceProvider.KeywordScope.AsString());
        builder.Append(" Scope { \n");

        // scope getter
        AppendGetMethod(builder, indent.Sp8, "GetJSRuntime", "Microsoft.JSInterop.IJSRuntime");
        AppendGetMethod(builder, indent.Sp8, "GetNavigationManager", "Microsoft.AspNetCore.Components.NavigationManager");
        AppendGetMethod(builder, indent.Sp8, "GetNavigationInterception", "Microsoft.AspNetCore.Components.Routing.INavigationInterception");
        AppendGetMethod(builder, indent.Sp8, "GetScrollToLocationHash", "Microsoft.AspNetCore.Components.Routing.IScrollToLocationHash");
        AppendGetMethod(builder, indent.Sp8, "GetErrorBoundaryLogger", "Microsoft.AspNetCore.Components.Web.IErrorBoundaryLogger");
        if (!blazorServiceGeneration.HasFlag(BlazorServiceGeneration.Hybrid)) {
            AppendGetMethod(builder, indent.Sp8, "GetComponentStatePersistenceManager", "Microsoft.AspNetCore.Components.Infrastructure.ComponentStatePersistenceManager");
            AppendGetMethod(builder, indent.Sp8, "GetPersistentComponentState", "Microsoft.AspNetCore.Components.PersistentComponentState");
            AppendGetMethod(builder, indent.Sp8, "GetAntiforgeryStateProvider", "Microsoft.AspNetCore.Components.Forms.AntiforgeryStateProvider");
        }

        // closing brackets
        builder.Append(indent.Sp4);
        builder.Append('}');
        builder.Append('\n');
        builder.Append(indent.Sp0);
        builder.Append('}');
        builder.Append('\n');

        // containing types closing
        for (int i = 0; i < serviceProvider.Identifier.ContainingTypeList.Count; i++) {
            indent.DecreaseLevel();
            builder.Append(indent.Sp0);
            builder.Append('}');
            builder.Append('\n');
        }


        string source = builder.ToString();
        string hintName = builder.CreateHintName(serviceProvider.Identifier, ".DefaultServiceMethods.g.cs");
        context.AddSource(hintName, source);

        stringBuilderPool.Return(builder);


        static void AppendGetMethod(StringBuilder builder, string indent, string methodName, string type) {
            builder.Append(indent);
            builder.Append("private global::");
            builder.Append(type);
            builder.Append(' ');
            builder.Append(methodName);
            builder.Append("() => (");
            builder.Append(type);
            builder.Append(")_builtinServiceProvider.GetService(typeof(");
            builder.Append(type);
            builder.Append("));\n");
        }
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
