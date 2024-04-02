using CircleDI.DefaultServiceGeneration;
using CircleDI.Defenitions;
using CircleDI.Extensions;
using CircleDI.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.ObjectPool;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Text;
using ServiceProviderWithExtra = (CircleDI.Generation.ServiceProvider serviceProvider, CircleDI.DefaultServiceGeneration.BlazorServiceGeneration defaultServiceGeneration, bool AddRazorComponents);

namespace CircleDI.Blazor.Generation;

[Generator(LanguageNames.CSharp)]
public sealed class CircleDIGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(static (IncrementalGeneratorPostInitializationContext context) => {
            // attributes
            context.AddSource("ServiceProviderAttribute.g.cs", CircleDI.Blazor.Defenitions.Attributes.ServiceProviderAttribute);
            context.AddSource("ScopedProviderAttribute.g.cs", CircleDI.Defenitions.Attributes.ScopedProviderAttribute);
            context.AddSource("SingletonAttribute.g.cs", CircleDI.Defenitions.Attributes.SingletonAttribute);
            context.AddSource("ScopedAttribute.g.cs", CircleDI.Defenitions.Attributes.ScopedAttribute);
            context.AddSource("TransientAttribute.g.cs", CircleDI.Defenitions.Attributes.TransientAttribute);
            context.AddSource("DelegateAttribute.g.cs", CircleDI.Defenitions.Attributes.DelegateAttribute);
            context.AddSource("DependencyAttribute.g.cs", CircleDI.Defenitions.Attributes.DependencyAttribute);
            context.AddSource("ConstructorAttribute.g.cs", CircleDI.Defenitions.Attributes.ConstructorAttribute);

            // enums
            context.AddSource("CreationTiming.g.cs", CircleDI.Defenitions.Attributes.CreationTimingEnum);
            context.AddSource("GetAccess.g.cs", CircleDI.Defenitions.Attributes.GetAccessEnum);
            context.AddSource("DisposeGeneration.g.cs", CircleDI.Defenitions.Attributes.DisposeGenerationEnum);
            context.AddSource("BlazorServiceGeneration.g.cs", CircleDI.Blazor.Defenitions.Attributes.BlazorServiceGenerationEnum);

            // class
            context.AddSource("CircleDIComponentActivator.g.cs", CircleDI.Blazor.Defenitions.Attributes.CircleDIComponentActivator);
        });


        // find all components
        IncrementalValueProvider<ImmutableArray<INamedTypeSymbol?>> componentList = context.SyntaxProvider.CreateSyntaxProvider(
            static (SyntaxNode syntaxNode, CancellationToken _) => syntaxNode is ClassDeclarationSyntax { BaseList: not null },
            FilterComponents
        ).Collect();


        ObjectPool<StringBuilder> stringBuilderPool = CircleDIBuilder.CreateStringBuilderPool();
        context.RegisterServiceProviderAttribute("CircleDIAttributes.ServiceProviderAttribute", componentList, stringBuilderPool);
        context.RegisterServiceProviderAttribute("CircleDIAttributes.ServiceProviderAttribute`1", componentList, stringBuilderPool);
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
            if (baseType is INamedTypeSymbol {
                Name: "ComponentBase",
                ContainingNamespace: INamespaceSymbol {
                    Name: "Components",
                    ContainingNamespace: INamespaceSymbol {
                        Name: "AspNetCore",
                        ContainingNamespace: INamespaceSymbol {
                            Name: "Microsoft",
                            ContainingNamespace: INamespaceSymbol { Name: "" }
                        }
                    }
                }
            })
                return component;

        return null;
    }
}

file static class RegisterServiceProviderAttributeExtension {
    public static void RegisterServiceProviderAttribute(this IncrementalGeneratorInitializationContext context, string serviceProviderAttributeName, IncrementalValueProvider<ImmutableArray<INamedTypeSymbol?>> componentList, ObjectPool<StringBuilder> stringBuilderPool) {
        // all service providers
        IncrementalValuesProvider<ServiceProviderWithExtra> serviceProviderWithExtraList = context.SyntaxProvider.ForAttributeWithMetadataName(
            serviceProviderAttributeName,
            static (SyntaxNode syntaxNode, CancellationToken _) => syntaxNode is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
            CreateServiceProviderWithExtra
        );

        // add components and init dependency tree
        IncrementalValuesProvider<ServiceProvider> serviceProviderList = serviceProviderWithExtraList.WithComparer(NoComparison<ServiceProviderWithExtra>.Instance)
            .Combine(componentList).WithComparer(NoComparison<(ServiceProviderWithExtra, ImmutableArray<INamedTypeSymbol?>)>.Instance)
            .Select(ServiceProviderWithComponents);


        // generate default service get-methods
        context.RegisterSourceOutput(serviceProviderWithExtraList, (SourceProductionContext context, ServiceProviderWithExtra serviceProviderWithExtra)=> context.GenerateDefaultServiceMethods(stringBuilderPool, serviceProviderWithExtra.serviceProvider, serviceProviderWithExtra.defaultServiceGeneration));

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

        serviceProvider.AddDefaultServices(blazorServiceGeneration);

        return (serviceProvider, blazorServiceGeneration, addRazorComponents);
    }

    /// <summary>
    /// <para>
    /// Adds the components from the given componentList to the given serviceProvider and then returns the provider.<br />
    /// The component is not added when already registered.
    /// </para>
    /// <para>It also inits dependency tree.</para>
    /// </summary>
    /// <param name="pair">serviiceProvider and componentList</param>
    /// <param name="_">CancellationToken is not used</param>
    /// <returns></returns>
    private static ServiceProvider ServiceProviderWithComponents((ServiceProviderWithExtra serviceProviderWithExtra, ImmutableArray<INamedTypeSymbol?> componentList) pair, CancellationToken _) {
        ServiceProvider serviceProvider = pair.serviceProviderWithExtra.serviceProvider;
        if (!pair.serviceProviderWithExtra.AddRazorComponents)
            return serviceProvider.InitDependencyTree();

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

        return serviceProvider.InitDependencyTree();
    }
}
