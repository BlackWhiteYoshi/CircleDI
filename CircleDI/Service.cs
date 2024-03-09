using Microsoft.CodeAnalysis;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CircleDI;

/// <summary>
/// Contains all necessary information to source generate a service.<br />
/// It includes the information about the attribute (lifetime, type parameters and arguments) as well as the necessary information about the implementation class.
/// </summary>
public sealed class Service : IEquatable<Service> {
    /// <summary>
    /// Lifetime type of the service.<br />
    /// Additionally the service is also located in the corresponding list. See <see cref="ServiceProvider.SingletonList"/>, <see cref="ServiceProvider.ScopedList"/>, <see cref="ServiceProvider.TransientList"/>.
    /// </summary>
    public required ServiceLifetime Lifetime { get; set; } = ServiceLifetime.Singleton;

    /// <summary>
    /// <para>The type of the service to interact with. Often it is an interface.</para>
    /// <para>If only 1 TypeArgument in the register attribute is present, then ServiceType and ImplementationType is the same.</para>
    /// </summary>
    public required string ServiceType { get; init; }

    /// <summary>
    /// <para>
    /// The number of type parameters of ServiceType.<br />
    /// A non-generic type has zero arity.
    /// </para>
    /// <para>e.g. Dictionary&lt;string, int&gt; has arity 2</para>
    /// </summary>
    public int ServiceTypeArity { get; init; } = 0;

    /// <summary>
    /// <para>The type of the actual object that will be instatiated.</para>
    /// <para>If only 1 TypeArgument in the register attribute is present, then ServiceType and ImplementationType is the same.</para>
    /// </summary>
    public required string ImplementationType { get; init; }

    /// <summary>
    /// <para>Information about a custom implementation for retrieving an object for the Service.</para>
    /// </summary>
    public ImplementationMember Implementation { get; init; }

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
    public required ConstructorDependency[] ConstructorDependencyList { get; init; }

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
    /// Diagnostics with Severity error that are related to this Service.
    /// </summary>
    public List<Diagnostic>? ErrorList { get; private set; }


    /// <summary>
    /// <para>Iterator for all dependencies of this service.</para>
    /// <para>Normally it is just <see cref="ConstructorDependencyList"/> concatenated with <see cref="PropertyDependencyList"/>.</para>
    /// <para>The exception is the "special" service <see cref="ServiceProvider.CreateScope"/>, it needs an iterator with filter.</para>
    /// </summary>
    public required IEnumerable<Dependency> Dependencies { get; init; }
    private IEnumerable<Dependency> DependenciesDefaultIterator => ConstructorDependencyList.Concat<Dependency>(PropertyDependencyList);


    /// <summary>
    /// Some space for flags to create, process or consume the dependency tree.
    /// </summary>
    public DependencyTreeFlags TreeState { get; set; } = DependencyTreeFlags.New;



    /// <summary>
    /// <para>Creates a data-object that represents a service attribute (SingletonAttribute&lt;&gt;, ScopedAttribute&lt;&gt;, TransientAttribute&lt;&gt;, DelegateAttribute&lt;&gt;).</para>
    /// </summary>
    public Service() { }

    /// <summary>
    /// Creates a data-object based on a service attribute (SingletonAttribute&lt;&gt;, ScopedAttribute&lt;&gt;, TransientAttribute&lt;&gt;, DelegateAttribute&lt;&gt;).
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="attributeData"></param>
    /// <param name="lifetime"></param>
    /// <param name="creationTimeProvider"></param>
    /// <param name="getAccessorProvider"></param>
    [SetsRequiredMembers]
    public Service(INamedTypeSymbol serviceProvider, AttributeData attributeData, ServiceLifetime lifetime, CreationTiming creationTimeProvider, GetAccess getAccessorProvider) {
        Debug.Assert(attributeData.AttributeClass?.TypeArguments.Length > 0);
        
        INamedTypeSymbol attributeType = attributeData.AttributeClass!;
        INamedTypeSymbol serviceType = (INamedTypeSymbol)attributeType.TypeArguments[0];
        INamedTypeSymbol implementationType = attributeType.TypeArguments.Length switch {
            >= 2 => (INamedTypeSymbol)attributeType.TypeArguments[1],
            _ => serviceType
        };
        
        Lifetime = lifetime;
        ServiceType = serviceType.ToFullQualifiedName();
        ServiceTypeArity = serviceType.Arity;
        ImplementationType = implementationType.ToFullQualifiedName();
        Name = attributeData.NamedArguments.GetArgument<string>("Name") ?? implementationType.Name.Replace('.', '_');
        CreationTime = (CreationTiming?)attributeData.NamedArguments.GetArgument<int?>("CreationTime") ?? creationTimeProvider;
        GetAccessor = (GetAccess?)attributeData.NamedArguments.GetArgument<int?>("GetAccessor") ?? getAccessorProvider;

        // Implementation, ConstructorDependencyList, PropertyDependencyList
        if (attributeData.NamedArguments.GetArgument<string>("Implementation") is not string implementationName) {
            Implementation = default;
            
            (IMethodSymbol? constructor, Diagnostic? constructorListError) = FindConstructor(implementationType, attributeData);
            if (constructor != null)
                ConstructorDependencyList = constructor!.CreateConstructorDependencyList();
            else {
                ConstructorDependencyList = [];
                ErrorList ??= [];
                ErrorList.Add(constructorListError!);
            }

            (PropertyDependencyList, Diagnostic? propertyListError) = CreatePropertyDependencyList(implementationType, attributeData);
            if (propertyListError != null) {
                ErrorList ??= [];
                ErrorList.Add(propertyListError);
            }
        }
        else {
            if (implementationName == "this") {
                Implementation = new ImplementationMember(MemberType.Field, implementationName, IsStatic: false, IsScoped: Lifetime == ServiceLifetime.Scoped);
                ConstructorDependencyList = [];

                // check implementation type
                switch (Lifetime) {
                    case ServiceLifetime.Singleton: {
                        if (!SymbolEqualityComparer.Default.Equals(serviceProvider, implementationType)) {
                            Diagnostic error = attributeData.CreateWrongFieldImplementationTypeError(implementationName, serviceProvider.ToDisplayString(), ImplementationType);
                            ErrorList ??= [];
                            ErrorList.Add(error);
                        }
                        break;
                    }
                    case ServiceLifetime.Scoped: {
                        if ($"{serviceProvider.ToDisplayString()}.Scope" != implementationType.ToDisplayString()) {
                            Diagnostic error = attributeData.CreateWrongFieldImplementationTypeError(implementationName, $"{serviceProvider.ToDisplayString()}.Scope", ImplementationType);
                            ErrorList ??= [];
                            ErrorList.Add(error);
                        }
                        break;
                    }
                    case ServiceLifetime.Transient: {
                        Diagnostic error = attributeData.CreateTransientImplementationThisError();
                        ErrorList ??= [];
                        ErrorList.Add(error);
                        break;
                    }
                }
            }
            else {
                ISymbol? implementationSymbol;
                switch (Lifetime) {
                    case ServiceLifetime.Singleton:
                        (Implementation, implementationSymbol, ConstructorDependencyList) = GetImplementation(serviceProvider, implementationName, isScoped: false);
                        break;
                    case ServiceLifetime.Scoped: {
                        if (serviceProvider.GetMembers("Scope") is [INamedTypeSymbol scopeProvider, ..]) {
                            (Implementation, implementationSymbol, ConstructorDependencyList) = GetImplementation(scopeProvider, implementationName, isScoped: true);

                            if (Implementation.Type == MemberType.None)
                                (Implementation, implementationSymbol, ConstructorDependencyList) = GetImplementation(serviceProvider, implementationName, isScoped: false);
                        }
                        else
                            (Implementation, implementationSymbol, ConstructorDependencyList) = GetImplementation(serviceProvider, implementationName, isScoped: false);
                        break;
                    }
                    case ServiceLifetime.Transient: {
                        (Implementation, implementationSymbol, ConstructorDependencyList) = GetImplementation(serviceProvider, implementationName, isScoped: false);

                        if (Implementation.Type == MemberType.None) {
                            Lifetime = ServiceLifetime.TransientScoped;
                            if (serviceProvider.GetMembers("Scope") is [INamedTypeSymbol scopeProvider, ..])
                                (Implementation, implementationSymbol, ConstructorDependencyList) = GetImplementation(scopeProvider, implementationName, isScoped: true);
                        }

                        if (Implementation.Type == MemberType.Field) {
                            Diagnostic error = attributeData.CreateTransientImplementationFieldError();
                            ErrorList ??= [];
                            ErrorList.Add(error);
                        }
                        break;
                    }
                    default: {
                        throw new Exception($"Invalid enum LifeTime: '{Lifetime}', only the defined values are allowed.");
                    }

                    static (ImplementationMember implementation, ISymbol? implementationSymbol, ConstructorDependency[] constructorDependencyList) GetImplementation(INamedTypeSymbol provider, string implementationName, bool isScoped)
                        => provider.GetMembers(implementationName) switch {
                            [IFieldSymbol field, ..] => (new ImplementationMember(MemberType.Field, field.Name, field.IsStatic, isScoped), field, []),
                            [IPropertySymbol property, ..] => (new ImplementationMember(MemberType.Property, property.Name, property.IsStatic, isScoped), property, []),
                            [IMethodSymbol method, ..] => (new ImplementationMember(MemberType.Method, method.Name, method.IsStatic, isScoped), method, method.CreateConstructorDependencyList()),
                            _ => (default, null, [])
                        };
                }

                // check implementation
                switch (implementationSymbol) {
                    case null: {
                        Diagnostic error = attributeData.CreateMissingImplementationMemberError(serviceProvider.ToDisplayString(), implementationName);
                        ErrorList ??= [];
                        ErrorList.Add(error);
                        break;
                    }
                    case IFieldSymbol field: {
                        if (!SymbolEqualityComparer.Default.Equals(field.Type, implementationType)) {
                            Diagnostic error = attributeData.CreateWrongFieldImplementationTypeError(implementationName, field.Type.ToDisplayString(), ImplementationType);
                            ErrorList ??= [];
                            ErrorList.Add(error);
                        }
                        break;
                    }
                    case IPropertySymbol property: {
                        if (!SymbolEqualityComparer.Default.Equals(property.Type, implementationType)) {
                            Diagnostic error = attributeData.CreateWrongPropertyImplementationTypeError(implementationName, property.Type.ToDisplayString(), ImplementationType);
                            ErrorList ??= [];
                            ErrorList.Add(error);
                        }
                        break;
                    }
                    case IMethodSymbol method: {
                        if (!SymbolEqualityComparer.Default.Equals(method.ReturnType, implementationType)) {
                            Diagnostic error = attributeData.CreateWrongMethodImplementationTypeError(implementationName, method.ReturnType.ToDisplayString(), ImplementationType);
                            ErrorList ??= [];
                            ErrorList.Add(error);
                        }
                        break;
                    }
                }
            }

            PropertyDependencyList = [];
        }
        Dependencies = DependenciesDefaultIterator;

        IsDisposable = implementationType.HasInterface("IDisposable");
        IsAsyncDisposable = implementationType.HasInterface("IAsyncDisposable");
    }

    /// <summary>
    /// Creates a data-object based on a delegate service attribute (DelegateAttribute&lt;&gt;).
    /// </summary>
    /// <param name="serviceProvider"></param>
    /// <param name="attributeData"></param>
    /// <param name="getAccessorProvider"></param>
    [SetsRequiredMembers]
    public Service(INamedTypeSymbol serviceProvider, AttributeData attributeData, GetAccess getAccessorProvider) {
        Debug.Assert(attributeData.AttributeClass?.TypeArguments.Length > 0);
        Debug.Assert(attributeData.ConstructorArguments.Length > 0);

        INamedTypeSymbol attribute = attributeData.AttributeClass!;
        INamedTypeSymbol serviceType = (INamedTypeSymbol)attribute.TypeArguments[0];

        ServiceType = serviceType.ToFullQualifiedName();
        ImplementationType = ServiceType;

        Name = attributeData.NamedArguments.GetArgument<string>("Name") ?? serviceType.Name.Replace(".", "");
        Lifetime = ServiceLifetime.Delegate;
        CreationTime = CreationTiming.Constructor;
        GetAccessor = (GetAccess?)attributeData.NamedArguments.GetArgument<int?>("GetAccessor") ?? getAccessorProvider;

        ConstructorDependencyList = [];
        PropertyDependencyList = [];
        Dependencies = DependenciesDefaultIterator;
        // Has no dependencies, so it is already dependency tree initialized
        TreeState = DependencyTreeFlags.Traversed;


        // check serviceType is delegate
        if (serviceType.DelegateInvokeMethod == null) {
            Diagnostic error = attributeData.CreateDelegateServiceIsNotDelegateError(ServiceType);
            ErrorList ??= [];
            ErrorList.Add(error);
            return;
        }


        // implementation method
        if (attributeData.ConstructorArguments[0].Value is string methodName) {
            IMethodSymbol implementation;
            if (serviceProvider.GetMembers(methodName) is [IMethodSymbol method, ..]) {
                implementation = method;
                Implementation = new ImplementationMember(MemberType.Method, methodName, method.IsStatic, IsScoped: false);
            }
            else if (serviceProvider.GetMembers("Scope") is [INamedTypeSymbol scopeProvider, ..] && scopeProvider.GetMembers(methodName) is [IMethodSymbol scopeMethod, ..]) {
                implementation = scopeMethod;
                Implementation = new ImplementationMember(MemberType.Method, methodName, scopeMethod.IsStatic, IsScoped: true);
            }
            else {
                Diagnostic error = attributeData.CreateMissingDelegateImplementationError(serviceProvider.ToDisplayString(), methodName);
                ErrorList ??= [];
                ErrorList.Add(error);
                return;
            }

            if (implementation.Parameters.Length != serviceType.DelegateInvokeMethod.Parameters.Length) {
                Diagnostic error = attributeData.CreateDelegateWrongParameterCountError(methodName, implementation.Parameters.Length, serviceType.DelegateInvokeMethod.Parameters.Length);
                ErrorList ??= [];
                ErrorList.Add(error);
            }
            else
                for (int i = 0; i < implementation.Parameters.Length; i++)
                    if (!SymbolEqualityComparer.Default.Equals(implementation.Parameters[i].Type, serviceType.DelegateInvokeMethod.Parameters[i].Type)) {
                        Diagnostic error = attributeData.CreateDelegateWrongParameterTypeError(methodName, implementation.Parameters[i].Type.ToDisplayString(), serviceType.DelegateInvokeMethod.Parameters[i].Type.ToDisplayString(), i + 1);
                        ErrorList ??= [];
                        ErrorList.Add(error);
                    }

            if (!SymbolEqualityComparer.Default.Equals(implementation.ReturnType, serviceType.DelegateInvokeMethod?.ReturnType)) {
                Diagnostic error = attributeData.CreateDelegateWrongReturnTypeError(methodName, implementation.ReturnType.ToDisplayString(), serviceType.DelegateInvokeMethod!.ReturnType.ToDisplayString());
                ErrorList ??= [];
                ErrorList.Add(error);
            }
        }
    }


    /// <summary>
    /// Creates the ConstructorDependencyList by analyzing the available constructors at the given class/implementation.<br />
    /// When an applicable constructor is found, the list will be created based on that constructor, otherwise an error will be created and the list will be empty.
    /// </summary>
    /// <param name="implementation"></param>
    /// <param name="attributeData"></param>
    /// <returns></returns>
    public static (IMethodSymbol? constructor, Diagnostic? error) FindConstructor(INamedTypeSymbol implementation, AttributeData attributeData) {
        switch (implementation.InstanceConstructors.Length) {
            case 0:
                return (null, attributeData.CreateMissingClassOrConstructorError(implementation.ToDisplayString()));
            case 1:
                if (implementation.InstanceConstructors[0].DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
                    return (null, attributeData.CreateMissingClassOrConstructorError(implementation.ToDisplayString()));
                return (implementation.InstanceConstructors[0], null);
            default:
                IMethodSymbol? constructor = null;
                int availableConstructors = 0;
                foreach (IMethodSymbol ctor in implementation.InstanceConstructors) {
                    if (ctor.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
                        continue;

                    availableConstructors++;

                    if (ctor.GetAttribute("ConstructorAttribute") != null)
                        if (constructor == null)
                            constructor = ctor;
                        else {
                            AttributeData firstAttribute = constructor.GetAttribute("ConstructorAttribute")!;
                            AttributeData secondAttribute = ctor.GetAttribute("ConstructorAttribute")!;
                            return (null, firstAttribute.CreateMultipleConstructorAttributesError(secondAttribute, implementation, implementation.ToDisplayString()));
                        }
                }

                switch (availableConstructors) {
                    case 0:
                        return (null, attributeData.CreateMissingClassOrConstructorError(implementation.ToDisplayString()));
                    case 1:
                        return (implementation.InstanceConstructors[0], null);
                    default:
                        if (constructor == null)
                            return (null, attributeData.CreateMissingConstructorAttributesError(implementation, implementation.ToDisplayString()));
                        else
                            return (constructor, null);
                }
        }
    }


    /// <summary>
    /// Creates the PropertyDependencyList based on the given class/implementation.<br />
    /// Each property marked with 'required' or '[Dependency]' will be added to the list.
    /// If one of these properties have no set/init accessor, then an error will be created and the list will be empty.
    /// </summary>
    /// <param name="implementation"></param>
    /// <param name="attributeData"></param>
    /// <returns></returns>
    public static (List<PropertyDependency> propertyDependencyList, Diagnostic? error) CreatePropertyDependencyList(INamedTypeSymbol implementation, AttributeData attributeData) {
        List<PropertyDependency> propertyDependencyList = [];

        for (INamedTypeSymbol? baseType = implementation; baseType is not null; baseType = baseType.BaseType)
            foreach (ISymbol member in baseType.GetMembers()) {
                if (member is not IPropertySymbol { Name.Length: > 0 } property)
                    continue;

                bool isNamed;
                string serviceIdentifier;
                bool isParameter;
                if (property.GetAttribute("DependencyAttribute") is AttributeData propertyAttribute) {
                    if (property.SetMethod == null)
                        return ([], attributeData.CreateMissingSetAccessorError(property, baseType, property.ToDisplayString()));

                    if (propertyAttribute.NamedArguments.GetArgument<string>("Name") is string dependencyName) {
                        isNamed = true;
                        serviceIdentifier = dependencyName;
                        isParameter = false;
                    }
                    else {
                        isNamed = false;
                        serviceIdentifier = property.Type.ToFullQualifiedName();
                        isParameter = false;
                    }
                }
                else if (property.IsRequired) {
                    if (property.SetMethod == null)
                        // natuaral syntax error when required and no setMethod
                        continue;

                    isNamed = false;
                    serviceIdentifier = property.Type.ToFullQualifiedName();
                    isParameter = true;
                }
                else
                    continue;

                propertyDependencyList.Add(new PropertyDependency() {
                    Name = property.Name,
                    IsNamed = isNamed,
                    ServiceIdentifier = serviceIdentifier,
                    HasAttribute = isParameter,
                    IsInit = property.SetMethod.IsInitOnly,
                    IsRequired = property.IsRequired,
                });
            }

        return (propertyDependencyList, null);
    }


    #region Equals

    public static bool operator ==(Service left, Service right) => left.Equals(right);

    public static bool operator !=(Service left, Service right) => !(left == right);

    public override bool Equals(object? obj)
        => obj switch {
            Service service => Equals(service),
            _ => false
        };

    public bool Equals(Service other) {
        if (ReferenceEquals(this, other))
            return true;

        if (Lifetime != other.Lifetime)
            // treat Transient and TransientScope the same
            if (!Lifetime.HasFlag(ServiceLifetime.Transient) || !other.Lifetime.HasFlag(ServiceLifetime.Transient))
                return false;

        if (ServiceType != other.ServiceType)
            return false;
        if (ServiceTypeArity != other.ServiceTypeArity)
            return false;
        if (ImplementationType != other.ImplementationType)
            return false;
        if (Implementation != other.Implementation)
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

        switch ((ErrorList, other.ErrorList)) {
            case (null, null):
                break;
            case (not null, null):
                return false;
            case (null, not null):
                return false;
            default:
                if (!ErrorList.SequenceEqual(other.ErrorList))
                    return false;
                break;
        }

        return true;
    }

    public override int GetHashCode() {
        int hashCode = Lifetime.GetHashCode();
        hashCode = Combine(hashCode, ServiceType.GetHashCode());
        hashCode = Combine(hashCode, ServiceTypeArity.GetHashCode());
        hashCode = Combine(hashCode, ImplementationType.GetHashCode());
        hashCode = Combine(hashCode, Implementation.GetHashCode());

        hashCode = Combine(hashCode, Name.GetHashCode());
        hashCode = Combine(hashCode, CreationTime.GetHashCode());
        hashCode = Combine(hashCode, GetAccessor.GetHashCode());

        foreach (ConstructorDependency constructorDependency in ConstructorDependencyList)
            hashCode = Combine(hashCode, constructorDependency.GetHashCode());
        foreach (PropertyDependency propertyDependency in PropertyDependencyList)
            hashCode = Combine(hashCode, propertyDependency.GetHashCode());

        hashCode = Combine(hashCode, IsDisposable.GetHashCode());
        hashCode = Combine(hashCode, IsAsyncDisposable.GetHashCode());

        if (ErrorList != null)
            foreach (Diagnostic error in ErrorList)
                hashCode = Combine(hashCode, error.GetHashCode());

        return hashCode;


        static int Combine(int h1, int h2) {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }

    #endregion
}
