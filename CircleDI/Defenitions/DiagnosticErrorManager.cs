using CircleDI.Extensions;
using Microsoft.CodeAnalysis;
using System.Text;

namespace CircleDI.Defenitions;

/// <summary>
/// Collection of global <see cref="DiagnosticDescriptor"/> objects
/// and methods to add <see cref="Diagnostic"/> objects of severity error to the <see cref="ErrorList"/>.
/// </summary>
public sealed class DiagnosticErrorManager(AttributeData serviceProviderAttribute) {
    public AttributeData ServiceProviderAttribute { get; } = serviceProviderAttribute;

    private AttributeData _currentAttribute = serviceProviderAttribute;
    public AttributeData CurrentAttribute {
        get => _currentAttribute;
        set {
            if (SymbolEqualityComparer.Default.Equals(value.AttributeClass!.ContainingAssembly, ServiceProviderAttribute.AttributeClass!.ContainingAssembly))
                _currentAttribute = value;
        }
    }

    /// <summary>
    /// Diagnostics with Severity error.
    /// </summary>
    public List<Diagnostic> ErrorList { get; } = [];


    #region ServiceProvider Errors

    public void AddPartialKeywordServiceProviderError()
        => ErrorList.Add(Diagnostic.Create(PartialKeywordServiceProvider, ServiceProviderAttribute.ToLocation()));

    private static DiagnosticDescriptor PartialKeywordServiceProvider { get; } = new(
        id: "CDI001",
        title: "ServiceProvider has no partial keyword",
        messageFormat: "Missing partial keyword",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddPartialKeywordScopeProviderError()
        => ErrorList.Add(Diagnostic.Create(PartialKeywordScopeProvider, ServiceProviderAttribute.ToLocation()));

    private static DiagnosticDescriptor PartialKeywordScopeProvider { get; } = new(
        id: "CDI002",
        title: "ScopedProvider has no partial keyword",
        messageFormat: "Missing partial keyword",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddScopeProviderAttributeTwiceError(AttributeData scopedProviderAttributeNested, AttributeData scopedProviderAttribute)
        => ErrorList.Add(Diagnostic.Create(ScopeProviderAttributeTwice, scopedProviderAttributeNested.ToLocation(), additionalLocations: scopedProviderAttribute.ToLocationList()));

    private static DiagnosticDescriptor ScopeProviderAttributeTwice { get; } = new(
        id: "CDI003",
        title: "ScopedProviderAttribute is defined twice",
        messageFormat: "Double ScopedProviderAttribute is not allowed, put either one on the ServiceProvider or ScopedProvider, but not both",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddInterfaceTypeAndNameError()
        => ErrorList.Add(Diagnostic.Create(InterfaceTypeAndName, ServiceProviderAttribute.ToLocation()));

    private static DiagnosticDescriptor InterfaceTypeAndName { get; } = new(
        id: "CDI040",
        title: "InterfaceType and InterfaceName are incompatible",
        messageFormat: "InterfaceType and InterfaceName are not compatible, at most one property must be set.",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddInterfaceNameIServiceProviderError()
        => ErrorList.Add(Diagnostic.Create(InterfaceNameIServiceProvider, ServiceProviderAttribute.ToLocation()));

    private static DiagnosticDescriptor InterfaceNameIServiceProvider { get; } = new(
        id: "CDI035",
        title: "InterfaceName is IServiceProvider",
        messageFormat: "InterfaceName 'IServiceProvider' is not allowed, it collides with 'System.IServiceProvider'",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddModuleCircleError(TypeName serviceProvider, IEnumerable<string> circleList)
        => ErrorList.Add(Diagnostic.Create(ModuleCircle, ServiceProviderAttribute.ToLocation(), [serviceProvider.CreateFullyQualifiedName(), string.Join("' -> '", circleList)]));

    private static DiagnosticDescriptor ModuleCircle { get; } = new(
        id: "CDI036",
        title: "Module circle",
        messageFormat: "Module cycle in ServiceProvider '{0}': ['{1}']",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    #endregion


    #region Service Errors

    public void AddEndlessRecursiveConstructorCallError(string serviceName)
        => ErrorList.Add(Diagnostic.Create(EndlessRecursiveConstructorCall, CurrentAttribute.ToLocation(), [serviceName]));

    private static DiagnosticDescriptor EndlessRecursiveConstructorCall { get; } = new(
        id: "CDI004",
        title: "Endless recursive constructor call",
        messageFormat: "Endless recursive constructor call in ServiceProvider: Service '{0}' adds a constructor call to the constructor which results in an endless recursion. Did you mean to add 'Implementation = \"this\"'?",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddEndlessRecursiveConstructorCallScopeError(string serviceName)
        => ErrorList.Add(Diagnostic.Create(EndlessRecursiveConstructorCallScope, CurrentAttribute.ToLocation(), [serviceName]));

    private static DiagnosticDescriptor EndlessRecursiveConstructorCallScope { get; } = new(
        id: "CDI005",
        title: "Endless recursive constructor call in scope",
        messageFormat: "Endless recursive constructor call in ScopedProvider: Service '{0}' adds a constructor call to the constructor which results in an endless recursion. Did you mean to add 'Implementation = \"this\"'?",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public void AddInvalidServiceRegistrationError(TypeName serviceProviderIdentifier, TypeName interfaceIdentifier)
        => ErrorList.Add(Diagnostic.Create(InvalidServiceRegistration, CurrentAttribute.ToLocation(), [serviceProviderIdentifier.CreateFullyQualifiedName(), interfaceIdentifier.CreateFullyQualifiedName()]));

    private static DiagnosticDescriptor InvalidServiceRegistration { get; } = new(
        id: "CDI037",
        title: "Invalid service registration",
        messageFormat: "Invalid type at service registration. If you are using a generated type like '{0}.Scope', '{1}' or '{1}.IScope', declare that type again, so it is available before generation.",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddTransientImplementationFieldError()
        => ErrorList.Add(Diagnostic.Create(TransientImplementationField, CurrentAttribute.ToLocation()));

    private static DiagnosticDescriptor TransientImplementationField { get; } = new(
        id: "CDI006",
        title: "Transient + Implementation field member is not allowed",
        messageFormat: "Transient + Implementation field member is not allowed. Use Singleton or Scoped instead or use a property/method as Implementation",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddTransientImplementationThisError()
        => ErrorList.Add(Diagnostic.Create(TransientImplementationThis, CurrentAttribute.ToLocation()));

    private static DiagnosticDescriptor TransientImplementationThis { get; } = new(
        id: "CDI007",
        title: "Transient + Implementation = 'this'",
        messageFormat: "Transient + Implementation = 'this' is not allowed. Use Singleton or Scoped instead",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddMissingImplementationMemberError(string serviceProviderName, string implementationName)
        => ErrorList.Add(Diagnostic.Create(MissingImplementationMember, CurrentAttribute.ToLocation(), [serviceProviderName, implementationName]));

    private static DiagnosticDescriptor MissingImplementationMember { get; } = new(
        id: "CDI008",
        title: "Missing Implementation member",
        messageFormat: "No field, property or method with the name '{1}' in class '{0}' could be found",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    
    public void AddWrongFieldImplementationTypeError(string implementationName, string actualType, TypeName expectedType)
        => ErrorList.Add(Diagnostic.Create(WrongFieldImplementationTypeError, CurrentAttribute.ToLocation(), [implementationName, actualType, expectedType.CreateFullyQualifiedName()]));

    private static DiagnosticDescriptor WrongFieldImplementationTypeError { get; } = new(
        id: "CDI009",
        title: "Wrong field Implementation type",
        messageFormat: "Wrong type of field '{0}': '{1}' <-> '{2}' expected",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddWrongPropertyImplementationTypeError(string implementationName, string actualType, TypeName expectedType)
        => ErrorList.Add(Diagnostic.Create(WrongPropertyImplementationTypeError, CurrentAttribute.ToLocation(), [implementationName, actualType, expectedType.CreateFullyQualifiedName()]));

    private static DiagnosticDescriptor WrongPropertyImplementationTypeError { get; } = new(
        id: "CDI010",
        title: "Wrong property Implementation type",
        messageFormat: "Wrong type of property '{0}': '{1}' <-> '{2}' expected",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddWrongMethodImplementationTypeError(string implementationName, string actualType, TypeName expectedType)
        => ErrorList.Add(Diagnostic.Create(WrongMethodImplementationTypeError, CurrentAttribute.ToLocation(), [implementationName, actualType, expectedType.CreateFullyQualifiedName()]));

    private static DiagnosticDescriptor WrongMethodImplementationTypeError { get; } = new(
        id: "CDI011",
        title: "Wrong method Implementation type",
        messageFormat: "Wrong return type of method '{0}': '{1}' <-> '{2}' expected",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddDelegateServiceIsNotDelegateError(TypeName serviceType)
        => ErrorList.Add(Diagnostic.Create(DelegateServiceIsNotDelegate, CurrentAttribute.ToLocation(), [serviceType.CreateFullyQualifiedName()]));

    private static DiagnosticDescriptor DelegateServiceIsNotDelegate { get; } = new(
        id: "CDI012",
        title: "Delegate service is not Delegate",
        messageFormat: "Delegate service '{0}' is not a Delegate type",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddMissingDelegateImplementationError(string serviceProviderName, string implementationName)
        => ErrorList.Add(Diagnostic.Create(MissingDelegateImplementation, CurrentAttribute.ToLocation(), [serviceProviderName, implementationName]));

    private static DiagnosticDescriptor MissingDelegateImplementation { get; } = new(
        id: "CDI013",
        title: "Missing delegate implementation",
        messageFormat: "No method with the name '{1}' in class '{0}' could be found",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddDelegateWrongParameterCountError(string methodName, int methodParameterNumber, int delegateParameterNumber)
        => ErrorList.Add(Diagnostic.Create(DelegateWrongNumberOfParameters, CurrentAttribute.ToLocation(), [methodName, methodParameterNumber, delegateParameterNumber]));

    private static DiagnosticDescriptor DelegateWrongNumberOfParameters { get; } = new(
        id: "CDI014",
        title: "Delegate has wrong number of parameters",
        messageFormat: "Method '{0}' has wrong number of parameters: '{1}' <-> '{2}' expected",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddDelegateWrongParameterTypeError(string methodName, string methodParameterType, string delegateParameterType, int position)
        => ErrorList.Add(Diagnostic.Create(DelegateWrongParameterType, CurrentAttribute.ToLocation(), [methodName, methodParameterType, delegateParameterType, position]));

    private static DiagnosticDescriptor DelegateWrongParameterType { get; } = new(
        id: "CDI015",
        title: "Delegate has wrong parameter type",
        messageFormat: "Method '{0}' has wrong parameter type at position '{3}': '{1}' <-> '{2}' expected",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddDelegateWrongReturnTypeError(string methodName, string methodParameterType, string delegateParameterType)
        => ErrorList.Add(Diagnostic.Create(DelegateWrongReturnType, CurrentAttribute.ToLocation(), [methodName, methodParameterType, delegateParameterType]));

    private static DiagnosticDescriptor DelegateWrongReturnType { get; } = new(
        id: "CDI016",
        title: "Delegate has wrong return type",
        messageFormat: "Method '{0}' has wrong return type: '{1}' <-> '{2}' expected",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddMissingClassOrConstructorError(string implementationType)
        => ErrorList.Add(Diagnostic.Create(MissingClassOrConstructor, CurrentAttribute.ToLocation(), [implementationType]));

    private static DiagnosticDescriptor MissingClassOrConstructor { get; } = new(
        id: "CDI017",
        title: "Missing class or constructor",
        messageFormat: "ServiceImplementation '{0}' does not exist or has no accessible constructor",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddMissingConstructorAttributesError(INamedTypeSymbol implementation, string implementationType) {
        System.Collections.Immutable.ImmutableArray<Location> locations = [];
        if (SymbolEqualityComparer.Default.Equals(implementation.ContainingAssembly, ServiceProviderAttribute.AttributeClass!.ContainingAssembly))
            locations = implementation.Locations;

        ErrorList.Add(Diagnostic.Create(MissingConstructorAttributes, CurrentAttribute.ToLocation(), locations, [implementationType]));
    }

    private static DiagnosticDescriptor MissingConstructorAttributes { get; } = new(
        id: "CDI018",
        title: "Missing ConstructorAttributes",
        messageFormat: "No ConstructorAttribute at ServiceImplementation '{0}', but there are multiple constructors",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddMultipleConstructorAttributesError(AttributeData firstAttribute, AttributeData secondAttribute, INamedTypeSymbol implementation, string implementationType) {
        Location? location;
        List<Location> locationList;
        if (SymbolEqualityComparer.Default.Equals(implementation.ContainingAssembly, ServiceProviderAttribute.AttributeClass!.ContainingAssembly)) {
            location = secondAttribute.ToLocation();
            
            locationList = new List<Location>(1 + implementation.Locations.Length);
            if (firstAttribute.ApplicationSyntaxReference is SyntaxReference reference)
                locationList.Add(Location.Create(reference.SyntaxTree, reference.Span));
            locationList.AddRange(implementation.Locations);
        }
        else {
            location = CurrentAttribute.ToLocation();
            locationList = [];
        }

        ErrorList.Add(Diagnostic.Create(MultipleConstructorAttributes, location, locationList, [implementationType]));
    }

    private static DiagnosticDescriptor MultipleConstructorAttributes { get; } = new(
        id: "CDI019",
        title: "Multiple ConstructorAttributes",
        messageFormat: "Multiple ConstructorAttributes at ServiceImplementation '{0}', there must be exactly one when there are multiple constructors",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddMissingSetAccessorError(IPropertySymbol property, INamedTypeSymbol implementation, string propertyName) {
        Location[] locationList;
        if (SymbolEqualityComparer.Default.Equals(implementation.ContainingAssembly, ServiceProviderAttribute.AttributeClass!.ContainingAssembly)) {
            locationList = new Location[property.Locations.Length + implementation.Locations.Length];
            property.Locations.CopyTo(locationList);
            implementation.Locations.CopyTo(locationList, property.Locations.Length);
        }
        else
            locationList = [];

        ErrorList.Add(Diagnostic.Create(MissingSetAccessor, CurrentAttribute.ToLocation(), locationList, [propertyName]));
    }

    private static DiagnosticDescriptor MissingSetAccessor { get; } = new(
        id: "CDI020",
        title: "Missing set accessor",
        messageFormat: "No set/init accessor at Property '{0}'",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    #endregion


    #region Dependency Tree Generation

    public void AddDependencyUnregisteredError(string serviceName, TypeName dependencyServiceIdentifier)
        => ErrorList.Add(Diagnostic.Create(DependencyUnregistered, ServiceProviderAttribute.ToLocation(), [serviceName, dependencyServiceIdentifier.CreateFullyQualifiedName()]));

    private static DiagnosticDescriptor DependencyUnregistered { get; } = new(
        id: "CDI021",
        title: "Dependency unregistered",
        messageFormat: "Unregistered dependency at Service '{0}' with type '{1}'",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddDependencyInterfaceUndeclaredError(TypeName dependencyServiceIdentifier, string namspace, string interfaceType)
        => ErrorList.Add(Diagnostic.Create(DependencyInterfaceUndeclared, ServiceProviderAttribute.ToLocation(), [dependencyServiceIdentifier.CreateFullyQualifiedName(), namspace, interfaceType]));

    private static DiagnosticDescriptor DependencyInterfaceUndeclared { get; } = new(
        id: "CDI022",
        title: "Dependency Interface unregistered",
        messageFormat: "Unregistered dependency '{0}' has the same identifier as generated interface type '{1}.{0}', only missing the namespace. If you mean this generated type, you can correct the namespace by just declaring the interface type in namespace '{1}': \"public partial interface {2};\"",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddDependencyAmbiguousError(string serviceName, TypeName dependencyServiceIdentifier, IEnumerable<string> servicesWithSameType, bool isParameter)
        => ErrorList.Add(Diagnostic.Create(DependencyAmbiguous, ServiceProviderAttribute.ToLocation(), [serviceName, dependencyServiceIdentifier.CreateFullyQualifiedName(), string.Join("\", \"", servicesWithSameType), isParameter ? "parameter" : "property"]));

    private static DiagnosticDescriptor DependencyAmbiguous { get; } = new(
        id: "CDI023",
        title: "Dependency ambiguous",
        messageFormat: "Ambiguous dependency at Service '{0}' with type '{1}': There are multiple Services registered for this type: [\"{2}\"]. Use the '[Dependency(Name=\"...\")]'-attribute on the {3} to choose one specific service",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddDependencyNamedUnregisteredError(string serviceName, string dependencyServiceIdentifier)
        => ErrorList.Add(Diagnostic.Create(DependencyNamedUnregistered, ServiceProviderAttribute.ToLocation(), [serviceName, dependencyServiceIdentifier]));

    private static DiagnosticDescriptor DependencyNamedUnregistered { get; } = new(
        id: "CDI024",
        title: "Dependency named unregistered",
        messageFormat: "Unregistered named dependency at Service '{0}' with name \"{1}\"",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddDependencyCircleError(IEnumerable<string> circleList)
        => ErrorList.Add(Diagnostic.Create(DependencyCircle, ServiceProviderAttribute.ToLocation(), [string.Join("' -> '", circleList)]));

    private static DiagnosticDescriptor DependencyCircle { get; } = new(
        id: "CDI025",
        title: "Dependency circle",
        messageFormat: "Circular dependency unresolvable: ['{0}']. Only singleton and scoped dependencies injected as properties can be resolved circular",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddDependencyLifetimeScopeError(string serviceName, TypeName dependencyServiceIdentifier)
        => ErrorList.Add(Diagnostic.Create(DependencyLifetimeScope, ServiceProviderAttribute.ToLocation(), [serviceName, dependencyServiceIdentifier.CreateFullyQualifiedName()]));

    private static DiagnosticDescriptor DependencyLifetimeScope { get; } = new(
        id: "CDI026",
        title: "Dependency Lifetime: Singleton on Scoped",
        messageFormat: "Lifetime Violation: Singleton Service '{0}' has Scoped dependency '{1}'",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddDependencyLifetimeTransientError(string serviceName, TypeName dependencyServiceIdentifier)
        => ErrorList.Add(Diagnostic.Create(DependencyLifetimeTransient, ServiceProviderAttribute.ToLocation(), [serviceName, dependencyServiceIdentifier.CreateFullyQualifiedName()]));

    private static DiagnosticDescriptor DependencyLifetimeTransient { get; } = new(
        id: "CDI027",
        title: "Dependency Lifetime: Singleton on Transient-Scoped",
        messageFormat: "Lifetime Violation: Singleton Service '{0}' has Transient-Scoped dependency '{1}'. \"Transient-Scoped\" means the service itself is transient, but it has at least one dependency or one dependency of the dependencies that is Scoped",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddDependencyLifetimeDelegateError(string serviceName, TypeName dependencyServiceIdentifier)
        => ErrorList.Add(Diagnostic.Create(DependencyLifetimeDelegate, ServiceProviderAttribute.ToLocation(), [serviceName, dependencyServiceIdentifier.CreateFullyQualifiedName()]));

    private static DiagnosticDescriptor DependencyLifetimeDelegate { get; } = new(
        id: "CDI038",
        title: "Dependency Lifetime: Singleton on Delegate-Scoped",
        messageFormat: "Lifetime Violation: Singleton Service '{0}' has Delegate-Scoped dependency '{1}'. \"Delegate-Scoped\" means the method is declared inside Scope and therefore only available for scoped services.",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddDependencyLifetimeAllServicesError(string serviceName, TypeName dependencyServiceIdentifier, IEnumerable<string> servicesWithSameType)
        => ErrorList.Add(Diagnostic.Create(DependencyLifetimeAllServices, ServiceProviderAttribute.ToLocation(), [serviceName, dependencyServiceIdentifier.CreateFullyQualifiedName(), string.Join("\", \"", servicesWithSameType)]));

    private static DiagnosticDescriptor DependencyLifetimeAllServices { get; } = new(
        id: "CDI028",
        title: "Dependency Lifetime: Multiple servcies, but all invalid",
        messageFormat: "Lifetime Violation: Singleton Service '{0}' has dependency with type '{1}' and there are multiple services of that type, but they are all invalid (Scoped or Transient-Scoped): [\"{2}\"]",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddScopedProviderUnregisteredError(TypeName serviceProvider, TypeName dependencyServiceIdentifier)
        => ErrorList.Add(Diagnostic.Create(ScopedProviderUnregistered, ServiceProviderAttribute.ToLocation(), [serviceProvider.CreateFullyQualifiedName(), dependencyServiceIdentifier.CreateFullyQualifiedName()]));

    private static DiagnosticDescriptor ScopedProviderUnregistered { get; } = new(
        id: "CDI030",
        title: "ScopedProvider Dependency unregistered",
        messageFormat: "Unregistered dependency at '{0}.Scope' with type '{1}'",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddScopedProviderAmbiguousError(TypeName serviceProvider, TypeName dependencyServiceIdentifier, IEnumerable<string> servicesWithSameType, bool isParameter)
        => ErrorList.Add(Diagnostic.Create(ScopedProviderAmbiguous, ServiceProviderAttribute.ToLocation(), [serviceProvider.CreateFullyQualifiedName(), dependencyServiceIdentifier.CreateFullyQualifiedName(), string.Join("\", \"", servicesWithSameType), isParameter ? "parameter" : "property"]));

    private static DiagnosticDescriptor ScopedProviderAmbiguous { get; } = new(
        id: "CDI031",
        title: "Dependency ambiguous",
        messageFormat: "Ambiguous dependency at '{0}.Scope' with type '{1}': There are multiple Services registered for this type: [\"{2}\"]. Use the '[Dependency(Name=\"...\")]'-attribute on the {3} to choose one specific service",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddScopedProviderNamedUnregisteredError(TypeName serviceProvider, string dependencyServiceIdentifier)
        => ErrorList.Add(Diagnostic.Create(ScopedProviderNamedUnregistered, ServiceProviderAttribute.ToLocation(), [serviceProvider.CreateFullyQualifiedName(), dependencyServiceIdentifier]));

    private static DiagnosticDescriptor ScopedProviderNamedUnregistered { get; } = new(
        id: "CDI032",
        title: "Dependency named unregistered",
        messageFormat: "Unregistered named dependency at '{0}.Scope' with name \"{1}\"",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddScopedProviderLifetimeScopeError(TypeName serviceProvider, TypeName dependencyServiceIdentifier)
        => ErrorList.Add(Diagnostic.Create(ScopedProviderLifetimeScope, ServiceProviderAttribute.ToLocation(), [serviceProvider.CreateFullyQualifiedName(), dependencyServiceIdentifier.CreateFullyQualifiedName()]));

    private static DiagnosticDescriptor ScopedProviderLifetimeScope { get; } = new(
        id: "CDI033",
        title: "Dependency Lifetime: ScopedProvider on Scoped",
        messageFormat: "Lifetime Violation: ScopedProvider '{0}.Scope' has Scoped dependency '{1}'",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddScopedProviderLifetimeTransientError(TypeName serviceProvider, TypeName dependencyServiceIdentifier)
        => ErrorList.Add(Diagnostic.Create(ScopedProviderLifetimeTransient, ServiceProviderAttribute.ToLocation(), [serviceProvider.CreateFullyQualifiedName(), dependencyServiceIdentifier.CreateFullyQualifiedName()]));

    private static DiagnosticDescriptor ScopedProviderLifetimeTransient { get; } = new(
        id: "CDI034",
        title: "Dependency Lifetime: ScopedProvider on Transient-Scoped",
        messageFormat: "Lifetime Violation: ScopedProvider '{0}.Scope' has Transient-Scoped dependency '{1}'. \"Transient-Scoped\" means the service itself is transient, but it has at least one dependency or one dependency of the dependencies that is Scoped",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public void AddScopedProviderLifetimeDelegateError(TypeName serviceProvider, TypeName dependencyServiceIdentifier)
        => ErrorList.Add(Diagnostic.Create(ScopedProviderLifetimeDelegate, ServiceProviderAttribute.ToLocation(), [serviceProvider.CreateFullyQualifiedName(), dependencyServiceIdentifier.CreateFullyQualifiedName()]));

    private static DiagnosticDescriptor ScopedProviderLifetimeDelegate { get; } = new(
        id: "CDI039",
        title: "Dependency Lifetime: ScopedProvider on Delegate-Scoped",
        messageFormat: "Lifetime Violation: ScopedProvider '{0}.Scope' has Delegate-Scoped dependency '{1}'. \"Delegate-Scoped\" means the method is declared inside Scope and therefore only available for scoped services.",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    #endregion
}

file static class DiagnosticErrorManagerExtensions {
    public static Location? ToLocation(this AttributeData attributeData)
        => attributeData.ApplicationSyntaxReference switch {
            SyntaxReference reference => Location.Create(reference.SyntaxTree, reference.Span),
            _ => null
        };

    public static Location[] ToLocationList(this AttributeData attributeData)
        => attributeData.ApplicationSyntaxReference switch {
            SyntaxReference reference => [Location.Create(reference.SyntaxTree, reference.Span)],
            _ => []
        };

    public static string CreateFullyQualifiedName(this TypeName typeName) {
        StringBuilder builder = new();
        builder.AppendClosedFullyQualified(typeName);
        return builder.ToString();
    }
}
