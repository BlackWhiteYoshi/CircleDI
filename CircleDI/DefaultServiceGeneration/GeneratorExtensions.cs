using CircleDI.Defenitions;
using CircleDI.Extensions;
using CircleDI.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.Extensions.ObjectPool;
using System.Text;

namespace CircleDI.DefaultServiceGeneration;

/// <summary>
/// Extension methods for generating and bulding default services.<br />
/// Default services are services provided by the built in provider <see cref="Microsoft.Extensions.DependencyInjection"/>.
/// Depending on the environment, that provider contains services by default.
/// </summary>
public static class GeneratorExtensions {
    /// <summary>
    /// <para>if <see cref="BlazorServiceGeneration"/> is <see cref="BlazorServiceGeneration.None"/>, this method does nothing.</para>
    /// <para>Adds to the given ServiceProvider a constructorDependency and default services.</para>
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="blazorServiceGeneration"></param>
    public static void AddDefaultServices(this ServiceProvider serviceProvider, BlazorServiceGeneration blazorServiceGeneration) {
        if (blazorServiceGeneration == BlazorServiceGeneration.None)
            return;

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
    /// <param name="context"></param>
    /// <param name="stringBuilderPool"></param>
    /// <param name="serviceProvider"></param>
    /// <param name="blazorServiceGeneration"></param>
    public static void GenerateDefaultServiceMethods(this SourceProductionContext context, ObjectPool<StringBuilder> stringBuilderPool, ServiceProvider serviceProvider, BlazorServiceGeneration blazorServiceGeneration) {
        if (blazorServiceGeneration == BlazorServiceGeneration.None)
            return;

        StringBuilder builder = stringBuilderPool.Get();
        Indent indent = new();

        builder.Append("""
            // <auto-generated/>
            #pragma warning disable
            #nullable enable annotations


            
            """
        );

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
}
