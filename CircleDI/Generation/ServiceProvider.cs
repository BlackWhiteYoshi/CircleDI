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
    /// The name/identifier of the class
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// <para>The namespace names the ServiceProvider is located.</para>
    /// <para>
    /// The first item is the most inner namespace and the last item is the most outer namespace.<br />
    /// So, to construct a fully-qualified name this list should be iterated backwards.
    /// </para>
    /// </summary>
    public List<string> NameSpaceList { get; init; }

    /// <summary>
    /// The type of the ServiceProvider: class, struct, record
    /// </summary>
    public ClassStructKeyword Keyword { get; init; } = ClassStructKeyword.Class;

    /// <summary>
    /// The type of the ScopeProvider: class, struct, record
    /// </summary>
    public ClassStructKeyword KeywordScope { get; init; } = ClassStructKeyword.Class;

    /// <summary>
    /// <para>The list of modifiers of this ServiceProvider without the last modifier "partial"</para>
    /// <para>Since the modifier "partial" is required and "partial" must be the last modifier, it can be omitted.</para>
    /// <para>e.g. ["public", "sealed"]</para>
    /// </summary>
    public string[] Modifiers { get; init; } = [];

    /// <summary>
    /// <para>The list of modifiers of the scoped ServiceProvider without the last modifier "partial"</para>
    /// <para>Since the modifier "partial" is required and "partial" must be the last modifier, it can be omitted.</para>
    /// <para>e.g. ["public", "sealed"]</para>
    /// </summary>
    public string[] ModifiersScope { get; init; } = [];

    /// <summary>
    /// <para>A list of all types (name and type) the ServiceProvider is nested in.</para>
    /// <para>
    /// The first item is the most inner type and the last item is the most outer type.<br />
    /// So, to construct a fully-qualified name this list should be iterated backwards.
    /// </para>
    /// </summary>
    public List<(string name, TypeKind type)> ContainingTypeList { get; init; }


    /// <summary>
    /// Name of the interface that the generated Service Provider implements.
    /// </summary>
    public required string InterfaceName {
        get => _interfaceName;
        init {
            _interfaceName = value;
            HasInterface = value.Length > 0;
        }
    }
    private readonly string _interfaceName = string.Empty;

    /// <summary>
    /// Is true when <see cref="InterfaceName"/> is not empty.<br />
    /// If empty, no interface will be generated.
    /// </summary>
    public bool HasInterface { get; private set; }

    /// <summary>
    /// The modifier that is relevant for the interface. The other modifier 'partial' is always applied.
    /// </summary>
    public Accessibility InterfaceAccessibility { get; init; } = Accessibility.Public;

    /// <summary>
    /// he modifier that is relevant for the interface scope. The other modifier 'partial' is always applied.
    /// </summary>
    public Accessibility InterfaceAccessibilityScope { get; init; } = Accessibility.Public;

    /// <summary>
    /// <para>The namespace names the interface is located.</para>
    /// <para>
    /// The first item is the most inner namespace and the last item is the most outer namespace.<br />
    /// So, to construct a fully-qualified name this list should be iterated backwards.
    /// </para>
    /// </summary>
    public List<string> InterfaceNameSpaceList { get; init; }

    /// <summary>
    /// <para>A list of all types (name and type) the interface is nested in.</para>
    /// <para>
    /// The first item is the most inner type and the last item is the most outer type.<br />
    /// So, to construct a fully-qualified name this list should be iterated backwards.
    /// </summary>
    public List<(string name, TypeKind type)> InterfaceContainingTypeList { get; init; }


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
    public (int index, int count) FindService(string serviceType) {
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


    /// <summary>
    /// <para>Diagnostics with Severity error that are related to the ServiceProvider or to the dependecy tree creation.</para>
    /// <para>Diagnostics that are related to <see cref="Service">Services</see> are not listed here, see <see cref="Service.ErrorList"/>.</para>
    /// </summary>
    public List<Diagnostic>? ErrorList { get; private set; }


    private readonly AttributeData serviceProviderAttribute;


    /// <summary>
    /// Creates a data-object representing a ServiceProviderAttribute.
    /// </summary>
    /// <param name="serviceProviderAttribute"></param>
    public ServiceProvider(AttributeData serviceProviderAttribute) {
        this.serviceProviderAttribute = serviceProviderAttribute;
        NameSpaceList = [];
        InterfaceNameSpaceList = [];
        ContainingTypeList = [];
        InterfaceContainingTypeList = [];
    } 

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
        if (serviceProvider.GetMembers("Scope") is [INamedTypeSymbol scope, ..])
            serviceProviderScope = scope;

        Debug.Assert(syntaxContext.Attributes.Length > 0);
        serviceProviderAttribute = syntaxContext.Attributes[0];


        if (serviceProviderSyntax.Modifiers[^1].ValueText != "partial") {
            Diagnostic error = serviceProviderAttribute.CreatePartialKeywordServiceProviderError();
            ErrorList ??= [];
            ErrorList.Add(error);
        }

        if (serviceProviderScopeSyntax != null && serviceProviderScopeSyntax.Modifiers[^1].ValueText != "partial") {
            Diagnostic error = serviceProviderAttribute.CreatePartialKeywordScopeProviderError();
            ErrorList ??= [];
            ErrorList.Add(error);
        }
        

        Name = serviceProviderSyntax.Identifier.ValueText;
        NameSpaceList = serviceProvider.GetNamespaceList();
        ContainingTypeList = serviceProvider.GetContainingTypeList();

        Keyword = serviceProviderSyntax switch {
            ClassDeclarationSyntax => ClassStructKeyword.Class,
            StructDeclarationSyntax => ClassStructKeyword.Struct,
            RecordDeclarationSyntax { ClassOrStructKeyword.ValueText: "" } => ClassStructKeyword.Record,
            RecordDeclarationSyntax { ClassOrStructKeyword.ValueText: "class" } => ClassStructKeyword.RecordClass,
            RecordDeclarationSyntax { ClassOrStructKeyword.ValueText: "struct" } => ClassStructKeyword.RecordStruct,
            _ => ClassStructKeyword.Class
        };

        KeywordScope = serviceProviderScopeSyntax switch {
            null => ClassStructKeyword.Class,
            ClassDeclarationSyntax => ClassStructKeyword.Class,
            StructDeclarationSyntax => ClassStructKeyword.Struct,
            RecordDeclarationSyntax { ClassOrStructKeyword.ValueText: "" } => ClassStructKeyword.Record,
            RecordDeclarationSyntax { ClassOrStructKeyword.ValueText: "class" } => ClassStructKeyword.RecordClass,
            RecordDeclarationSyntax { ClassOrStructKeyword.ValueText: "struct" } => ClassStructKeyword.RecordStruct,
            _ => ClassStructKeyword.Class
        };

        Modifiers = new string[serviceProviderSyntax.Modifiers.Count - 1];
        for (int i = 0; i < Modifiers.Length; i++)
            Modifiers[i] = serviceProviderSyntax.Modifiers[i].ValueText;

        if (serviceProviderScopeSyntax != null) {
            ModifiersScope = new string[serviceProviderScopeSyntax.Modifiers.Count - 1];
            for (int i = 0; i < ModifiersScope.Length; i++)
                ModifiersScope[i] = serviceProviderScopeSyntax.Modifiers[i].ValueText;
        }
        else
            ModifiersScope = ["public", "sealed"];


        // interface type
        InterfaceAccessibility = Accessibility.Public;
        InterfaceAccessibilityScope = Accessibility.Public;
        InterfaceNameSpaceList = NameSpaceList;
        InterfaceContainingTypeList = ContainingTypeList;
        if (serviceProviderAttribute.AttributeClass!.TypeArguments.Length > 0 && serviceProviderAttribute.AttributeClass.TypeArguments[0] is INamedTypeSymbol { TypeKind: TypeKind.Interface } interfaceSymbol) {
            InterfaceName = interfaceSymbol.Name;

            InterfaceAccessibility = interfaceSymbol.DeclaredAccessibility;
            if (interfaceSymbol.GetMembers("IScope") is [INamedTypeSymbol interfaceScopeSymbol] && interfaceScopeSymbol.TypeKind == TypeKind.Interface)
                InterfaceAccessibilityScope = interfaceScopeSymbol.DeclaredAccessibility;

            InterfaceNameSpaceList = interfaceSymbol.GetNamespaceList();
            InterfaceContainingTypeList = interfaceSymbol.GetContainingTypeList();
        }
        else if (serviceProviderAttribute.NamedArguments.Length > 0 && serviceProviderAttribute.NamedArguments.GetArgument<string?>("InterfaceName") is string interfaceName)
            InterfaceName = interfaceName;
        else
            InterfaceName = Name != "ServiceProvider" ? $"I{Name}" : "IServiceprovider";

        if (InterfaceName == "IServiceProvider") {
            Diagnostic error = serviceProviderAttribute.CreateInterfaceNameIServiceProviderError();
            ErrorList ??= [];
            ErrorList.Add(error);
        }


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
        if (serviceProviderScope != null) {
            AttributeData? scopedProviderAttributeNested = serviceProviderScope.GetAttribute("ScopedProviderAttribute");
            if (scopedProviderAttributeNested != null) {
                if (scopedProviderAttribute == null)
                    scopedProviderAttribute = scopedProviderAttributeNested;
                else {
                    Diagnostic error = scopedProviderAttributeNested.CreateScopeProviderAttributeTwiceError(scopedProviderAttribute);
                    ErrorList ??= [];
                    ErrorList.Add(error);
                    scopedProviderAttribute = null; // just ignore both and display error instead
                }
            }
        }

        bool generateScope = true;
        CreationTiming creationTimeScopeProvider = creationTimeMainProvider;
        GetAccess getAccessorScopeProvider = getAccessorMainProvider;
        GenerateDisposeMethodsScope = GenerateDisposeMethods;
        ThreadSafeScope = ThreadSafe;
        if (scopedProviderAttribute != null) {
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
        if (serviceProviderScopeSyntax != null)
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
        if (serviceProviderScope != null)
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
        if (serviceProviderScope != null)
            foreach (ISymbol member in serviceProviderScope.GetMembers("DisposeAsync"))
                if (member is IMethodSymbol { Parameters.Length: 0 }) {
                    HasDisposeAsyncMethodScope = true;
                    break;
                }


        // adding services and default services
        {
            // Default service ServiceProvider itself
            string implementationType = Name.GetFullyQualifiedName(NameSpaceList, ContainingTypeList);
            string serviceType = HasInterface ? InterfaceName.GetFullyQualifiedName(InterfaceNameSpaceList, InterfaceContainingTypeList) : implementationType;
            bool hasServiceSelf = false;

            // Default Service ServiceProvider.Scope self
            string implementationTypeScope = Name.GetFullyQualifiedName("Scope", NameSpaceList, ContainingTypeList);
            string serviceTypeScope = HasInterface ? InterfaceName.GetFullyQualifiedName("IScope", InterfaceNameSpaceList, InterfaceContainingTypeList) : implementationTypeScope;
            bool hasServiceSelfScope = false;

            // register services [Singleton<>, Scoped<>, Transient<>, Delegate<> attributes]
            foreach (AttributeData attributeData in serviceProvider.GetAttributes().Concat(serviceProviderScope?.GetAttributes() ?? Enumerable.Empty<AttributeData>())) {
                INamedTypeSymbol? attribute = attributeData.AttributeClass;
                if (attribute == null || attribute.TypeArguments.Length == 0)
                    continue;

                switch (attribute.Name) {
                    case "SingletonAttribute": {
                        Service service = new(serviceProvider, attributeData, ServiceLifetime.Singleton, creationTimeMainProvider, getAccessorMainProvider);
                        hasServiceSelf |= service.ServiceType == serviceType;
                        SingletonList.Add(service);

                        // check for recursive cunstructor call
                        if (service.ImplementationType == implementationType)
                            if (service.Implementation.Type == MemberType.None)
                                if (service.CreationTime == CreationTiming.Constructor) {
                                    Diagnostic error = attributeData.CreateEndlessRecursiveConstructorCallError(service.Name);
                                    ErrorList ??= [];
                                    ErrorList.Add(error);
                                }
                        break;
                    }
                    case "ScopedAttribute": {
                        if (!generateScope)
                            continue;

                        Service service = new(serviceProvider, attributeData, ServiceLifetime.Scoped, creationTimeScopeProvider, getAccessorScopeProvider);
                        hasServiceSelfScope |= service.ServiceType == serviceTypeScope;
                        ScopedList.Add(service);

                        // check for recursive cunstructor call
                        if (service.ImplementationType == implementationTypeScope)
                            if (service.Implementation.Type == MemberType.None)
                                if (service.CreationTime == CreationTiming.Constructor) {
                                    Diagnostic error = attributeData.CreateEndlessRecursiveConstructorCallScopeError(service.Name);
                                    ErrorList ??= [];
                                    ErrorList.Add(error);
                                }
                        break;
                    }
                    case "TransientAttribute": {
                        Service service = new(serviceProvider, attributeData, ServiceLifetime.Transient, CreationTiming.Lazy, getAccessorMainProvider);
                        TransientList.Add(service);
                        break;
                    }
                    case "DelegateAttribute": {
                        if (attributeData.ConstructorArguments.Length == 0)
                            continue;

                        Service service = new(serviceProvider, attributeData, getAccessorScopeProvider);
                        DelegateList.Add(service);
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
                ConstructorDependency[] constructorDependencyList;
                List<PropertyDependency> propertyDependencyList;
                if (serviceProviderScope != null) {
                    if (HasConstructorScope) {
                        (IMethodSymbol? constructor, Diagnostic? constructorListError) = Service.FindConstructor(serviceProviderScope!, serviceProviderAttribute);
                        if (constructor != null)
                            constructorDependencyList = constructor.CreateConstructorDependencyList();
                        else {
                            constructorDependencyList = [];
                            ErrorList ??= [];
                            ErrorList.Add(constructorListError!);
                        }
                    }
                    else
                        // default constructorDependency
                        constructorDependencyList = [new ConstructorDependency() {
                            Name = HasInterface ? InterfaceName : Name,
                            IsNamed = false,
                            ServiceIdentifier = serviceType,
                            HasAttribute = true,
                            ByRef = RefKind.None
                        }];

                    (propertyDependencyList, Diagnostic? propertyListError) = Service.CreatePropertyDependencyList(serviceProviderScope!, serviceProviderAttribute);
                    if (propertyListError != null) {
                        ErrorList ??= [];
                        ErrorList.Add(propertyListError);
                    }
                }
                else {
                    // default constructorDependency and no propertyDependencyList
                    constructorDependencyList = [new ConstructorDependency() {
                        Name = HasInterface ? InterfaceName : Name,
                        IsNamed = false,
                        ServiceIdentifier = serviceType,
                        HasAttribute = true,
                        ByRef = RefKind.None
                    }];
                    propertyDependencyList = [];
                }

                CreateScope = new Service() {
                    Lifetime = ServiceLifetime.TransientSingleton,
                    Name = "CreateScope()",
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


    #region Dependency Tree

    /// <summary>
    /// <para>Creates <see cref="SortedServiceList"/>.</para>
    /// <para>Creates and validates the dependency tree.</para>
    /// <para>
    /// The tree itself are <see cref="Service"/> nodes and the edges are <see cref="Dependency">Dependencies</see>.<br />
    /// The dependencies of a service can be found at <see cref="Service.Dependencies"/>.<br />
    /// A child of a node can be accessed with the reference <see cref="Dependency.Service"/>.<br />
    /// The number of children of a node is: <see cref="Service.ConstructorDependencyList"/>.Count + <see cref="Service.PropertyDependencyList"/>.Count
    /// </para>
    /// <para>
    /// The nodes are also listet in the 4 lists of <see cref="ServiceProvider"/>:<br />
    /// - <see cref="ServiceProvider.SingletonList"/><br />
    /// - <see cref="ServiceProvider.ScopedList"/><br />
    /// - <see cref="ServiceProvider.TransientList"/><br />
    /// - <see cref="ServiceProvider.DelegateList"/><br />
    /// </para>
    /// </summary>
    /// <remarks>In some circumstances circle dependencies are also allowed, so strictly spoken it's not a tree. Furthermore there is no one root node, there can be many root nodes and independent trees.</remarks>
    public void CreateDependencyTree() {
        SortedServiceList = [.. SingletonList, .. ScopedList, .. TransientList, .. DelegateList];
        SortedServiceList.Sort((Service x, Service y) => x.ServiceType.CompareTo(y.ServiceType));

        CreateDependencyTreeCore core = new(this);

        if (CreateScope is not null)
            core.InitNode(CreateScope);

        foreach (Service service in SortedServiceList)
            core.InitNode(service);
    }

    private readonly struct CreateDependencyTreeCore(ServiceProvider serviceProvider) {
        public readonly List<(Service node, Dependency edge)> path = [];

        public void InitNode(Service service) {
            if (service.TreeState.HasFlag(DependencyTreeFlags.Traversed))
                return;
            service.TreeState |= DependencyTreeFlags.Traversed;

            foreach (Dependency dependency in service.Dependencies) {
                path.Add((service, dependency));

                try {
                    Service? dependencyService;
                    if (dependency.IsNamed) {
                        foreach (Service providerService in serviceProvider.SortedServiceList)
                            if (providerService.Name == dependency.ServiceIdentifier) {
                                dependencyService = providerService;
                                goto dependencyServiceInitialized;
                            }
                        // else
                        {
                            Diagnostic error = ReferenceEquals(service, serviceProvider.CreateScope) switch {
                                true => serviceProvider.serviceProviderAttribute.CreateScopedProviderNamedUnregisteredError(GetFullyQualifiedName(serviceProvider), dependency.ServiceIdentifier),
                                false => serviceProvider.serviceProviderAttribute.CreateDependencyNamedUnregisteredError(service.Name, dependency.ServiceIdentifier)
                            };
                            serviceProvider.ErrorList ??= [];
                            serviceProvider.ErrorList.Add(error);
                            return;
                        }
                        dependencyServiceInitialized:;
                    }
                    else {
                        (int index, int count) = serviceProvider.FindService(dependency.ServiceIdentifier);
                        switch (count) {
                            case 0: {
                                serviceProvider.ErrorList ??= [];

                                Diagnostic error = ReferenceEquals(service, serviceProvider.CreateScope) switch {
                                    true => serviceProvider.serviceProviderAttribute.CreateScopedProviderUnregisteredError(GetFullyQualifiedName(serviceProvider), dependency.ServiceIdentifier),
                                    false => serviceProvider.serviceProviderAttribute.CreateDependencyUnregisteredError(service.Name, dependency.ServiceIdentifier)
                                };
                                serviceProvider.ErrorList.Add(error);

                                if (serviceProvider.HasInterface)
                                    if (dependency.ServiceIdentifier == serviceProvider.InterfaceName || dependency.ServiceIdentifier == $"{serviceProvider.InterfaceName}.IScope") {
                                        Diagnostic hintError = serviceProvider.serviceProviderAttribute.CreateDependencyInterfaceUndeclaredError(dependency.ServiceIdentifier, string.Join(".", serviceProvider.NameSpaceList.Reverse<string>()), serviceProvider.InterfaceName);
                                        serviceProvider.ErrorList.Add(hintError);
                                    }

                                return;
                            }
                            case 1: {
                                dependencyService = serviceProvider.SortedServiceList[index];
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

                                        Diagnostic error = serviceProvider.serviceProviderAttribute.CreateDependencyLifetimeAllServicesError(service.Name, dependency.ServiceIdentifier, servicesWithSameType);
                                        serviceProvider.ErrorList ??= [];
                                        serviceProvider.ErrorList.Add(error);
                                        return;
                                    }
                                    
                                    dependencyService = serviceProvider.SortedServiceList[serviceIndex];
                                    break;    
                                }
                                error:
                                {
                                    IEnumerable<string> servicesWithSameType = serviceProvider.SortedServiceList.Skip(index).Take(count).Select((Service service) => service.Name);
                                    bool isParameter = dependency is ConstructorDependency;

                                    Diagnostic error = ReferenceEquals(service, serviceProvider.CreateScope) switch {
                                        true => serviceProvider.serviceProviderAttribute.CreateScopedProviderAmbiguousError(GetFullyQualifiedName(serviceProvider), dependency.ServiceIdentifier, servicesWithSameType, isParameter),
                                        false => serviceProvider.serviceProviderAttribute.CreateDependencyAmbiguousError(service.Name, dependency.ServiceIdentifier, servicesWithSameType, isParameter)
                                    };
                                    serviceProvider.ErrorList ??= [];
                                    serviceProvider.ErrorList.Add(error);
                                    return;
                                }
                            }
                        }
                    }

                    // check CreationTiming
                    if (service.CreationTime == CreationTiming.Constructor && dependencyService.CreationTime == CreationTiming.Lazy)
                        if (!service.Lifetime.HasFlag(ServiceLifetime.Transient) && !dependencyService.Lifetime.HasFlag(ServiceLifetime.Transient)) {
                            Diagnostic error = serviceProvider.serviceProviderAttribute.CreateDependencyCreationTimingError(service.Name, dependency.ServiceIdentifier);
                            serviceProvider.ErrorList ??= [];
                            serviceProvider.ErrorList.Add(error);
                            return;
                        }

                    // check circle
                    for (int index = 0; index < path.Count; index++)
                        if (ReferenceEquals(path[index].node, dependencyService)) {
                            for (int circleIndex = index; circleIndex < path.Count; circleIndex++)
                                if (path[circleIndex].edge is PropertyDependency propertyDependency && !dependencyService.Lifetime.HasFlag(ServiceLifetime.Transient)) {
                                    propertyDependency.IsCircular = true;
                                    goto circleCheckOK;
                                }
                            // else
                            {
                                IEnumerable<string> servicesInCircle = path.Skip(index).Select(((Service node, Dependency edge) pair) => pair.node.Name);
                                // append first item again as last item to illustrate the circle
                                servicesInCircle = servicesInCircle.Concat(servicesInCircle.Take(1));

                                Diagnostic error = serviceProvider.serviceProviderAttribute.CreateDependencyCircleError(servicesInCircle);
                                serviceProvider.ErrorList ??= [];
                                serviceProvider.ErrorList.Add(error);
                                return;
                            }
                        }
                    circleCheckOK:

                    InitNode(dependencyService);
                    dependency.Service = dependencyService;

                    // check Lifetime
                    switch (service.Lifetime) {
                        case ServiceLifetime.Singleton:
                            if (dependency.Service.Lifetime is ServiceLifetime.Scoped) {
                                Diagnostic error = serviceProvider.serviceProviderAttribute.CreateDependencyLifetimeScopeError(service.Name, dependency.ServiceIdentifier);
                                serviceProvider.ErrorList ??= [];
                                serviceProvider.ErrorList.Add(error);
                                return;
                            }
                            if (dependency.Service.Lifetime is ServiceLifetime.TransientScoped) {
                                Diagnostic error = serviceProvider.serviceProviderAttribute.CreateDependencyLifetimeTransientError(service.Name, dependency.ServiceIdentifier);
                                serviceProvider.ErrorList ??= [];
                                serviceProvider.ErrorList.Add(error);
                                return;
                            }
                            break;
                        case ServiceLifetime.TransientSingleton:
                            if (dependency.Service.Lifetime is ServiceLifetime.Scoped) {
                                Diagnostic error = serviceProvider.serviceProviderAttribute.CreateScopedProviderLifetimeScopeError(GetFullyQualifiedName(serviceProvider), dependency.ServiceIdentifier);
                                serviceProvider.ErrorList ??= [];
                                serviceProvider.ErrorList.Add(error);
                                return;
                            }
                            if (dependency.Service.Lifetime is ServiceLifetime.TransientScoped) {
                                Diagnostic error = serviceProvider.serviceProviderAttribute.CreateScopedProviderLifetimeTransientError(GetFullyQualifiedName(serviceProvider), dependency.ServiceIdentifier);
                                serviceProvider.ErrorList ??= [];
                                serviceProvider.ErrorList.Add(error);
                                return;
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
                }
            }


            static string GetFullyQualifiedName(ServiceProvider serviceProvider) => serviceProvider.Name.GetFullyQualifiedName(serviceProvider.NameSpaceList, serviceProvider.ContainingTypeList);
        }
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

        if (Name != other.Name)
            return false;
        if (!NameSpaceList.SequenceEqual(other.NameSpaceList))
            return false;
        if (Keyword != other.Keyword)
            return false;
        if (KeywordScope != other.KeywordScope)
            return false;
        if (!Modifiers.SequenceEqual(other.Modifiers))
            return false;
        if (!ModifiersScope.SequenceEqual(other.ModifiersScope))
            return false;
        if (!ContainingTypeList.SequenceEqual(other.ContainingTypeList))
            return false;

        if (InterfaceName != other.InterfaceName)
            return false;
        if (InterfaceAccessibility != other.InterfaceAccessibility)
            return false;
        if (InterfaceAccessibilityScope != other.InterfaceAccessibilityScope)
            return false;
        if (!InterfaceNameSpaceList.SequenceEqual(other.InterfaceNameSpaceList))
            return false;
        if (!InterfaceContainingTypeList.SequenceEqual(other.InterfaceContainingTypeList))
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

        if (!ErrorList.SequenceNullEqual(other.ErrorList))
            return false;

        return true;
    }

    public override int GetHashCode() {
        int hashCode = Name.GetHashCode();
        hashCode = CombineList(hashCode, NameSpaceList);
        hashCode = Combine(hashCode, Keyword.GetHashCode());
        hashCode = Combine(hashCode, KeywordScope.GetHashCode());
        hashCode = CombineList(hashCode, Modifiers);
        hashCode = CombineList(hashCode, ModifiersScope);
        hashCode = CombineList(hashCode, ContainingTypeList);

        hashCode = Combine(hashCode, InterfaceName.GetHashCode());
        hashCode = Combine(hashCode, InterfaceAccessibility.GetHashCode());
        hashCode = Combine(hashCode, InterfaceAccessibilityScope.GetHashCode());
        hashCode = CombineList(hashCode, InterfaceNameSpaceList);
        hashCode = CombineList(hashCode, InterfaceContainingTypeList);

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

        if (ErrorList != null)
            hashCode = CombineList(hashCode, ErrorList);

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
