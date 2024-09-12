using CircleDI.Defenitions;
using CircleDI.Extensions;
using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;

namespace CircleDI.Generation;

/// <summary>
/// Contains all necessary information to source generate a service.<br />
/// It includes the information about the attribute (lifetime, type parameters and arguments) as well as the necessary information about the implementation class.
/// </summary>
[DebuggerDisplay("{Name}")]
public sealed class Service : IEquatable<Service> {
    /// <summary>
    /// Lifetime type of the service.<br />
    /// Additionally the service is also located in the corresponding list.
    /// </summary>
    public required ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;


    /// <summary>
    /// <para>The type of the service to interact with. Often it is an interface.</para>
    /// <para>If only 1 TypeArgument in the register attribute is present, then ServiceType and ImplementationType is the same.</para>
    /// </summary>
    public required TypeName ServiceType { get; init; }

    /// <summary>
    /// Is true, when service is singleton/scoped and service and implementation are the same and they are a struct, record struct or native/built-in type.<br />
    /// Is false, when Class or record class.
    /// </summary>
    public bool IsRefable { get; init; } = false;

    /// <summary>
    /// Indicates wheather this service has open/unbound type paramters.
    /// </summary>
    public bool IsGeneric { get; init; } = false;

    /// <summary>
    /// <para>The type of the actual object that will be instatiated.</para>
    /// <para>If only 1 TypeArgument in the register attribute is present, then ServiceType and ImplementationType is the same.</para>
    /// </summary>
    public required TypeName ImplementationType { get; init; }

    /// <summary>
    /// <para>Information about a custom implementation for retrieving an object for the Service.</para>
    /// </summary>
    public ImplementationMember Implementation { get; init; }


    /// <summary>
    /// <para>
    /// The import mode of the module this service is declared at.<br />
    /// It is <see cref="ImportMode.Auto"/> when the service is declared at the ServiceProvider.
    /// </para>
    /// <para>If this property is <see cref="ImportMode.Auto"/>, <see cref="ImportMode"/> should be null.</para>
    /// </summary>
    public ImportMode ImportMode { get; init; } = ImportMode.Auto;

    /// <summary>
    /// <para>
    /// The full qualified name of the module this service is declared at.<br />
    /// It is null when the service is declared at the ServiceProvider.
    /// </para>
    /// <para>If this property is null, <see cref="ImportMode"/> should be <see cref="ImportMode.Auto"/>.</para>
    /// </summary>
    public TypeName? Module { get; init; }


    /// <summary>
    /// <para>Identifier for the backing field and get property/method.</para>
    /// <para>Default is the name of the implementation type with '.' replaced with '_'.</para>
    /// <para>If the getter is a method, for the identifier a "Get" prefix will be used, but this does not affect the name.</para>
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// <para>Decides whether this service will be lazy constructed or instantiated inside the constructor.</para>
    /// <para>Default is <see cref="CreationTiming.Constructor"/>.</para>
    /// </summary>
    public CreationTiming CreationTime { get; init; } = CreationTiming.Constructor;

    /// <summary>
    /// <para>
    /// The same value as <see cref="CreationTime"/>
    /// except the service is dependency of a <see cref="CreationTiming.Constructor"/> service, then it will be also set to <see cref="CreationTiming.Constructor"/>.
    /// </para>
    /// <para>This value can change inside DependencyTree initialization, for caching equals check only <see cref="CreationTime"/> is used.</para>
    /// </summary>
    public CreationTiming CreationTimeTransitive { get; set; } = CreationTiming.Constructor;

    /// <summary>
    /// <para>>Decides whether this service accessor will be a property or method.</para
    /// <para>Default is <see cref="GetAccess.Property"/>.</para>
    /// </summary>
    public GetAccess GetAccessor { get; init; } = GetAccess.Property;


    /// <summary>
    /// <para>Dependencies that are listed as parameters in the constructor/function to cunstruct the object.<br />
    /// If <see cref="Implementation"/> has type <see cref="MemberType.Method"/>, this list contains the parameters of that function.<br />
    /// If <see cref="Implementation"/> has type <see cref="MemberType.Field"/> or <see cref="MemberType.Property"/>, this list is empty.</para>
    /// <para>
    /// The order of the list correspond to the order of parameters of the constructor/function.<br />
    /// e.g. the first item in the list is the service for the first parameter of the constructor/function.
    /// </para>
    /// </summary>
    public required List<ConstructorDependency> ConstructorDependencyList { get; init; }

    /// <summary>
    /// <para>Dependencies that are listed as properties.</para>
    /// <para>Properties get listed as dependency when they are either required or have the <see cref="Attributes.DependencyAttribute">DependencyAttribute</see></para>
    /// <para>If <see cref="Implementation"/> has type <see cref="MemberType.None"/>, this list is empty.</para>
    /// </summary>
    public required List<PropertyDependency> PropertyDependencyList { get; init; }


    /// <summary>
    /// <para>
    /// Implements <see cref="IDisposable"/> directly or indirectly.<br />
    /// Implementing Indirectly means, when one of the base types implements <see cref="IDisposable"/> (this also applies recursively for the base types).
    /// </para>
    /// <para>Default is false.</para>
    /// </summary>
    public bool IsDisposable { get; init; } = false;

    /// <summary>
    /// <para>
    /// Implements IAsyncDisposable directly or indirectly.<br />
    /// Implementing Indirectly means, when one of the base types implements IAsyncDisposable (this also applies recursively for the base types).
    /// </para>
    /// <para>Default is false.</para>
    /// </summary>
    public bool IsAsyncDisposable { get; init; } = false;


    /// <summary>
    /// <para>Iterator for all dependencies of this service.</para>
    /// <para>Normally it is just <see cref="ConstructorDependencyList"/> concatenated with <see cref="PropertyDependencyList"/>.</para>
    /// <para>
    /// The exceptions are:<br />
    /// - "special" service <see cref="ServiceProvider.CreateScope"/>, it needs an iterator with filter by <see cref="Dependency.HasAttribute"/>.
    /// - "endpoint" services (see Blazor.MinimalAPI), it needs an iterator with filter by <see cref="Dependency.HasAttribute"/>.
    /// </para>
    /// </summary>
    public required IEnumerable<Dependency> Dependencies { get; init; }
    private IEnumerable<Dependency> DependenciesDefaultIterator => ConstructorDependencyList.Concat<Dependency>(PropertyDependencyList);


    /// <summary>
    /// Some space for flags to create, process or consume the dependency tree.<br />
    /// Visited is set as soon this node is traversed.<br />
    /// Init is set when this node visits ends.
    /// </summary>
    public ref (DependencyTreeFlags visited, DependencyTreeFlags init) TreeState => ref _treeState;
    private (DependencyTreeFlags visited, DependencyTreeFlags init) _treeState = default;


    /// <summary>
    /// <para>Creates a data-object that represents a service attribute (SingletonAttribute&lt;&gt;, ScopedAttribute&lt;&gt;, TransientAttribute&lt;&gt;, DelegateAttribute&lt;&gt;).</para>
    /// </summary>
    public Service() { }

    /// <summary>
    /// Creates a data-object based on a service attribute (SingletonAttribute&lt;&gt;, ScopedAttribute&lt;&gt;, TransientAttribute&lt;&gt;, DelegateAttribute&lt;&gt;).
    /// </summary>
    /// <param name="module"></param>
    /// <param name="attributeData"></param>
    /// <param name="lifetime"></param>
    /// <param name="creationTimeProvider"></param>
    /// <param name="getAccessorProvider"></param>
    [SetsRequiredMembers]
    public Service(INamedTypeSymbol module, AttributeData attributeData, ServiceLifetime lifetime, CreationTiming creationTimeProvider, GetAccess getAccessorProvider, ErrorManager errorManager) {
        Debug.Assert(attributeData.AttributeClass?.TypeKind != TypeKind.Error == true || attributeData.AttributeClass?.TypeArguments.All((ITypeSymbol typeSymbol) => typeSymbol.TypeKind != TypeKind.Error) == true);

        INamedTypeSymbol attributeType = attributeData.AttributeClass!;
        INamedTypeSymbol? serviceType;
        INamedTypeSymbol? implementationType;
        switch (attributeType.TypeArguments.Length) {
            case 2: // Service<TService, TImplementation>
                serviceType = attributeType.TypeArguments[0] as INamedTypeSymbol;
                implementationType = attributeType.TypeArguments[1] as INamedTypeSymbol;
                break;

            case 1: // Service<TService>
                serviceType = attributeType.TypeArguments[0] as INamedTypeSymbol;
                implementationType = serviceType;
                break;

            case 0: // Service(typeof(service)) || Service(typeof(service), typeof(implementation))
                switch (attributeData.ConstructorArguments.Length) {
                    case 2: // Service(typeof(service), typeof(implementation))
                        serviceType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
                        implementationType = attributeData.ConstructorArguments[1].Value as INamedTypeSymbol;
                        break;
                    case 1: // Service(typeof(service))
                        serviceType = attributeData.ConstructorArguments[0].Value as INamedTypeSymbol;
                        implementationType = serviceType;
                        break;
                    default: // syntax error input: 0 || >2
                        serviceType = null;
                        implementationType = null;
                        break;
                }
                break;

            default: // syntax error input: >2
                serviceType = null;
                implementationType = null;
                break;
        }
        if (serviceType is null || implementationType is null) {
            ServiceType = new TypeName(string.Empty);
            ImplementationType = new TypeName(string.Empty);
            Name = string.Empty;
            ConstructorDependencyList = [];
            PropertyDependencyList = [];
            Dependencies = [];
            return; // Syntax Error
        }

        Lifetime = lifetime;

        ServiceType = new TypeName(serviceType);
        IsRefable = lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped && serviceType.IsValueType == true && SymbolEqualityComparer.Default.Equals(serviceType, implementationType);
        ImplementationType = new TypeName(implementationType);

        // map unbound generic to open generic
        foreach (TypeName? argument in ServiceType.TypeArgumentList)
            if (argument is null) {
                IsGeneric = true;
                serviceType = serviceType.ConstructedFrom;
                implementationType = implementationType.ConstructedFrom;

                if (ServiceType.TypeArgumentList.Count != ImplementationType.TypeArgumentList.Count)
                    errorManager.AddServiceRegistrationTypeParameterMismatchError(ServiceType, ImplementationType);
                else
                    for (int i = 0; i < ServiceType.TypeArgumentList.Count; i++)
                        if (ImplementationType.TypeArgumentList[i] is not null && ImplementationType.TypeArgumentList[i] != ServiceType.TypeArgumentList[i])
                            errorManager.AddServiceRegistrationTypeParameterMismatchError(ServiceType, ImplementationType);

                break;
            }

        Name = implementationType.Name;
        CreationTime = creationTimeProvider;
        GetAccessor = getAccessorProvider;
        string? implementationName = null;
        bool noDispose = false;
        if (attributeData.NamedArguments.Length > 0) {
            if (attributeData.NamedArguments.GetArgument<string>("Name") is string name)
                Name = name;
            if (attributeData.NamedArguments.GetArgument<int?>("CreationTime") is int creationTime)
                CreationTime = (CreationTiming)creationTime;
            if (attributeData.NamedArguments.GetArgument<int?>("GetAccessor") is int getAccessor)
                GetAccessor = (GetAccess)getAccessor;
            implementationName = attributeData.NamedArguments.GetArgument<string>("Implementation");
            noDispose = attributeData.NamedArguments.GetArgument<bool>("NoDispose");
        }
        CreationTimeTransitive = CreationTime;

        // Implementation, ConstructorDependencyList, PropertyDependencyList
        if (implementationName is null) {
            Implementation = default;
            ConstructorDependencyList = implementationType.CreateConstructorDependencyList(errorManager) ?? [];
            PropertyDependencyList = implementationType.CreatePropertyDependencyList(errorManager) ?? [];
        }
        else {
            if (implementationName == "this") {
                Implementation = new ImplementationMember(MemberType.Field, "this", IsStatic: false, IsScoped: Lifetime == ServiceLifetime.Scoped);
                ConstructorDependencyList = [];

                // check implementation type
                switch (Lifetime) {
                    case ServiceLifetime.Singleton:
                        if (!SymbolEqualityComparer.Default.Equals(module, implementationType))
                            errorManager.AddWrongFieldImplementationTypeError("this", module.ToDisplayString(), ImplementationType);
                        break;
                    case ServiceLifetime.Scoped:
                        if ($"{module.ToDisplayString()}.Scope" != implementationType.ToDisplayString())
                            errorManager.AddWrongFieldImplementationTypeError("this", $"{module.ToDisplayString()}.Scope", ImplementationType);
                        break;
                    case ServiceLifetime.Transient:
                        errorManager.AddTransientImplementationThisError();
                        break;
                }
            }
            else {
                ISymbol? implementationSymbol;
                switch (Lifetime) {
                    case ServiceLifetime.Singleton:
                        (Implementation, implementationSymbol, ConstructorDependencyList) = GetImplementation(module, implementationName, isScoped: false);
                        break;
                    case ServiceLifetime.Scoped: {
                        if (module.GetMembers("Scope") is [INamedTypeSymbol scopeProvider, ..]) {
                            (Implementation, implementationSymbol, ConstructorDependencyList) = GetImplementation(scopeProvider, implementationName, isScoped: true);

                            if (Implementation.Type == MemberType.None)
                                (Implementation, implementationSymbol, ConstructorDependencyList) = GetImplementation(module, implementationName, isScoped: false);
                        }
                        else
                            (Implementation, implementationSymbol, ConstructorDependencyList) = GetImplementation(module, implementationName, isScoped: false);
                        break;
                    }
                    case ServiceLifetime.Transient: {
                        (Implementation, implementationSymbol, ConstructorDependencyList) = GetImplementation(module, implementationName, isScoped: false);

                        if (Implementation.Type == MemberType.None) {
                            Lifetime = ServiceLifetime.TransientScoped;
                            if (module.GetMembers("Scope") is [INamedTypeSymbol scopeProvider, ..])
                                (Implementation, implementationSymbol, ConstructorDependencyList) = GetImplementation(scopeProvider, implementationName, isScoped: true);
                        }

                        if (Implementation.Type == MemberType.Field)
                            errorManager.AddTransientImplementationFieldError();
                        break;
                    }
                    default: {
                        throw new Exception($"Invalid enum LifeTime: '{Lifetime}', only the defined values are allowed.");
                    }

                    static (ImplementationMember implementation, ISymbol? implementationSymbol, List<ConstructorDependency> constructorDependencyList) GetImplementation(INamedTypeSymbol provider, string implementationName, bool isScoped)
                        => provider.GetMembers(implementationName) switch {
                            [IFieldSymbol field, ..] => (new ImplementationMember(MemberType.Field, field.Name, field.IsStatic, isScoped), field, []),
                            [IPropertySymbol property, ..] => (new ImplementationMember(MemberType.Property, property.Name, property.IsStatic, isScoped), property, []),
                            [IMethodSymbol method, ..] => (new ImplementationMember(MemberType.Method, method.Name, method.IsStatic, isScoped), method, method.CreateConstructorDependencyList()),
                            _ => (default, null, [])
                        };
                }

                // check implementation
                switch (implementationSymbol) {
                    case null:
                        errorManager.AddMissingImplementationMemberError(module.ToDisplayString(), implementationName);
                        break;
                    case IFieldSymbol field:
                        if (!SymbolEqualityComparer.Default.Equals(field.Type, implementationType))
                            if (!SymbolEqualityComparer.Default.Equals((field.Type as INamedTypeSymbol)?.ConstructedFrom, implementationType))
                                errorManager.AddWrongFieldImplementationTypeError(implementationName, field.Type.ToDisplayString(), ImplementationType);

                        if (implementationType.Arity > 0)
                            errorManager.AddImplementationGenericError(implementationName, ImplementationType, implementationType.Arity);
                        break;
                    case IPropertySymbol property:
                        if (!SymbolEqualityComparer.Default.Equals(property.Type, implementationType))
                            if (!SymbolEqualityComparer.Default.Equals((property.Type as INamedTypeSymbol)?.ConstructedFrom, implementationType))
                                errorManager.AddWrongPropertyImplementationTypeError(implementationName, property.Type.ToDisplayString(), ImplementationType);

                        if (implementationType.Arity > 0)
                            errorManager.AddImplementationGenericError(implementationName, ImplementationType, implementationType.Arity);
                        break;
                    case IMethodSymbol method: {
                        if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, implementationType))
                            if (!SymbolEqualityComparer.Default.Equals((method.ReturnType as INamedTypeSymbol)?.ConstructedFrom, implementationType))
                                errorManager.AddWrongMethodImplementationTypeError(implementationName, method.ReturnType.ToDisplayString(), ImplementationType);

                        if (method.Arity != implementationType.Arity)
                            errorManager.AddMethodImplementationTypeParameterMismatchError(implementationName, method.Arity, implementationType.Arity);
                        break;
                    }
                }
            }

            PropertyDependencyList = [];
        }

        IsDisposable = !noDispose && implementationType.HasInterface("IDisposable");
        IsAsyncDisposable = !noDispose && implementationType.HasInterface("IAsyncDisposable");

        Dependencies = DependenciesDefaultIterator;
    }

    /// <summary>
    /// Creates a data-object based on a delegate service attribute (DelegateAttribute&lt;&gt;).
    /// </summary>
    /// <param name="module"></param>
    /// <param name="attributeData"></param>
    /// <param name="getAccessorProvider"></param>
    [SetsRequiredMembers]
    public Service(INamedTypeSymbol module, AttributeData attributeData, GetAccess getAccessorProvider, ErrorManager errorManager) {
        Debug.Assert(attributeData.AttributeClass?.TypeArguments.All((ITypeSymbol typeSymbol) => typeSymbol.TypeKind != TypeKind.Error) == true);

        INamedTypeSymbol attributeType = attributeData.AttributeClass!;
        INamedTypeSymbol? serviceType = attributeType.TypeArguments.Length switch {
            1 /*Delegate<TService>("method")*/ => attributeType.TypeArguments[0] as INamedTypeSymbol,
            0 /*Delegate(typeof(service), "method")*/ => attributeData.ConstructorArguments.Length switch {
                2 => attributeData.ConstructorArguments[0].Value as INamedTypeSymbol,
                _ => null
            },
            _ => null
        };
        if (serviceType is null) {
            ServiceType = new TypeName(string.Empty);
            ImplementationType = new TypeName(string.Empty);
            Name = string.Empty;
            ConstructorDependencyList = [];
            PropertyDependencyList = [];
            Dependencies = [];
            return; // Syntax Error
        }

        ServiceType = new TypeName(serviceType);
        ImplementationType = ServiceType;
        IsGeneric = false;
        foreach (TypeName? argumant in ServiceType.TypeArgumentList)
            if (argumant is null) {
                IsGeneric = true;
                serviceType = serviceType.ConstructedFrom;
            }

        Name = attributeData.NamedArguments.GetArgument<string>("Name") ?? serviceType.Name.Replace(".", "");
        CreationTime = CreationTiming.Constructor;
        CreationTimeTransitive = CreationTiming.Constructor;
        GetAccessor = (GetAccess?)attributeData.NamedArguments.GetArgument<int?>("GetAccessor") ?? getAccessorProvider;

        ConstructorDependencyList = [];
        PropertyDependencyList = [];
        Dependencies = DependenciesDefaultIterator;
        // Has no dependencies, so it is already dependency tree initialized
        TreeState = (DependencyTreeFlags.New, DependencyTreeFlags.New);


        // check serviceType is delegate
        if (serviceType.DelegateInvokeMethod is null) {
            errorManager.AddDelegateServiceIsNotDelegateError(ServiceType);
            return;
        }


        // implementation method
        if (attributeData.ConstructorArguments is not [.., TypedConstant { Value: string methodName }]) {
            Implementation = new ImplementationMember(MemberType.Method, string.Empty, IsStatic: false, IsScoped: false);
            errorManager.AddMissingDelegateImplementationError(module.ToDisplayString(), string.Empty);
            return;
        }

        IMethodSymbol implementation;
        if (module.GetMembers(methodName) is [IMethodSymbol method, ..]) {
            implementation = method;
            Implementation = new ImplementationMember(MemberType.Method, methodName, method.IsStatic, IsScoped: false);
            Lifetime = ServiceLifetime.Delegate;
        }
        else if (module.GetMembers("Scope") is [INamedTypeSymbol scopeProvider, ..] && scopeProvider.GetMembers(methodName) is [IMethodSymbol scopeMethod, ..]) {
            implementation = scopeMethod;
            Implementation = new ImplementationMember(MemberType.Method, methodName, scopeMethod.IsStatic, IsScoped: true);
            Lifetime = ServiceLifetime.DelegateScoped;
        }
        else {
            Implementation = new ImplementationMember(MemberType.Method, string.Empty, IsStatic: false, IsScoped: false);
            errorManager.AddMissingDelegateImplementationError(module.ToDisplayString(), methodName);
            return;
        }

        if (implementation.Parameters.Length != serviceType.DelegateInvokeMethod.Parameters.Length)
            errorManager.AddDelegateWrongParameterCountError(methodName, implementation.Parameters.Length, serviceType.DelegateInvokeMethod.Parameters.Length);
        else
            for (int i = 0; i < implementation.Parameters.Length; i++)
                if (!SymbolEqualityComparer.Default.Equals(implementation.Parameters[i].Type, serviceType.DelegateInvokeMethod.Parameters[i].Type))
                    if (!SymbolEqualityComparer.Default.Equals((implementation.Parameters[i].Type as INamedTypeSymbol)?.ConstructedFrom, serviceType.DelegateInvokeMethod.Parameters[i].Type))
                        if ((implementation.Parameters[i].Type as ITypeParameterSymbol)?.Name != serviceType.DelegateInvokeMethod.Parameters[i].Type.Name)
                            errorManager.AddDelegateWrongParameterTypeError(methodName, implementation.Parameters[i].Type.ToDisplayString(), serviceType.DelegateInvokeMethod.Parameters[i].Type.ToDisplayString(), i + 1);

        if (!SymbolEqualityComparer.Default.Equals(implementation.ReturnType, serviceType.DelegateInvokeMethod.ReturnType))
            if (!SymbolEqualityComparer.Default.Equals((implementation.ReturnType as INamedTypeSymbol)?.ConstructedFrom, serviceType.DelegateInvokeMethod.ReturnType))
                if ((implementation.ReturnType as ITypeParameterSymbol)?.Name != serviceType.DelegateInvokeMethod.ReturnType.Name)
                    errorManager.AddDelegateWrongReturnTypeError(methodName, implementation.ReturnType.ToDisplayString(), serviceType.DelegateInvokeMethod!.ReturnType.ToDisplayString());

        if (implementation.Arity != serviceType.Arity)
            errorManager.AddDelegateTypeParameterMismatchError(methodName, implementation.Arity, serviceType.Arity);
    }


    /// <summary>
    /// Creates closed service out of a generic service
    /// </summary>
    /// <param name="genericService">open/unbound service</param>
    /// <param name="serviceWithArgumentList">Contains the TypeArgumentList to fill the open/unbound type parameters</param>
    /// <returns>A non generic service</returns>
    public static Service CreateClosedService(Service genericService, TypeName serviceWithArgumentList) {
        TypeName implementationType = new(
            genericService.ImplementationType.Name,
            genericService.ImplementationType.Keyword,
            genericService.ImplementationType.NameSpaceList,
            genericService.ImplementationType.ContainingTypeList,
            genericService.ImplementationType.TypeParameterList,
            serviceWithArgumentList.TypeArgumentList);

        string name; // "{Name}_{generic1}_{generic2}.._{genericN}
        {
            StringBuilder builder = new(genericService.Name.Length + 8 * serviceWithArgumentList.TypeArgumentList.Count);

            builder.Append(genericService.Name);

            AppendTypeArguments(builder, serviceWithArgumentList.TypeArgumentList!);
            static void AppendTypeArguments(StringBuilder builder, List<TypeName> typeArgumentList) {
                foreach (TypeName typeName in typeArgumentList) {
                    builder.AppendInterpolation($"_{typeName.Name}");
                    AppendTypeArguments(builder, typeName.TypeArgumentList!);
                }
            }

            name = builder.ToString();
        }

        List<ConstructorDependency> constructorDependencyList = CreateDependencyList(genericService.ConstructorDependencyList, implementationType);
        List<PropertyDependency> propertyDependencyList = CreateDependencyList(genericService.PropertyDependencyList, implementationType);

        return new Service() {
            Lifetime = genericService.Lifetime,
            IsRefable = genericService.IsRefable,
            IsGeneric = genericService.IsGeneric,
            Implementation = genericService.Implementation,
            ImportMode = genericService.ImportMode,
            Module = genericService.Module,
            CreationTime = genericService.CreationTime,
            CreationTimeTransitive = genericService.CreationTimeTransitive,
            GetAccessor = genericService.GetAccessor,
            IsDisposable = genericService.IsDisposable,
            IsAsyncDisposable = genericService.IsAsyncDisposable,

            Name = name,
            ServiceType = serviceWithArgumentList,
            ImplementationType = implementationType,

            ConstructorDependencyList = constructorDependencyList,
            PropertyDependencyList = propertyDependencyList,
            Dependencies = constructorDependencyList.Concat<Dependency>(propertyDependencyList)
        };


        static List<T> CreateDependencyList<T>(List<T> dependencyList, TypeName implementationType) where T : Dependency {
            List<T> result = dependencyList;
            bool newListAllocated = false;

            for (int i = 0; i < dependencyList.Count; i++) {
                // check if argumentlist is different
                TypeName? dependencyServiceType = null;
                if (dependencyList[i].ServiceType is TypeName parameterServiceType)
                    for (int j = 0; j < parameterServiceType.TypeArgumentList.Count; j++)
                        if (parameterServiceType.TypeArgumentList[j] is null) {
                            List<TypeName?> parameterTypeArgumentList = new(parameterServiceType.TypeArgumentList.Count);
                            for (int jj = 0; jj < j; jj++)
                                parameterTypeArgumentList.Add(parameterServiceType.TypeArgumentList[j]);

                            do {
                                if (parameterServiceType.TypeArgumentList[j] is null) {
                                    string typeParameter = parameterServiceType.TypeParameterList[j];
                                    for (int k = 0; k < implementationType.TypeParameterList.Count; k++)
                                        if (implementationType.TypeParameterList[k] == typeParameter) {
                                            parameterTypeArgumentList.Add(implementationType.TypeArgumentList[k]);
                                            goto _break;
                                        }
                                    // else
                                    {
                                        // not defined typeparameter => syntax error
                                        parameterTypeArgumentList.Add(null);
                                    }
                                    _break:;
                                }
                            } while (++j < implementationType.TypeArgumentList.Count);

                            dependencyServiceType = new TypeName(
                                parameterServiceType.Name,
                                parameterServiceType.Keyword,
                                parameterServiceType.NameSpaceList,
                                parameterServiceType.ContainingTypeList,
                                parameterServiceType.TypeParameterList,
                                parameterTypeArgumentList);
                            break;
                        }

                // check if implementationBase is different
                TypeName? implementationBaseName = null;
                if (typeof(T) == typeof(PropertyDependency)) {
                    PropertyDependency propertyDependency = Unsafe.As<PropertyDependency>(dependencyList[i]);
                    TypeName? baseImplementation = propertyDependency.ImplementationBaseName;
                    List<TypeName?> baseArgumentList = baseImplementation.TypeArgumentList;

                    for (int j = 0; j < baseArgumentList.Count; j++)
                        if (baseArgumentList[j] is null) {
                            List<TypeName?> filledArgumentList = new(baseArgumentList.Count);
                            for (int jj = 0; jj < j; jj++)
                                filledArgumentList.Add(baseArgumentList[j]);

                            do {
                                if (baseArgumentList[j] is null) {
                                    string typeParameter = baseImplementation.TypeParameterList[j];
                                    for (int k = 0; k < implementationType.TypeParameterList.Count; k++)
                                        if (implementationType.TypeParameterList[k] == typeParameter) {
                                            filledArgumentList.Add(implementationType.TypeArgumentList[k]);
                                            goto _break;
                                        }
                                    // else
                                    {
                                        // not defined typeparameter => syntax error
                                        filledArgumentList.Add(null);
                                    }
                                    _break:;
                                }
                            } while (++j < baseImplementation.TypeArgumentList.Count);

                            implementationBaseName = new TypeName(
                                baseImplementation.Name,
                                baseImplementation.Keyword,
                                baseImplementation.NameSpaceList,
                                baseImplementation.ContainingTypeList,
                                baseImplementation.TypeParameterList,
                                filledArgumentList
                                );
                        }
                }

                if (dependencyServiceType is not null || implementationBaseName is not null) {
                    // something is not equal, allocate a new list to make a copy
                    if (!newListAllocated) {
                        result = new(dependencyList.Count);
                        for (int ii = 0; ii < i; ii++)
                            result.Add(dependencyList[ii]);
                        newListAllocated = true;
                    }
                    result.Add(dependencyList[i]);

                    if (typeof(T) == typeof(ConstructorDependency)) {
                        ConstructorDependency constructorDependency = Unsafe.As<ConstructorDependency>(dependencyList[i]);
                        result[i] = Unsafe.As<T>(new ConstructorDependency() {
                            Name = constructorDependency.Name,
                            ServiceName = constructorDependency.ServiceName,
                            ServiceType = dependencyServiceType ?? constructorDependency.ServiceType,
                            HasAttribute = constructorDependency.HasAttribute,
                            ByRef = constructorDependency.ByRef
                        });
                    }
                    else if (typeof(T) == typeof(PropertyDependency)) {
                        PropertyDependency propertyDependency = Unsafe.As<PropertyDependency>(dependencyList[i]);
                        result[i] = Unsafe.As<T>(new PropertyDependency() {
                            Name = propertyDependency.Name,
                            ServiceName = propertyDependency.ServiceName,
                            ServiceType = dependencyServiceType ?? propertyDependency.ServiceType,
                            HasAttribute = propertyDependency.HasAttribute,
                            IsInit = propertyDependency.IsInit,
                            IsRequired = propertyDependency.IsRequired,
                            ImplementationBaseName = implementationBaseName ?? propertyDependency.ImplementationBaseName
                        });
                    }
                }
                else
                    if (newListAllocated)
                        result.Add(dependencyList[i]);
            }

            return result;
        }
    }


    #region Equals

    public static bool operator ==(Service? left, Service? right)
        => (left, right) switch {
            (null, null) => true,
            (null, not null) => false,
            (not null, _) => left.Equals(right)
        };

    public static bool operator !=(Service? left, Service? right) => !(left == right);

    public override bool Equals(object? obj) => Equals(obj as Service);

    public bool Equals(Service? other) {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        if (Lifetime != other.Lifetime)
            // treat Transient, TransientSingleton and TransientScope the same
            if (!Lifetime.HasFlag(ServiceLifetime.Transient) || !other.Lifetime.HasFlag(ServiceLifetime.Transient))
                return false;

        if (ServiceType != other.ServiceType)
            return false;
        if (IsRefable != other.IsRefable)
            return false;
        if (IsGeneric != other.IsGeneric)
            return false;
        if (ImplementationType != other.ImplementationType)
            return false;
        if (Implementation != other.Implementation)
            return false;

        if (ImportMode != other.ImportMode)
            return false;
        if (Module != other.Module)
            return false;

        if (Name != other.Name)
            return false;
        if (CreationTime != other.CreationTime)
            return false;
        if (GetAccessor != other.GetAccessor)
            return false;

        if (!ConstructorDependencyList.SequenceEqual(other.ConstructorDependencyList))
            return false;
        if (!PropertyDependencyList.SequenceEqual(other.PropertyDependencyList))
            return false;

        if (IsDisposable != other.IsDisposable)
            return false;
        if (IsAsyncDisposable != other.IsAsyncDisposable)
            return false;

        return true;
    }

    public override int GetHashCode() {
        int hashCode = Lifetime.HasFlag(ServiceLifetime.Transient) switch {
            true => ServiceLifetime.Transient.GetHashCode(),
            false => Lifetime.GetHashCode()
        };

        hashCode = Combine(hashCode, ServiceType.GetHashCode());
        hashCode = Combine(hashCode, IsRefable.GetHashCode());
        hashCode = Combine(hashCode, IsGeneric.GetHashCode());
        hashCode = Combine(hashCode, ImplementationType.GetHashCode());
        hashCode = Combine(hashCode, Implementation.GetHashCode());

        hashCode = Combine(hashCode, ImportMode.GetHashCode());
        hashCode = Combine(hashCode, Module?.GetHashCode() ?? 0);

        hashCode = Combine(hashCode, Name.GetHashCode());
        hashCode = Combine(hashCode, CreationTime.GetHashCode());
        hashCode = Combine(hashCode, GetAccessor.GetHashCode());

        hashCode = CombineList(hashCode, ConstructorDependencyList);
        hashCode = CombineList(hashCode, PropertyDependencyList);

        hashCode = Combine(hashCode, IsDisposable.GetHashCode());
        hashCode = Combine(hashCode, IsAsyncDisposable.GetHashCode());

        return hashCode;


        static int CombineList(int hashCode, IEnumerable<object> list) {
            foreach (object item in list)
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
