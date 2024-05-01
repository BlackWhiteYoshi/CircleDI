using CircleDI.Defenitions;
using CircleDI.Extensions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CircleDI.Generation;

/// <summary>
/// <para>Holds all relevant information to source generate the ServiceProvider class and interface.</para>
/// <para>It includes the parameters of the attribute, parameters of the ScopedProviderAttribute and lists for the services (Singleton, Scoped, Transient, Delegate).</para>
/// <para>This object can be used as equality check to short circuit the generation.</para>
/// </summary>
public sealed class ServiceProvider : IEquatable<ServiceProvider> {
    /// <summary>
    /// Name, namespace, containing types and type parameters of ServiceProvider
    /// </summary>
    public required TypeName Identifier { get; init; }

    /// <summary>
    /// The type of the ServiceProvider: class, struct, record
    /// </summary>
    public TypeKeyword Keyword { get; init; } = TypeKeyword.Class;

    /// <summary>
    /// <para>The list of modifiers of this ServiceProvider without the last modifier "partial"</para>
    /// <para>Since the modifier "partial" is required and "partial" must be the last modifier, it can be omitted.</para>
    /// <para>e.g. ["public", "sealed"]</para>
    /// </summary>
    public string[] Modifiers { get; init; } = [];


    /// <summary>
    /// Name, namespace, containing types and type parameters of ScopeProvider
    /// </summary>
    public required TypeName IdentifierScope { get; init; }

    /// <summary>
    /// The type of the ScopeProvider: class, struct, record
    /// </summary>
    public TypeKeyword KeywordScope { get; init; } = TypeKeyword.Class;

    /// <summary>
    /// <para>The list of modifiers of the scoped ServiceProvider without the last modifier "partial"</para>
    /// <para>Since the modifier "partial" is required and "partial" must be the last modifier, it can be omitted.</para>
    /// <para>e.g. ["public", "sealed"]</para>
    /// </summary>
    public string[] ModifiersScope { get; init; } = [];


    /// <summary>
    /// Name, namespace, containing types and type parameters of Interface
    /// </summary>
    public required TypeName InterfaceIdentifier { get; init; }

    /// <summary>
    /// Is true when <see cref="InterfaceName"/> is not empty.<br />
    /// If empty, no interface will be generated.
    /// </summary>
    public bool HasInterface => InterfaceIdentifier.Name != string.Empty;

    /// <summary>
    /// The modifier that is relevant for the interface. The other modifier 'partial' is always applied.
    /// </summary>
    public Accessibility InterfaceAccessibility { get; init; } = Accessibility.Public;


    /// <summary>
    /// Name, namespace, containing types and type parameters of InterfaceScope
    /// </summary>
    public required TypeName InterfaceIdentifierScope { get; init; }

    /// <summary>
    /// he modifier that is relevant for the interface scope. The other modifier 'partial' is always applied.
    /// </summary>
    public Accessibility InterfaceAccessibilityScope { get; init; } = Accessibility.Public;


    /// <summary>
    /// <para>Parameters of the constructor or InitServices() in ServiceProvider.</para>
    /// <para><see cref="Dependency.Service"/> reference is null, <see cref="Dependency.ServiceName"/> is empty, <see cref="Dependency.ServiceType"/> is alwalys set.</para>
    /// </summary>
    public List<ConstructorDependency> ConstructorParameterList { get; init; } = [];

    /// <summary>
    /// <para>Parameters of the constructor or InitServices() in ScopeProvider.</para>
    /// <para><see cref="Dependency.Service"/> reference is null, <see cref="Dependency.ServiceName"/> is empty, <see cref="Dependency.ServiceType"/> is alwalys set.</para>
    /// </summary>
    public List<ConstructorDependency> ConstructorParameterListScope { get; init; } = [];

    /// <summary>
    /// Indicates if a custom constructor is defined.
    /// </summary>
    public bool HasConstructor { get; init; }

    /// <summary>
    /// Indicates if the Scope class inside ServiceProvider has a custom constructor defined.
    /// </summary>
    public bool HasConstructorScope { get; init; }


    /// <summary>
    /// Indicates if a custom Dispose()-method is defined.
    /// </summary>
    public bool HasDisposeMethod { get; init; }

    /// <summary>
    /// Indicates if the Scope class inside ServiceProvider has a custom Dispose()-method defined.
    /// </summary>
    public bool HasDisposeMethodScope { get; init; }

    /// <summary>
    /// Indicates if a custom DisposeAsync()-method is defined.
    /// </summary>
    public bool HasDisposeAsyncMethod { get; init; }

    /// <summary>
    /// Indicates if the Scope class inside ServiceProvider has a custom DisposeAsync()-method defined.
    /// </summary>
    public bool HasDisposeAsyncMethodScope { get; init; }


    /// <summary>
    /// <para>Option that toggles generating of the Dispose methods of this ServiceProvider.</para>
    /// <para>
    /// Options are<br />
    /// - generate both<br />
    /// - generate sync only<br />
    /// - generate async only<br />
    /// - skip generation
    /// </para>
    /// </summary>
    public DisposeGeneration GenerateDisposeMethods { get; init; } = DisposeGeneration.GenerateBoth;

    /// <summary>
    /// <para>Option that toggles generating of the Dispose methods in the Scope class inside ServiceProvider.</para>
    /// <para>
    /// Options are<br />
    /// - generate both<br />
    /// - generate sync only<br />
    /// - generate async only<br />
    /// - skip generation
    /// </para>
    /// </summary>
    public DisposeGeneration GenerateDisposeMethodsScope { get; init; } = DisposeGeneration.GenerateBoth;


    /// <summary>
    /// Affects 2 scenarios:<br />
    /// - If lazy singleton services have to use a lock or not.<br />
    /// - If the list to track disposable transient services is a <see cref="System.Collections.Concurrent.ConcurrentBag{T}">ConcurrentBag&lt;T&gt;</see> or <see cref="List{T}"/>.
    /// </summary>
    public bool ThreadSafe { get; init; }

    /// <summary>
    /// Affects 2 scenarios:<br />
    /// - If lazy scoped services have to use a lock or not.<br />
    /// - If the list to track disposable scoped transient services is a <see cref="System.Collections.Concurrent.ConcurrentBag{T}">ConcurrentBag&lt;T&gt;</see> or <see cref="List{T}"/>.
    /// </summary>
    public bool ThreadSafeScope { get; init; }


    /// <summary>
    /// Indicates generating of a scope class/interface is enabled.
    /// </summary>
    [MemberNotNullWhen(true, nameof(CreateScope))]
    public bool GenerateScope => CreateScope is not null;

    /// <summary>
    /// <para>
    /// A "special" Service to generate the CreateScope() method.<br />
    /// This Service is not listed, so in the dependency tree it is always a root node.
    /// </para>
    /// <para>If generating Scope is disabled, this is null.</para>
    /// </summary>
    public Service? CreateScope { get; }


    /// <summary>
    /// All registered services with <see cref="ServiceLifetime.Singleton"/>.
    /// </summary>
    public List<Service> SingletonList { get; } = [];

    /// <summary>
    /// All registered services with <see cref="ServiceLifetime.Scoped"/>.
    /// </summary>
    public List<Service> ScopedList { get; } = [];

    /// <summary>
    /// All registered services with <see cref="ServiceLifetime.Transient"/> or <see cref="ServiceLifetime.TransientScoped"/>.
    /// </summary>
    public List<Service> TransientList { get; } = [];

    /// <summary>
    /// <para>All registered services of type <see cref="Delegate"/>.</para>
    /// <para>DelegateServices have no dependencies, so they are always leaf nodes in the dependency tree.</para>
    /// </summary>
    public List<Service> DelegateList { get; } = [];

    /// <summary>
    /// <para>All Services listed in this provider.<br />
    /// The list is sorted by <see cref="Service.ServiceType"/>.</para>
    /// <para>When setter is used, the given list will be modified/sorted.</para>
    /// </summary>
    public List<Service> SortedServiceList { get; private set; } = [];

    /// <summary>
    /// Binary search the sorted <see cref="SortedServiceList"/>.
    /// </summary>
    /// <param name="serviceType"></param>
    /// <returns>The index of the first occurrence and the number of matched services.</returns>
    public (int index, int count) FindService(TypeName serviceType) {
        int lowerBound = 0;
        int upperBound = SortedServiceList.Count;
        while (lowerBound < upperBound) {
            int index = (lowerBound + upperBound) / 2;

            switch (SortedServiceList[index].ServiceType.CompareTo(serviceType)) {
                case -1:
                    lowerBound = index + 1;
                    break;
                case 1:
                    upperBound = index;
                    break;
                case 0:
                    int start = index;
                    while (start > 0 && SortedServiceList[start - 1].ServiceType == serviceType)
                        start--;

                    int end = index + 1;
                    while (end < SortedServiceList.Count && SortedServiceList[end].ServiceType == serviceType)
                        end++;

                    return (start, end - start);
            }
        }

        return (-1, 0);
    }


    public DiagnosticErrorManager ErrorManager { get; private set; }


    /// <summary>
    /// Creates a data-object representing a ServiceProviderAttribute.
    /// </summary>
    /// <param name="serviceProviderAttribute"></param>
    public ServiceProvider(AttributeData serviceProviderAttribute) => ErrorManager = new DiagnosticErrorManager(serviceProviderAttribute);

    /// <summary>
    /// Creates a data-object based on a ServiceProviderAttribute.
    /// </summary>
    /// <param name="syntaxContext"></param>
    [SetsRequiredMembers]
    public ServiceProvider(GeneratorAttributeSyntaxContext syntaxContext) {
        TypeDeclarationSyntax serviceProviderSyntax = (TypeDeclarationSyntax)syntaxContext.TargetNode;

        TypeDeclarationSyntax? serviceProviderScopeSyntax = null;
        foreach (MemberDeclarationSyntax memberSyntax in serviceProviderSyntax.Members)
            if (memberSyntax is TypeDeclarationSyntax { Identifier.ValueText: "Scope" } scopedProviderSyntax) {
                serviceProviderScopeSyntax = scopedProviderSyntax;
                break;
            }

        INamedTypeSymbol serviceProvider = (INamedTypeSymbol)syntaxContext.TargetSymbol;

        INamedTypeSymbol? serviceProviderScope = null;
        if (serviceProvider.GetMembers("Scope") is [INamedTypeSymbol scope])
            serviceProviderScope = scope;

        Debug.Assert(syntaxContext.Attributes.Length > 0);
        AttributeData serviceProviderAttribute = syntaxContext.Attributes[0];
        ErrorManager = new DiagnosticErrorManager(serviceProviderAttribute);


        if (serviceProviderSyntax.Modifiers[^1].ValueText != "partial")
            ErrorManager.AddPartialKeywordServiceProviderError();

        if (serviceProviderScopeSyntax is not null && serviceProviderScopeSyntax.Modifiers[^1].ValueText != "partial")
            ErrorManager.AddPartialKeywordScopeProviderError();


        Keyword = serviceProviderSyntax switch {
            ClassDeclarationSyntax => TypeKeyword.Class,
            StructDeclarationSyntax => TypeKeyword.Struct,
            RecordDeclarationSyntax { ClassOrStructKeyword.ValueText: "" } => TypeKeyword.Record,
            RecordDeclarationSyntax { ClassOrStructKeyword.ValueText: "class" } => TypeKeyword.RecordClass,
            RecordDeclarationSyntax { ClassOrStructKeyword.ValueText: "struct" } => TypeKeyword.RecordStruct,
            _ => TypeKeyword.Class
        };

        KeywordScope = serviceProviderScopeSyntax switch {
            null => TypeKeyword.Class,
            ClassDeclarationSyntax => TypeKeyword.Class,
            StructDeclarationSyntax => TypeKeyword.Struct,
            RecordDeclarationSyntax { ClassOrStructKeyword.ValueText: "" } => TypeKeyword.Record,
            RecordDeclarationSyntax { ClassOrStructKeyword.ValueText: "class" } => TypeKeyword.RecordClass,
            RecordDeclarationSyntax { ClassOrStructKeyword.ValueText: "struct" } => TypeKeyword.RecordStruct,
            _ => TypeKeyword.Class
        };

        Identifier = new TypeName(serviceProvider);
        IdentifierScope = serviceProviderScope switch {
            INamedTypeSymbol typeSymbol => new TypeName(typeSymbol),
            _ => IdentifierScope = new TypeName("Scope", KeywordScope, Identifier.NameSpaceList, [Identifier, .. Identifier.ContainingTypeList], [], [])
        };

        Modifiers = new string[serviceProviderSyntax.Modifiers.Count - 1];
        for (int i = 0; i < Modifiers.Length; i++)
            Modifiers[i] = serviceProviderSyntax.Modifiers[i].ValueText;

        if (serviceProviderScopeSyntax is not null) {
            ModifiersScope = new string[serviceProviderScopeSyntax.Modifiers.Count - 1];
            for (int i = 0; i < ModifiersScope.Length; i++)
                ModifiersScope[i] = serviceProviderScopeSyntax.Modifiers[i].ValueText;
        }
        else
            ModifiersScope = ["public", "sealed"];


        // interface type
        INamedTypeSymbol? interfaceSymbol = null;
        INamedTypeSymbol? interfaceScopeSymbol = null;
        if (serviceProviderAttribute.AttributeClass!.TypeArguments.Length > 0 && serviceProviderAttribute.AttributeClass.TypeArguments[0] is INamedTypeSymbol { TypeKind: TypeKind.Interface } symbol) {
            interfaceSymbol = symbol;
            if (interfaceSymbol.GetMembers("IScope") is [INamedTypeSymbol { TypeKind: TypeKind.Interface } scopeSymbol])
                interfaceScopeSymbol = scopeSymbol;
        }

        InterfaceAccessibility = Accessibility.Public;
        InterfaceAccessibilityScope = Accessibility.Public;
        if (interfaceSymbol is not null) {
            InterfaceIdentifier = new TypeName(interfaceSymbol);
            InterfaceAccessibility = interfaceSymbol.DeclaredAccessibility;
        }
        else if (serviceProviderAttribute.NamedArguments.GetArgument<string?>("InterfaceName") is string interfaceName)
            InterfaceIdentifier = new TypeName(interfaceName, TypeKeyword.Interface, Identifier.NameSpaceList, Identifier.ContainingTypeList, [], []);
        else
            InterfaceIdentifier = new TypeName(Identifier.Name != "ServiceProvider" ? $"I{Identifier.Name}" : "IServiceprovider", TypeKeyword.Interface, Identifier.NameSpaceList, Identifier.ContainingTypeList, [], []);

        if (InterfaceIdentifier.Name == "IServiceProvider")
            ErrorManager.AddInterfaceNameIServiceProviderError();

        if (interfaceScopeSymbol is not null) {
            InterfaceIdentifierScope = new TypeName(interfaceScopeSymbol);
            InterfaceAccessibilityScope = interfaceScopeSymbol.DeclaredAccessibility;
        }
        else
            InterfaceIdentifierScope = new TypeName("IScope", TypeKeyword.Interface, InterfaceIdentifier.NameSpaceList, [InterfaceIdentifier, .. InterfaceIdentifier.ContainingTypeList], [], []);


        // parameters on ServiceProviderAttribute
        CreationTiming creationTimeMainProvider = CreationTiming.Constructor;
        GetAccess getAccessorMainProvider = GetAccess.Property;
        GenerateDisposeMethods = DisposeGeneration.GenerateBoth;
        ThreadSafe = true;
        if (serviceProviderAttribute.NamedArguments.Length > 0) {
            if (serviceProviderAttribute.NamedArguments.GetArgument<int?>("CreationTime") is int creationTime)
                creationTimeMainProvider = (CreationTiming)creationTime;
            if (serviceProviderAttribute.NamedArguments.GetArgument<int?>("GetAccessor") is int getAccessor)
                getAccessorMainProvider = (GetAccess)getAccessor;
            if (serviceProviderAttribute.NamedArguments.GetArgument<int?>("GenerateDisposeMethods") is int generateDisposeMethods)
                GenerateDisposeMethods = (DisposeGeneration)generateDisposeMethods;
            if (serviceProviderAttribute.NamedArguments.GetArgument<bool?>("ThreadSafe") is bool threadSafe)
                ThreadSafe = threadSafe;
        }


        // parameters on ScopedProviderAttribute
        AttributeData? scopedProviderAttribute = serviceProvider.GetAttribute("ScopedProviderAttribute");
        if (serviceProviderScope is not null) {
            AttributeData? scopedProviderAttributeNested = serviceProviderScope.GetAttribute("ScopedProviderAttribute");
            if (scopedProviderAttributeNested is not null) {
                if (scopedProviderAttribute is null)
                    scopedProviderAttribute = scopedProviderAttributeNested;
                else {
                    ErrorManager.AddScopeProviderAttributeTwiceError(scopedProviderAttributeNested, scopedProviderAttribute);
                    scopedProviderAttribute = null; // just ignore both and display error instead
                }
            }
        }

        bool generateScope = true;
        CreationTiming creationTimeScopeProvider = creationTimeMainProvider;
        GetAccess getAccessorScopeProvider = getAccessorMainProvider;
        GenerateDisposeMethodsScope = GenerateDisposeMethods;
        ThreadSafeScope = ThreadSafe;
        if (scopedProviderAttribute is not null) {
            if (scopedProviderAttribute.NamedArguments.GetArgument<bool?>("Generate") is bool generate)
                generateScope = generate;
            if (scopedProviderAttribute.NamedArguments.GetArgument<int?>("CreationTime") is int creationTime)
                creationTimeScopeProvider = (CreationTiming)creationTime;
            if (scopedProviderAttribute.NamedArguments.GetArgument<int?>("GetAccessor") is int getAccessor)
                getAccessorScopeProvider = (GetAccess)getAccessor;
            if (scopedProviderAttribute.NamedArguments.GetArgument<int?>("GenerateDisposeMethods") is int generateDisposeMethods)
                GenerateDisposeMethodsScope = (DisposeGeneration)generateDisposeMethods;
            if (scopedProviderAttribute.NamedArguments.GetArgument<bool?>("ThreadSafe") is bool threadSafe)
                ThreadSafeScope = threadSafe;
        }


        HasConstructor = false;
        foreach (MemberDeclarationSyntax member in serviceProviderSyntax.Members)
            if (member is ConstructorDeclarationSyntax) {
                HasConstructor = true;
                break;
            }

        HasConstructorScope = false;
        if (serviceProviderScopeSyntax is not null)
            foreach (MemberDeclarationSyntax member in serviceProviderScopeSyntax.Members)
                if (member is ConstructorDeclarationSyntax) {
                    HasConstructorScope = true;
                    break;
                }

        HasDisposeMethod = false;
        foreach (ISymbol member in serviceProvider.GetMembers("Dispose"))
            if (member is IMethodSymbol { Parameters.Length: 0 }) {
                HasDisposeMethod = true;
                break;
            }

        HasDisposeMethodScope = false;
        if (serviceProviderScope is not null)
            foreach (ISymbol member in serviceProviderScope.GetMembers("Dispose"))
                if (member is IMethodSymbol { Parameters.Length: 0 }) {
                    HasDisposeMethodScope = true;
                    break;
                }

        HasDisposeAsyncMethod = false;
        foreach (ISymbol member in serviceProvider.GetMembers("DisposeAsync"))
            if (member is IMethodSymbol { Parameters.Length: 0 }) {
                HasDisposeAsyncMethod = true;
                break;
            }

        HasDisposeAsyncMethodScope = false;
        if (serviceProviderScope is not null)
            foreach (ISymbol member in serviceProviderScope.GetMembers("DisposeAsync"))
                if (member is IMethodSymbol { Parameters.Length: 0 }) {
                    HasDisposeAsyncMethodScope = true;
                    break;
                }


        // adding services and default services
        {
            // Default service ServiceProvider itself
            TypeName implementationType = Identifier;
            TypeName serviceType = HasInterface ? InterfaceIdentifier : implementationType;
            bool hasServiceSelf = false;

            // Default Service ServiceProvider.Scope self
            TypeName implementationTypeScope = IdentifierScope;
            TypeName serviceTypeScope = HasInterface ? InterfaceIdentifierScope : implementationTypeScope;
            bool hasServiceSelfScope = false;

            // add ServiceProvider as service parameter to ConstructorParameterList, it is important that it is the first member in the list
            if (generateScope)
                ConstructorParameterListScope.Add(new ConstructorDependency() {
                    Name = Identifier.Name,
                    ServiceName = string.Empty,
                    ServiceType = serviceType,
                    HasAttribute = true,
                    ByRef = RefKind.None
                });

            // register services [Singleton<>, Scoped<>, Transient<>, Delegate<>, Import<> attributes]
            IEnumerable<AttributeData> listedAttributes = serviceProviderScope switch {
                null => serviceProvider.GetAttributes(),
                _ => serviceProvider.GetAttributes().Concat(serviceProviderScope.GetAttributes())
            };
            foreach (AttributeData attributeData in listedAttributes) {
                ErrorManager.CurrentAttribute = attributeData;

                INamedTypeSymbol? attribute = attributeData.AttributeClass;
                if (attribute is not { ContainingNamespace: { Name: "CircleDIAttributes", ContainingNamespace.Name: "" }, ContainingType: null })
                    continue;

                if (attribute.TypeKind is TypeKind.Error || attribute.TypeArguments.Any((ITypeSymbol typeSymbol) => typeSymbol.TypeKind is TypeKind.Error)) {
                    ErrorManager.AddInvalidServiceRegistrationError(Identifier, InterfaceIdentifier);
                    continue;
                }

                ModuleRegistration moduleRegistration = new(this, generateScope, creationTimeMainProvider, creationTimeScopeProvider, getAccessorMainProvider, getAccessorScopeProvider);
                switch (attribute.Name) {
                    case "SingletonAttribute": {
                        Service service = new(serviceProvider, attributeData, ServiceLifetime.Singleton, creationTimeMainProvider, getAccessorMainProvider, ErrorManager);
                        hasServiceSelf |= service.ServiceType == serviceType;
                        SingletonList.Add(service);

                        // check for recursive cunstructor call
                        if (service.ImplementationType == implementationType && service.Implementation.Type == MemberType.None && service.CreationTime == CreationTiming.Constructor)
                            ErrorManager.AddEndlessRecursiveConstructorCallError(service.Name);
                        break;
                    }
                    case "ScopedAttribute": {
                        if (!generateScope)
                            break;

                        Service service = new(serviceProvider, attributeData, ServiceLifetime.Scoped, creationTimeScopeProvider, getAccessorScopeProvider, ErrorManager);
                        hasServiceSelfScope |= service.ServiceType == serviceTypeScope;
                        ScopedList.Add(service);

                        // check for recursive cunstructor call
                        if (service.ImplementationType == implementationTypeScope && service.Implementation.Type == MemberType.None && service.CreationTime == CreationTiming.Constructor)
                            ErrorManager.AddEndlessRecursiveConstructorCallScopeError(service.Name);
                        break;
                    }
                    case "TransientAttribute": {
                        Service service = new(serviceProvider, attributeData, ServiceLifetime.Transient, CreationTiming.Lazy, getAccessorMainProvider, ErrorManager);
                        TransientList.Add(service);
                        break;
                    }
                    case "DelegateAttribute": {
                        if (attributeData.ConstructorArguments.Length == 0)
                            break;

                        Service service = new(serviceProvider, attributeData, getAccessorScopeProvider, ErrorManager);
                        DelegateList.Add(service);
                        break;
                    }
                    case "ImportAttribute": {
                        moduleRegistration.RegisterServices(attributeData);
                        break;
                    }
                }
            }

            // Default service ServiceProvider itself
            if (!hasServiceSelf)
                SingletonList.Add(new Service() {
                    Lifetime = ServiceLifetime.Singleton,
                    ServiceType = serviceType,
                    ImplementationType = implementationType,
                    Implementation = new ImplementationMember(MemberType.Field, "this", IsStatic: false, IsScoped: false),

                    ConstructorDependencyList = [],
                    PropertyDependencyList = [],
                    Dependencies = [],
                    Name = "Self",
                    CreationTime = creationTimeMainProvider,
                    GetAccessor = getAccessorMainProvider,
                    IsDisposable = false,
                    IsAsyncDisposable = false
                });

            if (generateScope) {
                // Default Service ServiceProvider.Scope self
                if (!hasServiceSelfScope)
                    ScopedList.Add(new Service() {
                        Lifetime = ServiceLifetime.Scoped,
                        ServiceType = serviceTypeScope,
                        ImplementationType = implementationTypeScope,
                        Implementation = new ImplementationMember(MemberType.Field, "this", IsStatic: false, IsScoped: true),

                        ConstructorDependencyList = [],
                        PropertyDependencyList = [],
                        Dependencies = [],
                        Name = "SelfScope",
                        CreationTime = creationTimeScopeProvider,
                        GetAccessor = getAccessorScopeProvider,
                        IsDisposable = false,
                        IsAsyncDisposable = false
                    });


                // "special" method CreateScope()
                List<ConstructorDependency> constructorDependencyList;
                List<PropertyDependency> propertyDependencyList;
                if (serviceProviderScope is not null) {
                    if (HasConstructorScope) {
                        constructorDependencyList = serviceProviderScope.CreateConstructorDependencyList(ErrorManager) ?? [];
                    }
                    else
                        // default constructorDependency
                        constructorDependencyList = ConstructorParameterListScope;

                    propertyDependencyList = serviceProviderScope.CreatePropertyDependencyList(ErrorManager) ?? [];
                }
                else {
                    // default constructorDependency and no propertyDependencyList
                    constructorDependencyList = ConstructorParameterListScope;
                    propertyDependencyList = [];
                }

                CreateScope = new Service() {
                    Lifetime = ServiceLifetime.TransientSingleton,
                    Name = "MethodCreateScope",
                    ServiceType = serviceTypeScope,
                    ImplementationType = implementationTypeScope,
                    CreationTime = CreationTiming.Lazy,
                    GetAccessor = GetAccess.Property,
                    ConstructorDependencyList = constructorDependencyList,
                    PropertyDependencyList = propertyDependencyList,
                    Dependencies = constructorDependencyList.Concat<Dependency>(propertyDependencyList).Where((Dependency dependency) => dependency.HasAttribute)
                };
            }
        }
    }


    #region Register Services

    /// <summary>
    /// This type can add all services in a module.
    /// </summary>
    private readonly struct ModuleRegistration(ServiceProvider serviceProvider, bool generateScope, CreationTiming creationTimeMainProvider, CreationTiming creationTimeScopeProvider, GetAccess getAccessorMainProvider, GetAccess getAccessorScopeProvider) {
        private readonly List<INamedTypeSymbol> path = [];

        /// <summary>
        /// Adds all services from the module to the service provider.
        /// </summary>
        /// <param name="importAttribute"></param>
        public readonly void RegisterServices(AttributeData importAttribute) {
            Debug.Assert(importAttribute.AttributeClass?.TypeArguments.Length > 0);

            INamedTypeSymbol module = (INamedTypeSymbol)importAttribute.AttributeClass!.TypeArguments[0];
            INamedTypeSymbol? moduleScope = module.GetMembers("Scope") switch {
                [INamedTypeSymbol scope] => scope,
                _ => null
            };
            TypeName moduleTypeName = new(module);


            ImportMode importMode = importAttribute.ConstructorArguments switch {
                [TypedConstant { Value: int importModeValue }] => (ImportMode)importModeValue,
                _ => ImportMode.Auto
            };
            IMethodSymbol? serivceConstructor = null;
            IMethodSymbol? serivceConstructorScope = null;
            switch (importMode) {
                case ImportMode.Auto: {
                    if (module.TypeKind is TypeKind.Interface || module.IsStatic) {
                        importMode = ImportMode.Static;
                        goto case ImportMode.Static;
                    }

                    serivceConstructor = module.FindConstructor(serviceProvider.ErrorManager);
                    if (serivceConstructor?.Parameters.Length > 0) {
                        importMode = ImportMode.Parameter;
                        goto case ImportMode.Parameter;
                    }

                    if (moduleScope != null) {
                        serivceConstructorScope = moduleScope.FindConstructor(serviceProvider.ErrorManager);
                        if (serivceConstructorScope?.Parameters.Length > 0) {
                            importMode = ImportMode.Parameter;
                            goto case ImportMode.Parameter;
                        }
                    }

                    importMode = ImportMode.Service;
                    goto case ImportMode.Service;
                }
                case ImportMode.Static: {
                    break;
                }
                case ImportMode.Service: {
                    {
                        if (serivceConstructor is null) {
                            serivceConstructor = module.FindConstructor(serviceProvider.ErrorManager);
                            if (serivceConstructor is null)
                                return;
                        }
                        List<ConstructorDependency> constructorDependencyList = serivceConstructor!.CreateConstructorDependencyList();

                        List<PropertyDependency>? propertyDependencyList = module.CreatePropertyDependencyList(serviceProvider.ErrorManager);
                        if (propertyDependencyList is null)
                            return;

                        serviceProvider.SingletonList.Add(new Service() {
                            Lifetime = ServiceLifetime.Singleton,
                            Name = module.Name,
                            ServiceType = moduleTypeName,
                            ImplementationType = moduleTypeName,
                            CreationTime = CreationTiming.Constructor,
                            GetAccessor = GetAccess.Property,
                            ConstructorDependencyList = constructorDependencyList,
                            PropertyDependencyList = propertyDependencyList,
                            Dependencies = constructorDependencyList.Concat<Dependency>(propertyDependencyList)
                        });
                    }
                    if (moduleScope is not null) {
                        TypeName moduleTypeNameScope = new(moduleScope);

                        if (serivceConstructorScope is null) {
                            serivceConstructorScope = moduleScope.FindConstructor(serviceProvider.ErrorManager);
                            if (serivceConstructorScope is null)
                                return;
                        }
                        List<ConstructorDependency> constructorDependencyList = serivceConstructorScope!.CreateConstructorDependencyList();

                        List<PropertyDependency>? propertyDependencyList = moduleScope.CreatePropertyDependencyList(serviceProvider.ErrorManager);
                        if (propertyDependencyList is null)
                            return;

                        serviceProvider.ScopedList.Add(new Service() {
                            Lifetime = ServiceLifetime.Scoped,
                            Name = $"{module.Name}Scope",
                            ServiceType = moduleTypeNameScope,
                            ImplementationType = moduleTypeNameScope,
                            CreationTime = CreationTiming.Constructor,
                            GetAccessor = GetAccess.Property,
                            ConstructorDependencyList = constructorDependencyList,
                            PropertyDependencyList = propertyDependencyList,
                            Dependencies = constructorDependencyList.Concat<Dependency>(propertyDependencyList)
                        });
                    }
                    break;
                }
                case ImportMode.Parameter: {
                    serviceProvider.ConstructorParameterList.Add(new ConstructorDependency() {
                        Name = module.Name,
                        ServiceName = string.Empty,
                        ServiceType = new TypeName(module),
                        HasAttribute = false,
                        ByRef = RefKind.None
                    });
                    if (moduleScope is not null)
                        serviceProvider.ConstructorParameterListScope.Add(new ConstructorDependency() {
                            Name = $"{module.Name}Scope",
                            ServiceName = string.Empty,
                            ServiceType = new TypeName(moduleScope),
                            HasAttribute = false,
                            ByRef = RefKind.None
                        });
                    break;
                }
                default:
                    // just ignore entire ImportAttribute when invalid enum input
                    return;
            }

            // check circle
            for (int index = 0; index < path.Count; index++)
                if (SymbolEqualityComparer.Default.Equals(path[index], module)) {
                    IEnumerable<string> modulesInCircle = path.Skip(index).Select((INamedTypeSymbol typeSymbol) => typeSymbol.ToDisplayString());
                    // append first item again as last item to illustrate the circle
                    modulesInCircle = modulesInCircle.Concat(modulesInCircle.Take(1));

                    serviceProvider.ErrorManager.AddModuleCircleError(serviceProvider.Identifier, modulesInCircle);
                    return;
                }
            path.Add(module);

            try {
                IEnumerable<AttributeData> listedAttributes = moduleScope switch {
                    null => module.GetAttributes(),
                    _ => module.GetAttributes().Concat(moduleScope.GetAttributes())
                };

                foreach (AttributeData attributeData in listedAttributes) {
                    serviceProvider.ErrorManager.CurrentAttribute = attributeData;

                    INamedTypeSymbol? attribute = attributeData.AttributeClass;
                    if (attribute is not { ContainingNamespace: { Name: "CircleDIAttributes", ContainingNamespace.Name: "" }, ContainingType: null })
                        continue;

                    if (attribute.TypeKind is TypeKind.Error || attribute.TypeArguments.Any((ITypeSymbol typeSymbol) => typeSymbol.TypeKind is TypeKind.Error)) {
                        serviceProvider.ErrorManager.AddInvalidServiceRegistrationError(serviceProvider.Identifier, serviceProvider.InterfaceIdentifier);
                        continue;
                    }

                    switch (attribute.Name) {
                        case "SingletonAttribute": {
                            Service service = new(module, attributeData, ServiceLifetime.Singleton, creationTimeMainProvider, getAccessorMainProvider, serviceProvider.ErrorManager) {
                                ImportMode = importMode,
                                Module = moduleTypeName
                            };
                            serviceProvider.SingletonList.Add(service);
                            break;
                        }
                        case "ScopedAttribute": {
                            if (!generateScope)
                                break;

                            Service service = new(module, attributeData, ServiceLifetime.Scoped, creationTimeScopeProvider, getAccessorScopeProvider, serviceProvider.ErrorManager) {
                                ImportMode = importMode,
                                Module = moduleTypeName
                            };
                            serviceProvider.ScopedList.Add(service);
                            break;
                        }
                        case "TransientAttribute": {
                            Service service = new(module, attributeData, ServiceLifetime.Transient, CreationTiming.Lazy, getAccessorMainProvider, serviceProvider.ErrorManager) {
                                ImportMode = importMode,
                                Module = moduleTypeName
                            };
                            serviceProvider.TransientList.Add(service);
                            break;
                        }
                        case "DelegateAttribute": {
                            if (attributeData.ConstructorArguments.Length == 0)
                                break;

                            Service service = new(module, attributeData, getAccessorScopeProvider, serviceProvider.ErrorManager) {
                                ImportMode = importMode,
                                Module = moduleTypeName
                            };
                            serviceProvider.DelegateList.Add(service);
                            break;
                        }
                        case "ImportAttribute": {
                            RegisterServices(attributeData);
                            break;
                        }
                    }
                }
            }
            finally {
                path.RemoveAt(path.Count - 1);
            }
        }
    }

    #endregion


    #region Dependency Tree

    /// <summary>
    /// <para>Fills <see cref="SortedServiceList"/> with <see cref="SingletonList"/>, <see cref="ScopedList"/>, <see cref="TransientList"/>, <see cref="DelegateList"/> and sorts by <see cref="Service.ServiceType"/>.</para>
    /// <para>Creates and validates the dependency tree of <see cref="SortedServiceList"/> + <see cref="CreateScope"/>.</para>
    /// <para>
    /// The tree itself are <see cref="Service"/> nodes and the edges are <see cref="Dependency">Dependencies</see>.<br />
    /// The dependencies of a service can be found at <see cref="Service.Dependencies"/>.<br />
    /// A child of a node can be accessed with the reference <see cref="Dependency.Service"/>.<br />
    /// The number of children of a node is: <see cref="Service.ConstructorDependencyList"/>.Count + <see cref="Service.PropertyDependencyList"/>.Count
    /// </para>
    /// <para>If the ServiceProvider has any errors, this method does nothing.</para>
    /// </summary>
    /// <remarks>In some circumstances circle dependencies are also allowed, so strictly spoken it's not a tree. Furthermore there is no one root node, there can be many root nodes and independent trees.</remarks>
    public ServiceProvider CreateDependencyTree() {
        if (ErrorManager.ErrorList.Count > 0)
            return this;
        
        SortedServiceList = [.. SingletonList, .. ScopedList, .. TransientList, .. DelegateList];
        SortedServiceList.Sort((Service x, Service y) => x.ServiceType.CompareTo(y.ServiceType));

        // init dependency tree
        {
            DependencyTreeInitializer initializer = new(this);

            if (CreateScope is not null)
                initializer.InitNode(CreateScope);

            foreach (Service service in SortedServiceList)
                initializer.InitNode(service);
        }

        return this;
    }

    /// <summary>
    /// Creates and validates the dependency tree of a collecion of services.
    /// </summary>
    /// <param name="services"></param>
    public void CreateDependencyTree(IEnumerable<(Service service, AttributeData attribute)> services) {
        DependencyTreeInitializer initializer = new(this);
        foreach ((Service service, AttributeData attribute) in services) {
            ErrorManager.CurrentAttribute = attribute;
            initializer.InitNode(service);
        }
    }

    private struct DependencyTreeInitializer(ServiceProvider serviceProvider) {
        /// <summary>
        /// <para>Holds information to track shortcircuits.</para>
        /// <para>
        /// It contains at which node it starts,
        /// when it can be removed
        /// as well as the shortcircuitNodeList, the list of edges that are building the cycle.
        /// </para>
        /// </summary>
        /// <param name="cycleEnd"></param>
        /// <param name="cycleStart"></param>
        /// <param name="shortcircuitNodeList"></param>
        private readonly struct Cycle(int cycleEnd, int cycleStart, Dependency[] shortcircuitNodeList) {
            /// <summary>
            /// index of <see cref="path">path</see> node where the cycle starts.<br />
            /// If after that point, it should be removed.
            /// </summary>
            public readonly int cycleEnd = cycleEnd;

            /// <summary>
            /// index of <see cref="path">path</see> node with the weak edge.<br />
            /// If at or after that point, shortcircuitNodeList must be checked and therefore this cycle is active.<br />
            /// If -1, it is already active.
            /// </summary>
            public readonly int cycleStart = cycleStart;

            /// <summary>
            /// First dependency is the weak dependency and last dependency points to the last node. Dependency pointing to a path node is omitted.
            /// </summary>
            public readonly Dependency[] shortcircuitList = shortcircuitNodeList;
        }

        private readonly List<Dependency> path = [new ConstructorDependency() { Name = null!, ServiceType = null!, ServiceName = null!, HasAttribute = false, ByRef = default }];
        private readonly List<Cycle> cycleList = [];


        public readonly void InitNode(Service service) {
            path[0].Service = service;
            InitNodeRecursion(service);
        }

        private readonly void InitNodeRecursion(Service service) {
            if (service.TreeState.visited.HasFlag(serviceProvider.DependencyTreeFlag))
                return;
            service.TreeState.visited = serviceProvider.DependencyTreeFlag;

            try {
                foreach (Dependency dependency in service.Dependencies) {
                    Debug.Assert(dependency.ServiceName != string.Empty || dependency.ServiceType is not null);
                    path.Add(dependency);

                    try {
                        if (dependency.ServiceType is null) {
                            foreach (Service providerService in serviceProvider.SortedServiceList)
                                if (providerService.Name == dependency.ServiceName) {
                                    dependency.Service = providerService;
                                    goto dependencyServiceInitialized;
                                }
                            // else
                            {
                                if (ReferenceEquals(service, serviceProvider.CreateScope))
                                    serviceProvider.ErrorManager.AddScopedProviderNamedUnregisteredError(serviceProvider.Identifier, dependency.ServiceName);
                                else
                                    serviceProvider.ErrorManager.AddDependencyNamedUnregisteredError(service.Name, dependency.ServiceName);
                                return;
                            }
                            dependencyServiceInitialized:;
                        }
                        else {
                            (int index, int count) = serviceProvider.FindService(dependency.ServiceType);
                            switch (count) {
                                case 0: {
                                    if (ReferenceEquals(service, serviceProvider.CreateScope))
                                        serviceProvider.ErrorManager.AddScopedProviderUnregisteredError(serviceProvider.Identifier, dependency.ServiceType);
                                    else
                                        serviceProvider.ErrorManager.AddDependencyUnregisteredError(service.Name, dependency.ServiceType);

                                    if (serviceProvider.HasInterface)
                                        if (dependency.ServiceType.Name == serviceProvider.InterfaceIdentifier.Name || dependency.ServiceType.Name == $"{serviceProvider.InterfaceIdentifier.Name}.IScope")
                                            // hintError
                                            serviceProvider.ErrorManager.AddDependencyInterfaceUndeclaredError(dependency.ServiceType, string.Join(".", serviceProvider.Identifier.NameSpaceList.Reverse<string>()), serviceProvider.InterfaceIdentifier.Name);
                                    
                                    return;
                                }
                                case 1: {
                                    dependency.Service = serviceProvider.SortedServiceList[index];
                                    break;
                                }
                                default: {
                                    // filter all invalid services and if only
                                    if (service.Lifetime.HasFlag(ServiceLifetime.Singleton)) {
                                        int serviceIndex = -1;
                                        for (int i = index; i < index + count; i++)
                                            if (!serviceProvider.SortedServiceList[i].Lifetime.HasFlag(ServiceLifetime.Scoped))
                                                if (serviceIndex == -1)
                                                    serviceIndex = i;
                                                else
                                                    goto error;

                                        if (serviceIndex == -1) {
                                            IEnumerable<string> servicesWithSameType = serviceProvider.SortedServiceList.Skip(index).Take(count).Select((Service service) => service.Name);
                                            serviceProvider.ErrorManager.AddDependencyLifetimeAllServicesError(service.Name, dependency.ServiceType, servicesWithSameType);
                                            return;
                                        }

                                        dependency.Service = serviceProvider.SortedServiceList[serviceIndex];
                                        break;
                                    }
                                    error:
                                    {
                                        IEnumerable<string> servicesWithSameType = serviceProvider.SortedServiceList.Skip(index).Take(count).Select((Service service) => service.Name);
                                        bool isParameter = dependency is ConstructorDependency;

                                        if (ReferenceEquals(service, serviceProvider.CreateScope))
                                            serviceProvider.ErrorManager.AddScopedProviderAmbiguousError(serviceProvider.Identifier, dependency.ServiceType, servicesWithSameType, isParameter);
                                        else
                                            serviceProvider.ErrorManager.AddDependencyAmbiguousError(service.Name, dependency.ServiceType, servicesWithSameType, isParameter);

                                        return;
                                    }
                                }
                            }
                        }

                        // check CreationTiming
                        if (!dependency.Service.Lifetime.HasFlag(ServiceLifetime.Transient))
                            // singleton or scoped dependency (delegate dependencies are automatically filtered at the next step)
                            if (service.CreationTimeTransitive == CreationTiming.Constructor && dependency.Service.CreationTimeTransitive == CreationTiming.Lazy)
                                // constructor on lazy
                                if (service.Lifetime == dependency.Service.Lifetime)
                                    // singleton on singleton or scoped on scoped
                                    dependency.Service.CreationTimeTransitive = CreationTiming.Constructor;

                        // set cycleLists active
                        for (int i = 0; i < cycleList.Count; i++)
                            if (cycleList[i].cycleStart == path.Count - 1)
                                cycleList[i] = new Cycle(cycleList[i].cycleEnd, -1, cycleList[i].shortcircuitList);
                        // check for short circuits
                        for (int cycleIndex = 0; cycleIndex < cycleList.Count; cycleIndex++)
                            if (cycleList[cycleIndex].cycleStart == -1)
                                for (int shortcircuitIndex = 0; shortcircuitIndex < cycleList[cycleIndex].shortcircuitList.Length; shortcircuitIndex++)
                                    if (ReferenceEquals(cycleList[cycleIndex].shortcircuitList[shortcircuitIndex].Service, dependency.Service)) {
                                        // cycleList[cycleIndex].cycleEnd is start of circle
                                        for (int circleIndex = path.Count - 1; circleIndex >= cycleList[cycleIndex].cycleEnd; circleIndex--)
                                            if (path[circleIndex] is PropertyDependency propertyDependency && !dependency.Service.Lifetime.HasFlag(ServiceLifetime.Transient)) {
                                                // path[circleIndex] is weak dependency
                                                if (!propertyDependency.IsCircular) {
                                                    propertyDependency.IsCircular = true;

                                                    if (circleIndex < path.Count - 1) {
                                                        // crate nodeCycle starting with weak dependency and ending with last node, depdency pointing to path node is omitted.
                                                        Dependency[] nodeCycle = new Dependency[path.Count - 1 - circleIndex];
                                                        for (int i = 0; i < nodeCycle.Length; i++)
                                                            nodeCycle[i] = path[circleIndex + i];

                                                        cycleList.Add(new Cycle(cycleList[cycleIndex].cycleEnd, circleIndex, nodeCycle));
                                                    }
                                                }

                                                goto circleCheckOK;
                                            }
                                        // else
                                        {
                                            IEnumerable<string> servicesInCircle = path.Skip(cycleList[cycleIndex].cycleEnd - 1)
                                                .Concat(cycleList[cycleIndex].shortcircuitList.Skip(shortcircuitIndex + 1)).Select((Dependency edge) => edge.Service!.Name);
                                            // append first item again as last item to illustrate the circle
                                            servicesInCircle = servicesInCircle.Concat(servicesInCircle.Take(1));

                                            serviceProvider.ErrorManager.AddDependencyCircleError(servicesInCircle);
                                            return;
                                        }
                                    }

                        // check circle
                        for (int index = 0; index < path.Count - 1; index++)
                            if (ReferenceEquals(path[index].Service, dependency.Service)) {
                                index++;
                                // path[index] is start of circle
                                for (int circleIndex = path.Count - 1; circleIndex >= index; circleIndex--)
                                    if (path[circleIndex] is PropertyDependency propertyDependency && !propertyDependency.Service!.Lifetime.HasFlag(ServiceLifetime.Transient)) {
                                        // path[circleIndex] is weak dependency
                                        if (!propertyDependency.IsCircular) {
                                            propertyDependency.IsCircular = true;

                                            if (circleIndex < path.Count - 1) {
                                                // crate nodeCycle starting with weak dependency and ending with last node, depdency pointing to path node is omitted.
                                                Dependency[] nodeCycle = new Dependency[path.Count - 1 - circleIndex];
                                                for (int i = 0; i < nodeCycle.Length; i++)
                                                    nodeCycle[i] = path[circleIndex + i];

                                                cycleList.Add(new Cycle(index, circleIndex, nodeCycle));
                                            }
                                        }

                                        goto circleCheckOK;
                                    }
                                // else
                                {
                                    IEnumerable<string> servicesInCircle = path.Skip(index - 1).Select((Dependency edge) => edge.Service!.Name);
                                    serviceProvider.ErrorManager.AddDependencyCircleError(servicesInCircle);
                                    return;
                                }
                            }
                        circleCheckOK:

                        InitNodeRecursion(dependency.Service);

                        // check Lifetime
                        switch (service.Lifetime) {
                            case ServiceLifetime.Singleton:
                                if (dependency.Service.Lifetime.HasFlag(ServiceLifetime.Scoped))
                                    switch (dependency.Service.Lifetime) {
                                        case ServiceLifetime.Scoped:
                                            serviceProvider.ErrorManager.AddDependencyLifetimeScopeError(service.Name, dependency.Service.ServiceType);
                                            return;
                                        case ServiceLifetime.TransientScoped:
                                            serviceProvider.ErrorManager.AddDependencyLifetimeTransientError(service.Name, dependency.Service.ServiceType);
                                            return;
                                        case ServiceLifetime.DelegateScoped:
                                            serviceProvider.ErrorManager.AddDependencyLifetimeDelegateError(service.Name, dependency.Service.ServiceType);
                                            return;
                                        default:
                                            throw new Exception($"Not Reachable: Singleton service has scoped dependency, but this specific scoped lifetime is not handled: {dependency.Service.Lifetime}");
                                    }
                                break;
                            case ServiceLifetime.TransientSingleton:
                                if (dependency.Service.Lifetime.HasFlag(ServiceLifetime.Scoped))
                                    switch (dependency.Service.Lifetime) {
                                        case ServiceLifetime.Scoped:
                                            serviceProvider.ErrorManager.AddScopedProviderLifetimeScopeError(serviceProvider.Identifier, dependency.Service.ServiceType);
                                            return;
                                        case ServiceLifetime.TransientScoped:
                                            serviceProvider.ErrorManager.AddScopedProviderLifetimeTransientError(serviceProvider.Identifier, dependency.Service.ServiceType);
                                            return;
                                        case ServiceLifetime.DelegateScoped:
                                            serviceProvider.ErrorManager.AddScopedProviderLifetimeDelegateError(serviceProvider.Identifier, dependency.Service.ServiceType);
                                            return;
                                        default:
                                            throw new Exception($"Not Reachable: TransientSingleton service has scoped dependency, but this specific scoped lifetime is not handled: {dependency.Service.Lifetime}");
                                    }
                                break;
                            case ServiceLifetime.Transient:
                                if (dependency.Service.Lifetime.HasFlag(ServiceLifetime.Scoped))
                                    service.Lifetime = ServiceLifetime.TransientScoped;
                                break;
                        }
                    }
                    finally {
                        path.RemoveAt(path.Count - 1);
                        Debug.Assert(serviceProvider.ErrorManager.ErrorList.Count > 0 || dependency.Service is not null);
                    }
                }
            }
            finally {
                service.TreeState.init = serviceProvider.DependencyTreeFlag;
                for (int i = 0; i < cycleList.Count; i++)
                    if (cycleList[i].cycleEnd == path.Count)
                        cycleList.RemoveAt(i--);
            }
        }
    }


    /// <summary>
    /// A flag can be set to indicate that a node has been visited.<br />
    /// Each node has 32 flags, where bit 0 is reserved for creating the tree, so the tree can be visited 31 times before the flags must be resetted.<br />
    /// For advancing this flag to the next bit, see <see cref="NextDependencyTreeFlag"/>.
    /// </summary>
    public DependencyTreeFlags DependencyTreeFlag { get; private set; } = DependencyTreeFlags.New;

    /// <summary>
    /// Advances <see cref="DependencyTreeFlag"/> to the next bit.<br />
    /// If at the last bit (bit 32), all flags get resetted and it starts again at the second right bit. (the most right bit is reserved for creating the tree).
    /// </summary>
    public void NextDependencyTreeFlag() {
        // check if left most bit (bit 32) is currently 1
        if (DependencyTreeFlag == (DependencyTreeFlags)int.MinValue) {
            // reset all flags
            foreach (Service service in SortedServiceList)
                service.TreeState = default;
            DependencyTreeFlag = DependencyTreeFlags.New;
        }
        else
            DependencyTreeFlag = (DependencyTreeFlags)((int)DependencyTreeFlag << 1);
    }

    #endregion


    #region Equals

    public static bool operator ==(ServiceProvider? left, ServiceProvider? right)
        => (left, right) switch {
            (null, null) => true,
            (null, not null) => false,
            (not null, _) => left.Equals(right)
        };

    public static bool operator !=(ServiceProvider? left, ServiceProvider? right) => !(left == right);

    public override bool Equals(object? obj) => Equals(obj as ServiceProvider);

    public bool Equals(ServiceProvider? other) {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        if (Identifier != other.Identifier)
            return false;
        if (Keyword != other.Keyword)
            return false;
        if (!Modifiers.SequenceEqual(other.Modifiers))
            return false;

        if (IdentifierScope != other.IdentifierScope)
            return false;
        if (KeywordScope != other.KeywordScope)
            return false;
        if (!ModifiersScope.SequenceEqual(other.ModifiersScope))
            return false;

        if (InterfaceIdentifier != other.InterfaceIdentifier)
            return false;
        if (InterfaceAccessibility != other.InterfaceAccessibility)
            return false;

        if (InterfaceIdentifierScope != other.InterfaceIdentifierScope)
            return false;
        if (InterfaceAccessibilityScope != other.InterfaceAccessibilityScope)
            return false;

        if (!ConstructorParameterList.SequenceEqual(other.ConstructorParameterList))
            return false;
        if (!ConstructorParameterListScope.SequenceEqual(other.ConstructorParameterListScope))
            return false;
        if (HasConstructor != other.HasConstructor)
            return false;
        if (HasConstructorScope != other.HasConstructorScope)
            return false;

        if (HasDisposeMethod != other.HasDisposeMethod)
            return false;
        if (HasDisposeMethodScope != other.HasDisposeMethodScope)
            return false;
        if (HasDisposeAsyncMethod != other.HasDisposeAsyncMethod)
            return false;
        if (HasDisposeAsyncMethodScope != other.HasDisposeAsyncMethodScope)
            return false;

        if (GenerateDisposeMethods != other.GenerateDisposeMethods)
            return false;
        if (GenerateDisposeMethodsScope != other.GenerateDisposeMethodsScope)
            return false;

        if (ThreadSafe != other.ThreadSafe)
            return false;
        if (ThreadSafeScope != other.ThreadSafeScope)
            return false;

        if (CreateScope != other.CreateScope)
            return false;

        if (!SingletonList.SequenceEqual(other.SingletonList))
            return false;
        if (!ScopedList.SequenceEqual(other.ScopedList))
            return false;
        if (!TransientList.SequenceEqual(other.TransientList))
            return false;
        if (!DelegateList.SequenceEqual(other.DelegateList))
            return false;

        if (!ErrorManager.ErrorList.SequenceEqual(other.ErrorManager.ErrorList))
            return false;

        return true;
    }

    public override int GetHashCode() {
        int hashCode = Identifier.GetHashCode();
        hashCode = Combine(hashCode, Keyword.GetHashCode());
        hashCode = CombineList(hashCode, Modifiers);

        hashCode = Combine(hashCode, IdentifierScope.GetHashCode());
        hashCode = Combine(hashCode, KeywordScope.GetHashCode());
        hashCode = CombineList(hashCode, ModifiersScope);

        hashCode = Combine(hashCode, InterfaceIdentifier.GetHashCode());
        hashCode = Combine(hashCode, InterfaceAccessibility.GetHashCode());

        hashCode = Combine(hashCode, InterfaceIdentifierScope.GetHashCode());
        hashCode = Combine(hashCode, InterfaceAccessibilityScope.GetHashCode());

        hashCode = CombineList(hashCode, ConstructorParameterList);
        hashCode = CombineList(hashCode, ConstructorParameterListScope);
        hashCode = Combine(hashCode, HasConstructor.GetHashCode());
        hashCode = Combine(hashCode, HasConstructorScope.GetHashCode());

        hashCode = Combine(hashCode, HasDisposeMethod.GetHashCode());
        hashCode = Combine(hashCode, HasDisposeMethodScope.GetHashCode());
        hashCode = Combine(hashCode, HasDisposeAsyncMethod.GetHashCode());
        hashCode = Combine(hashCode, HasDisposeAsyncMethodScope.GetHashCode());

        hashCode = Combine(hashCode, GenerateDisposeMethods.GetHashCode());
        hashCode = Combine(hashCode, GenerateDisposeMethodsScope.GetHashCode());

        hashCode = Combine(hashCode, ThreadSafe.GetHashCode());
        hashCode = Combine(hashCode, ThreadSafeScope.GetHashCode());

        if (CreateScope is not null)
            hashCode = Combine(hashCode, CreateScope.GetHashCode());

        hashCode = CombineList(hashCode, SingletonList);
        hashCode = CombineList(hashCode, ScopedList);
        hashCode = CombineList(hashCode, TransientList);
        hashCode = CombineList(hashCode, DelegateList);

        hashCode = CombineList(hashCode, ErrorManager.ErrorList);

        return hashCode;


        static int CombineList<T>(int hashCode, IEnumerable<T> list) where T : notnull {
            foreach (T item in list)
                hashCode = Combine(hashCode, item.GetHashCode());
            return hashCode;
        }

        static int Combine(int h1, int h2) {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }

    #endregion
}
