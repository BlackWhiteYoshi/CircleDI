using Microsoft.CodeAnalysis;

namespace CircleDI.Defenitions;

public static class DiagnosticErrors {
    #region ServiceProvider Errors

    public static Diagnostic CreatePartialKeywordServiceProviderError(this AttributeData serviceProviderAttribute)
        => Diagnostic.Create(PartialKeywordServiceProvider, serviceProviderAttribute.ToLocation());

    private static DiagnosticDescriptor PartialKeywordServiceProvider { get; } = new(
        id: "CDI001",
        title: "ServiceProvider has no partial keyword",
        messageFormat: "Missing partial keyword",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreatePartialKeywordScopeProviderError(this AttributeData serviceProviderAttribute)
        => Diagnostic.Create(PartialKeywordScopeProvider, serviceProviderAttribute.ToLocation());

    private static DiagnosticDescriptor PartialKeywordScopeProvider { get; } = new(
        id: "CDI002",
        title: "ScopedProvider has no partial keyword",
        messageFormat: "Missing partial keyword",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateInterfaceNameIServiceProviderError(this AttributeData serviceProvider)
        => Diagnostic.Create(InterfaceNameIServiceProvider, serviceProvider.ToLocation());

    private static DiagnosticDescriptor InterfaceNameIServiceProvider { get; } = new(
        id: "CDI035",
        title: "InterfaceName is IServiceProvider",
        messageFormat: "InterfaceName 'IServiceProvider' is not allowed, it collides with 'System.IServiceProvider'",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateScopeProviderAttributeTwiceError(this AttributeData scopedProviderAttributeNested, AttributeData scopedProviderAttribute)
        => Diagnostic.Create(ScopeProviderAttributeTwice, scopedProviderAttributeNested.ToLocation(), additionalLocations: scopedProviderAttribute.ToLocationList());

    private static DiagnosticDescriptor ScopeProviderAttributeTwice { get; } = new(
        id: "CDI003",
        title: "ScopedProviderAttribute is defined twice",
        messageFormat: "Double ScopedProviderAttribute is not allowed, put either one on the ServiceProvider or ScopedProvider, but not both",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateEndlessRecursiveConstructorCallError(this AttributeData serviceAttribute, string serviceName)
        => Diagnostic.Create(EndlessRecursiveConstructorCall, serviceAttribute.ToLocation(), [serviceName]);

    private static DiagnosticDescriptor EndlessRecursiveConstructorCall { get; } = new(
        id: "CDI004",
        title: "Endless recursive constructor call",
        messageFormat: "Endless recursive constructor call in ServiceProvider: Service '{0}' adds a constructor call to the constructor which results in an endless recursion. Did you mean to add 'Implementation = \"this\"'?",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static Diagnostic CreateEndlessRecursiveConstructorCallScopeError(this AttributeData serviceAttribute, string serviceName)
        => Diagnostic.Create(EndlessRecursiveConstructorCallScope, serviceAttribute.ToLocation(), [serviceName]);

    private static DiagnosticDescriptor EndlessRecursiveConstructorCallScope { get; } = new(
        id: "CDI005",
        title: "Endless recursive constructor call in scope",
        messageFormat: "Endless recursive constructor call in ScopedProvider: Service '{0}' adds a constructor call to the constructor which results in an endless recursion. Did you mean to add 'Implementation = \"this\"'?",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    #endregion


    #region Service Errors

    public static Diagnostic CreateTransientImplementationFieldError(this AttributeData serviceAttribute)
        => Diagnostic.Create(TransientImplementationField, serviceAttribute.ToLocation());

    private static DiagnosticDescriptor TransientImplementationField { get; } = new(
        id: "CDI006",
        title: "Transient + Implementation field member is not allowed",
        messageFormat: "Transient + Implementation field member is not allowed. Use Singleton or Scoped instead or use a property/method as Implementation",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static Diagnostic CreateTransientImplementationThisError(this AttributeData serviceAttribute)
        => Diagnostic.Create(TransientImplementationThis, serviceAttribute.ToLocation());

    private static DiagnosticDescriptor TransientImplementationThis { get; } = new(
        id: "CDI007",
        title: "Transient + Implementation = 'this'",
        messageFormat: "Transient + Implementation = 'this' is not allowed. Use Singleton or Scoped instead",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateMissingImplementationMemberError(this AttributeData serviceAttribute, string serviceProviderName, string implementationName)
        => Diagnostic.Create(MissingImplementationMember, serviceAttribute.ToLocation(), [serviceProviderName, implementationName]);

    private static DiagnosticDescriptor MissingImplementationMember { get; } = new(
        id: "CDI008",
        title: "Missing Implementation member",
        messageFormat: "No field, property or method with the name '{1}' in class '{0}' could be found",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    
    public static Diagnostic CreateWrongFieldImplementationTypeError(this AttributeData serviceAttribute, string implementationName, string actualType, string expectedType)
        => Diagnostic.Create(WrongFieldImplementationTypeError, serviceAttribute.ToLocation(), [implementationName, actualType, expectedType]);

    private static DiagnosticDescriptor WrongFieldImplementationTypeError { get; } = new(
        id: "CDI009",
        title: "Wrong field Implementation type",
        messageFormat: "Wrong type of field '{0}': '{1}' <-> '{2}' expected",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateWrongPropertyImplementationTypeError(this AttributeData serviceAttribute, string implementationName, string actualType, string expectedType)
        => Diagnostic.Create(WrongPropertyImplementationTypeError, serviceAttribute.ToLocation(), [implementationName, actualType, expectedType]);

    private static DiagnosticDescriptor WrongPropertyImplementationTypeError { get; } = new(
        id: "CDI010",
        title: "Wrong property Implementation type",
        messageFormat: "Wrong type of property '{0}': '{1}' <-> '{2}' expected",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateWrongMethodImplementationTypeError(this AttributeData serviceAttribute, string implementationName, string actualType, string expectedType)
        => Diagnostic.Create(WrongMethodImplementationTypeError, serviceAttribute.ToLocation(), [implementationName, actualType, expectedType]);

    private static DiagnosticDescriptor WrongMethodImplementationTypeError { get; } = new(
        id: "CDI011",
        title: "Wrong method Implementation type",
        messageFormat: "Wrong return type of method '{0}': '{1}' <-> '{2}' expected",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateDelegateServiceIsNotDelegateError(this AttributeData serviceAttribute, string serviceType)
        => Diagnostic.Create(DelegateServiceIsNotDelegate, serviceAttribute.ToLocation(), [serviceType]);

    private static DiagnosticDescriptor DelegateServiceIsNotDelegate { get; } = new(
        id: "CDI012",
        title: "Delegate service is not Delegate",
        messageFormat: "Delegate service '{0}' is not a Delegate type",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateMissingDelegateImplementationError(this AttributeData serviceAttribute, string serviceProviderName, string implementationName)
        => Diagnostic.Create(MissingDelegateImplementation, serviceAttribute.ToLocation(), [serviceProviderName, implementationName]);

    private static DiagnosticDescriptor MissingDelegateImplementation { get; } = new(
        id: "CDI013",
        title: "Missing delegate implementation",
        messageFormat: "No method with the name '{1}' in class '{0}' could be found",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateDelegateWrongParameterCountError(this AttributeData serviceAttribute, string methodName, int methodParameterNumber, int delegateParameterNumber)
        => Diagnostic.Create(DelegateWrongNumberOfParameters, serviceAttribute.ToLocation(), [methodName, methodParameterNumber, delegateParameterNumber]);

    private static DiagnosticDescriptor DelegateWrongNumberOfParameters { get; } = new(
        id: "CDI014",
        title: "Delegate has wrong number of parameters",
        messageFormat: "Method '{0}' has wrong number of parameters: '{1}' <-> '{2}' expected",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateDelegateWrongParameterTypeError(this AttributeData serviceAttribute, string methodName, string methodParameterType, string delegateParameterType, int position)
        => Diagnostic.Create(DelegateWrongParameterType, serviceAttribute.ToLocation(), [methodName, methodParameterType, delegateParameterType, position]);

    private static DiagnosticDescriptor DelegateWrongParameterType { get; } = new(
        id: "CDI015",
        title: "Delegate has wrong parameter type",
        messageFormat: "Method '{0}' has wrong parameter type at position '{3}': '{1}' <-> '{2}' expected",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateDelegateWrongReturnTypeError(this AttributeData serviceAttribute, string methodName, string methodParameterType, string delegateParameterType)
        => Diagnostic.Create(DelegateWrongReturnType, serviceAttribute.ToLocation(), [methodName, methodParameterType, delegateParameterType]);

    private static DiagnosticDescriptor DelegateWrongReturnType { get; } = new(
        id: "CDI016",
        title: "Delegate has wrong return type",
        messageFormat: "Method '{0}' has wrong return type: '{1}' <-> '{2}' expected",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateMissingClassOrConstructorError(this AttributeData serviceAttribute, string implementationType)
        => Diagnostic.Create(MissingClassOrConstructor, serviceAttribute.ToLocation(), [implementationType]);

    private static DiagnosticDescriptor MissingClassOrConstructor { get; } = new(
        id: "CDI017",
        title: "Missing class or constructor",
        messageFormat: "ServiceImplementation '{0}' does not exist or has no accessible constructor",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateMissingConstructorAttributesError(this AttributeData serviceAttribute, INamedTypeSymbol implementation, string implementationType)
        => Diagnostic.Create(MissingConstructorAttributes, serviceAttribute.ToLocation(), implementation.Locations, [implementationType]);

    private static DiagnosticDescriptor MissingConstructorAttributes { get; } = new(
        id: "CDI018",
        title: "Missing ConstructorAttributes",
        messageFormat: "No ConstructorAttribute at ServiceImplementation '{0}', but there are multiple constructors",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateMultipleConstructorAttributesError(this AttributeData firstAttribute, AttributeData secondAttribute, INamedTypeSymbol implementation, string implementationType) {
        List<Location> locationList = new(1 + implementation.Locations.Length);
        if (firstAttribute.ApplicationSyntaxReference is SyntaxReference reference)
            locationList.Add(Location.Create(reference.SyntaxTree, reference.Span));
        locationList.AddRange(implementation.Locations);

        return Diagnostic.Create(MultipleConstructorAttributes, secondAttribute.ToLocation(), locationList, [implementationType]);
    }

    private static DiagnosticDescriptor MultipleConstructorAttributes { get; } = new(
        id: "CDI019",
        title: "Multiple ConstructorAttributes",
        messageFormat: "Multiple ConstructorAttributes at ServiceImplementation '{0}', there must be exactly one when there are multiple constructors",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateMissingSetAccessorError(this AttributeData serviceAttribute, IPropertySymbol property, INamedTypeSymbol implementation, string propertyName) {
        Location[] locationList = new Location[property.Locations.Length + implementation.Locations.Length];
        property.Locations.CopyTo(locationList);
        implementation.Locations.CopyTo(locationList, property.Locations.Length);

        return Diagnostic.Create(MissingSetAccessor, serviceAttribute.ToLocation(), locationList, [propertyName]);
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

    public static Diagnostic CreateDependencyUnregisteredError(this AttributeData serviceProviderAttribute, string serviceName, string dependencyServiceIdentifier)
        => Diagnostic.Create(DependencyUnregistered, serviceProviderAttribute.ToLocation(), [serviceName, dependencyServiceIdentifier]);

    private static DiagnosticDescriptor DependencyUnregistered { get; } = new(
        id: "CDI021",
        title: "Dependency unregistered",
        messageFormat: "Unregistered dependency at Service '{0}' with type '{1}'",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateDependencyInterfaceUndeclaredError(this AttributeData serviceProviderAttribute, string dependencyServiceIdentifier, string namspace, string interfaceType)
        => Diagnostic.Create(DependencyInterfaceUndeclared, serviceProviderAttribute.ToLocation(), [dependencyServiceIdentifier, namspace, interfaceType]);

    private static DiagnosticDescriptor DependencyInterfaceUndeclared { get; } = new(
        id: "CDI022",
        title: "Dependency Interface unregistered",
        messageFormat: "Unregistered dependency '{0}' has the same identifier as generated interface type '{1}.{0}', only missing the namespace. If you mean this generated type, you can correct the namespace by just declaring the interface type in namespace '{1}': \"public partial interface {2};\"",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateDependencyAmbiguousError(this AttributeData serviceProviderAttribute, string serviceName, string dependencyServiceIdentifier, IEnumerable<string> servicesWithSameType, bool isParameter)
        => Diagnostic.Create(DependencyAmbiguous, serviceProviderAttribute.ToLocation(), [serviceName, dependencyServiceIdentifier, string.Join("\", \"", servicesWithSameType), isParameter ? "parameter" : "property"]);

    private static DiagnosticDescriptor DependencyAmbiguous { get; } = new(
        id: "CDI023",
        title: "Dependency ambiguous",
        messageFormat: "Ambiguous dependency at Service '{0}' with type '{1}': There are multiple Services registered for this type: [\"{2}\"]. Use the '[Dependency(Name=\"...\")]'-attribute on the {3} to choose one specific service",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateDependencyNamedUnregisteredError(this AttributeData serviceProviderAttribute, string serviceName, string dependencyServiceIdentifier)
        => Diagnostic.Create(DependencyNamedUnregistered, serviceProviderAttribute.ToLocation(), [serviceName, dependencyServiceIdentifier]);

    private static DiagnosticDescriptor DependencyNamedUnregistered { get; } = new(
        id: "CDI024",
        title: "Dependency named unregistered",
        messageFormat: "Unregistered named dependency at Service '{0}' with name \"{1}\"",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateDependencyCircleError(this AttributeData serviceProviderAttribute, IEnumerable<string> circleList)
        => Diagnostic.Create(DependencyCircle, serviceProviderAttribute.ToLocation(), [string.Join("' -> '", circleList)]);

    private static DiagnosticDescriptor DependencyCircle { get; } = new(
        id: "CDI025",
        title: "Dependency circle",
        messageFormat: "Circular dependency unresolvable: ['{0}']. Only singleton and scoped dependencies injected as properties can be resolved circular",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateDependencyLifetimeScopeError(this AttributeData serviceProviderAttribute, string serviceName, string dependencyServiceIdentifier)
        => Diagnostic.Create(DependencyLifetimeScope, serviceProviderAttribute.ToLocation(), [serviceName, dependencyServiceIdentifier]);

    private static DiagnosticDescriptor DependencyLifetimeScope { get; } = new(
        id: "CDI026",
        title: "Dependency Lifetime: Singleton on Scoped",
        messageFormat: "Lifetime Violation: Singleton Service '{0}' has Scoped dependency '{1}'",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateDependencyLifetimeTransientError(this AttributeData serviceProviderAttribute, string serviceName, string dependencyServiceIdentifier)
        => Diagnostic.Create(DependencyLifetimeTransient, serviceProviderAttribute.ToLocation(), [serviceName, dependencyServiceIdentifier]);

    private static DiagnosticDescriptor DependencyLifetimeTransient { get; } = new(
        id: "CDI027",
        title: "Dependency Lifetime: Singleton on Transient-Scoped",
        messageFormat: "Lifetime Violation: Singleton Service '{0}' has Transient-Scoped dependency '{1}'. \"Transient-Scoped\" means the service itself is transient, but it has at least one dependency or one dependency of the dependencies that is Scoped",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateDependencyLifetimeAllServicesError(this AttributeData serviceProviderAttribute, string serviceName, string dependencyServiceIdentifier, IEnumerable<string> servicesWithSameType)
        => Diagnostic.Create(DependencyLifetimeAllServices, serviceProviderAttribute.ToLocation(), [serviceName, dependencyServiceIdentifier, string.Join("\", \"", servicesWithSameType)]);

    private static DiagnosticDescriptor DependencyLifetimeAllServices { get; } = new(
        id: "CDI028",
        title: "Dependency Lifetime: Multiple servcies, but all invalid",
        messageFormat: "Lifetime Violation: Singleton Service '{0}' has dependency with type '{1}' and there are multiple services of that type, but they are all Scoped or Transient-Scoped: [\"{2}\"]",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateScopedProviderUnregisteredError(this AttributeData serviceProviderAttribute, string serviceProviderName, string dependencyServiceIdentifier)
        => Diagnostic.Create(ScopedProviderUnregistered, serviceProviderAttribute.ToLocation(), [serviceProviderName, dependencyServiceIdentifier]);

    private static DiagnosticDescriptor ScopedProviderUnregistered { get; } = new(
        id: "CDI030",
        title: "ScopedProvider Dependency unregistered",
        messageFormat: "Unregistered dependency at '{0}.Scope' with type '{1}'",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateScopedProviderAmbiguousError(this AttributeData serviceProviderAttribute, string serviceProviderName, string dependencyServiceIdentifier, IEnumerable<string> servicesWithSameType, bool isParameter)
        => Diagnostic.Create(ScopedProviderAmbiguous, serviceProviderAttribute.ToLocation(), [serviceProviderName, dependencyServiceIdentifier, string.Join("\", \"", servicesWithSameType), isParameter ? "parameter" : "property"]);

    private static DiagnosticDescriptor ScopedProviderAmbiguous { get; } = new(
        id: "CDI031",
        title: "Dependency ambiguous",
        messageFormat: "Ambiguous dependency at '{0}.Scope' with type '{1}': There are multiple Services registered for this type: [\"{2}\"]. Use the '[Dependency(Name=\"...\")]'-attribute on the {3} to choose one specific service",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateScopedProviderNamedUnregisteredError(this AttributeData serviceProviderAttribute, string serviceProviderName, string dependencyServiceIdentifier)
        => Diagnostic.Create(ScopedProviderNamedUnregistered, serviceProviderAttribute.ToLocation(), [serviceProviderName, dependencyServiceIdentifier]);

    private static DiagnosticDescriptor ScopedProviderNamedUnregistered { get; } = new(
        id: "CDI032",
        title: "Dependency named unregistered",
        messageFormat: "Unregistered named dependency at '{0}.Scope' with name \"{1}\"",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateScopedProviderLifetimeScopeError(this AttributeData serviceProviderAttribute, string serviceProviderName, string dependencyServiceIdentifier)
        => Diagnostic.Create(ScopedProviderLifetimeScope, serviceProviderAttribute.ToLocation(), [serviceProviderName, dependencyServiceIdentifier]);

    private static DiagnosticDescriptor ScopedProviderLifetimeScope { get; } = new(
        id: "CDI033",
        title: "Dependency Lifetime: ScopedProvider on Scoped",
        messageFormat: "Lifetime Violation: ScopedProvider '{0}.Scope' has Scoped dependency '{1}'",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);


    public static Diagnostic CreateScopedProviderLifetimeTransientError(this AttributeData serviceProviderAttribute, string serviceProviderName, string dependencyServiceIdentifier)
        => Diagnostic.Create(ScopedProviderLifetimeTransient, serviceProviderAttribute.ToLocation(), [serviceProviderName, dependencyServiceIdentifier]);

    private static DiagnosticDescriptor ScopedProviderLifetimeTransient { get; } = new(
        id: "CDI034",
        title: "Dependency Lifetime: ScopedProvider on Transient-Scoped",
        messageFormat: "Lifetime Violation: ScopedProvider '{0}.Scope' has Transient-Scoped dependency '{1}'. \"Transient-Scoped\" means the service itself is transient, but it has at least one dependency or one dependency of the dependencies that is Scoped",
        category: "CircleDI",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    #endregion


    private static Location? ToLocation(this AttributeData attributeData)
        => attributeData.ApplicationSyntaxReference switch {
            SyntaxReference reference => Location.Create(reference.SyntaxTree, reference.Span),
            _ => null
        };

    private static Location[] ToLocationList(this AttributeData attributeData)
        => attributeData.ApplicationSyntaxReference switch {
            SyntaxReference reference => [Location.Create(reference.SyntaxTree, reference.Span)],
            _ => []
        };
}
