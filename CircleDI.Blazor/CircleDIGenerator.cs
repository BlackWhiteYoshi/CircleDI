﻿using CircleDI.Defenitions;
using CircleDI.Extensions;
using CircleDI.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.ObjectPool;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using ServiceProviderWithExtra = (CircleDI.Generation.ServiceProvider serviceProvider, CircleDI.Blazor.BlazorServiceGeneration defaultServiceGeneration, bool AddRazorComponents);

namespace CircleDI.Blazor;

[Generator(LanguageNames.CSharp)]
public sealed class CircleDIGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(static (IncrementalGeneratorPostInitializationContext context) => {
            // attributes
            context.AddSource("ServiceProviderAttribute.g.cs", Blazor.Attributes.ServiceProviderAttribute);
            context.AddSource("ScopedProviderAttribute.g.cs", Defenitions.Attributes.ScopedProviderAttribute);
            context.AddSource("SingletonAttribute.g.cs", Defenitions.Attributes.SingletonAttribute);
            context.AddSource("ScopedAttribute.g.cs", Defenitions.Attributes.ScopedAttribute);
            context.AddSource("TransientAttribute.g.cs", Defenitions.Attributes.TransientAttribute);
            context.AddSource("DelegateAttribute.g.cs", Defenitions.Attributes.DelegateAttribute);
            context.AddSource("DependencyAttribute.g.cs", Defenitions.Attributes.DependencyAttribute);
            context.AddSource("ConstructorAttribute.g.cs", Defenitions.Attributes.ConstructorAttribute);

            // enums
            context.AddSource("CreationTiming.g.cs", Defenitions.Attributes.CreationTimingEnum);
            context.AddSource("GetAccess.g.cs", Defenitions.Attributes.GetAccessEnum);
            context.AddSource("DisposeGeneration.g.cs", Defenitions.Attributes.DisposeGenerationEnum);
            context.AddSource("BlazorServiceGeneration.g.cs", Blazor.Attributes.BlazorServiceGenerationEnum);

            // class
            context.AddSource("CircleDIComponentActivator.g.cs", Blazor.Attributes.CircleDIComponentActivator);
        });

        ObjectPool<StringBuilder> stringBuilderPool = CircleDIBuilder.CreateStringBuilderPool();
        context.RegisterServiceProviderAttribute("CircleDIAttributes.ServiceProviderAttribute", stringBuilderPool);
        context.RegisterServiceProviderAttribute("CircleDIAttributes.ServiceProviderAttribute`1", stringBuilderPool);
    }
}

file static class CircleDIGeneratorExtensions {
    public static void RegisterServiceProviderAttribute(this IncrementalGeneratorInitializationContext context, string serviceProviderAttributeName, ObjectPool<StringBuilder> stringBuilderPool) {
        // all service providers
        IncrementalValuesProvider<ServiceProviderWithExtra> serviceProviderWithExtraList = context.SyntaxProvider.ForAttributeWithMetadataName(
            serviceProviderAttributeName,
            static (SyntaxNode syntaxNode, CancellationToken _) => syntaxNode is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
            CreateServiceProviderWithExtra
        );


        // generate default service get-methods
        context.RegisterSourceOutput(serviceProviderWithExtraList, stringBuilderPool.GenerateDefaultServiceMethods);


        // find all components
        IncrementalValueProvider<ImmutableArray<INamedTypeSymbol?>> componentList = context.SyntaxProvider.CreateSyntaxProvider(
            static (SyntaxNode syntaxNode, CancellationToken _) => syntaxNode is ClassDeclarationSyntax { BaseList: not null },
            FilterComponents
        ).Collect();

        // add components
        IncrementalValuesProvider<ServiceProvider> serviceProviderList = serviceProviderWithExtraList.Combine(componentList).Select(ServiceProviderWithComponents);


        context.RegisterSourceOutput(serviceProviderList, stringBuilderPool.GenerateClass);
        context.RegisterSourceOutput(serviceProviderList, stringBuilderPool.GenerateInterface);
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
        bool addRazorComponents = true;
        if (context.Attributes[0].NamedArguments.Length > 0) {
            if (context.Attributes[0].NamedArguments.GetArgument<int?>("DefaultServiceGeneration") is int serviceGeneration)
                blazorServiceGeneration = (BlazorServiceGeneration)serviceGeneration;
            if (context.Attributes[0].NamedArguments.GetArgument<bool?>("AddRazorComponents") == false)
                addRazorComponents = false;
        }

        if (blazorServiceGeneration == BlazorServiceGeneration.None)
            return (serviceProvider, blazorServiceGeneration, addRazorComponents);

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

        return (serviceProvider, blazorServiceGeneration, addRazorComponents);


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
            serviceProvider.Identifier.ContainingTypeList[i].AppendOpenContainingType(builder);
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
        string hintName = serviceProvider.Identifier.CreateHintName(builder, ".DefaultServiceMethods.g.cs");
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
    /// If the targetNode is derived from Microsoft.AspNetCore.Components.ComponentBase, it is returned as INamedTypeSymbol, otherwise null.
    /// </summary>
    /// <param name="context"></param>
    /// <param name="_">CancellationToken is not used</param>
    /// <returns></returns>
    private static INamedTypeSymbol? FilterComponents(GeneratorSyntaxContext context, CancellationToken _) {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol component || component.IsAbstract)
            return null;

        // Microsoft.AspNetCore.Components.ComponentBase
        for (INamedTypeSymbol? baseType = component.BaseType; baseType is not null; baseType = baseType.BaseType)
            if (baseType.Name == "ComponentBase"
            && baseType.ContainingNamespace is INamespaceSymbol { Name: "Components" } namespace3
            && namespace3.ContainingNamespace is INamespaceSymbol { Name: "AspNetCore" } namespace2
            && namespace2.ContainingNamespace is INamespaceSymbol { Name: "Microsoft" })
                return component;

        return null;
    }

    /// <summary>
    /// Adds the components from the given componentList to the given serviceProvider and then returns the provider.<br />
    /// The component is not added when already registered.
    /// </summary>
    /// <param name="pair">serviiceProvider and componentList</param>
    /// <param name="_">CancellationToken is not used</param>
    /// <returns></returns>
    private static ServiceProvider ServiceProviderWithComponents((ServiceProviderWithExtra serviceProviderWithExtra, ImmutableArray<INamedTypeSymbol?> componentList) pair, CancellationToken _) {
        ServiceProvider serviceProvider = pair.serviceProviderWithExtra.serviceProvider;
        if (!pair.serviceProviderWithExtra.AddRazorComponents)
            return serviceProvider;

        foreach (INamedTypeSymbol? component in pair.componentList) {
            if (component is null)
                continue;

            TypeName serviceType = new(component);
            if (serviceProvider.SingletonList.Concat(serviceProvider.ScopedList).Concat(serviceProvider.TransientList).Any((Service service) => service.ServiceType == serviceType))
                continue;


            (List<ConstructorDependency> constructorDependencyList, Diagnostic? constructorListError) = Service.CreateConstructorDependencyList(component, serviceProvider.Attribute);
            if (constructorListError is not null)
                serviceProvider.ErrorList.Add(constructorListError);

            (List<PropertyDependency> propertyDependencyList, Diagnostic? propertyListError) = Service.CreatePropertyDependencyList(component, serviceProvider.Attribute);
            if (propertyListError is not null)
                serviceProvider.ErrorList.Add(propertyListError);

            serviceProvider.TransientList.Add(new Service() {
                Lifetime = ServiceLifetime.Transient,
                ServiceType = serviceType,
                IsRefable = false,
                ImplementationType = serviceType,
                Name = component.Name,
                CreationTime = CreationTiming.Lazy,
                GetAccessor = GetAccess.Property,
                Implementation = default,
                ConstructorDependencyList = constructorDependencyList,
                PropertyDependencyList = propertyDependencyList,
                IsDisposable = false,
                IsAsyncDisposable = false,
                Dependencies = constructorDependencyList.Concat<Dependency>(propertyDependencyList)
            });
        }

        return serviceProvider;
    }
}
