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
            IsNamed = false,
            ServiceIdentifier = "System.IServiceProvider",
            HasAttribute = false,
            ByRef = RefKind.None
        };
        serviceProvider.ConstructorParameterList.Add(constructorDependency);
        serviceProvider.ConstructorParameterListScope.Add(constructorDependency);
        if (!serviceProvider.HasConstructorScope)
            serviceProvider.CreateScope?.ConstructorDependencyList.Add(constructorDependency);

        // default services
        {
            AddSingletonService(serviceProvider, "LoggerFactory", "GetLoggerFactory", "Microsoft.Extensions.Logging.ILoggerFactory");
            AddScopedService(serviceProvider, "JSRuntime", "GetJSRuntime", "Microsoft.JSInterop.IJSRuntime");
            AddScopedService(serviceProvider, "NavigationManager", "GetNavigationManager", "Microsoft.AspNetCore.Components.NavigationManager");
            AddScopedService(serviceProvider, "NavigationInterception", "GetNavigationInterception", "Microsoft.AspNetCore.Components.Routing.INavigationInterception");
            AddScopedService(serviceProvider, "ScrollToLocationHash", "GetScrollToLocationHash", "Microsoft.AspNetCore.Components.Routing.IScrollToLocationHash");
            AddScopedService(serviceProvider, "ErrorBoundaryLogger", "GetErrorBoundaryLogger", "Microsoft.AspNetCore.Components.Web.IErrorBoundaryLogger");
            // when support for unbound/open generics
            // AddSingletonService(serviceProvider, "Logger", "GetLogger<T>", "Microsoft.Extensions.Logging.ILogger<>")

            if (!blazorServiceGeneration.HasFlag(BlazorServiceGeneration.Hybrid)) {
                // server or webassembly
                AddSingletonService(serviceProvider, "Configuration", "GetConfiguration", "Microsoft.Extensions.Configuration.IConfiguration");
                AddScopedService(serviceProvider, "ComponentStatePersistenceManager", "GetComponentStatePersistenceManager", "Microsoft.AspNetCore.Components.Infrastructure.ComponentStatePersistenceManager");
                AddScopedService(serviceProvider, "PersistentComponentState", "GetPersistentComponentState", "Microsoft.AspNetCore.Components.PersistentComponentState");
                AddScopedService(serviceProvider, "AntiforgeryStateProvider", "GetAntiforgeryStateProvider", "Microsoft.AspNetCore.Components.Forms.AntiforgeryStateProvider");
            }

            switch (blazorServiceGeneration) {
                case BlazorServiceGeneration.Webassembly: // webassembly only
                    AddSingletonService(serviceProvider, "LazyAssemblyLoader", "GetLazyAssemblyLoader", "Microsoft.AspNetCore.Components.WebAssembly.Services.LazyAssemblyLoader");
                    AddSingletonService(serviceProvider, "WebAssemblyHostEnvironment", "GetWebAssemblyHostEnvironment", "Microsoft.AspNetCore.Components.WebAssembly.Hosting.IWebAssemblyHostEnvironment");
                    break;
                case BlazorServiceGeneration.Server: // server only
                    AddSingletonService(serviceProvider, "WebHostEnvironment", "GetWebHostEnvironment", "Microsoft.AspNetCore.Hosting.IWebHostEnvironment");
                    break;
            }
        }

        return (serviceProvider, blazorServiceGeneration, addRazorComponents);


        static void AddSingletonService(ServiceProvider serviceProvider, string name, string methodName, string type) {
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

        static void AddScopedService(ServiceProvider serviceProvider, string name, string methodName, string type) {
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

        if (serviceProvider.Identifier.NameSpaceList.Count > 0) {
            builder.AppendNamespace(serviceProvider.Identifier.NameSpaceList);
            builder.Append('\n');
            builder.Append('\n');
        }

        // containing types
        for (int i = serviceProvider.Identifier.ContainingTypeList.Count - 1; i >= 0; i--) {
            builder.Append(indent.Sp0);
            builder.Append("partial ");
            builder.Append(serviceProvider.InterfaceIdentifier.ContainingTypeList[i].Keyword.AsString());
            builder.Append(' ');
            builder.AppendContainingType(serviceProvider.Identifier.ContainingTypeList[i]);
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


        string hintName = serviceProvider.Identifier.CreateHintName(".DefaultServiceMethods.g.cs");
        string source = builder.ToString();
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

            string serviceType = component.ToFullQualifiedName();
            if (serviceProvider.SingletonList.Concat(serviceProvider.ScopedList).Concat(serviceProvider.TransientList).Any((Service service) => service.ServiceType == serviceType))
                continue;


            List<ConstructorDependency> constructorDependencyList;
            (IMethodSymbol? constructor, Diagnostic? constructorListError) = Service.FindConstructor(component, serviceProvider.Attribute);
            if (constructor != null)
                constructorDependencyList = constructor!.CreateConstructorDependencyList();
            else {
                constructorDependencyList = [];
                serviceProvider.ErrorList.Add(constructorListError!);
            }

            (List<PropertyDependency> propertyDependencyList, Diagnostic? propertyListError) = Service.CreatePropertyDependencyList(component, serviceProvider.Attribute);
            if (propertyListError != null)
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
