using CircleDI.Defenitions;
using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI.Extensions;

/// <summary>
/// Extension methods on source generator types (namespace 'Microsoft.CodeAnalysis').<br />
/// e.g. <see cref="SyntaxNode"/>, <see cref="ISymbol"/>, <see cref="AttributeData"/>, <see cref="TypedConstant"/>
/// </summary>
public static class SyntaxNodeExtensions {
    /// <summary>
    /// <para>Finds the attribute with the given name.</para>
    /// <para>If the given attribute is not present, it returns null.</para>
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static AttributeData? GetAttribute(this ISymbol symbol, string name) {
        foreach (AttributeData attributeData in symbol.GetAttributes())
            if (attributeData.AttributeClass?.Name == name)
                return attributeData;

        return null;
    }

    /// <summary>
    /// <para>Finds the argument with the given name and returns it's value.</para>
    /// <para>If not found or value is not castable, it returns default.</para>
    /// </summary>
    /// <param name="arguments"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static T? GetArgument<T>(this ImmutableArray<KeyValuePair<string, TypedConstant>> arguments, string name) {
        for (int i = 0; i < arguments.Length; i++)
            if (arguments[i].Key == name)
                return arguments[i].Value switch {
                    TypedConstant { Value: T value } => value,
                    _ => default
                };

        return default;
    }

    /// <summary>
    /// Checks if this symbol implements the given interface.
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    public static bool HasInterface(this ITypeSymbol symbol, string name) {
        foreach (INamedTypeSymbol interfaceSymbol in symbol.AllInterfaces)
            if (interfaceSymbol.Name == name)
                return true;

        return false;
    }


    /// <summary>
    /// Searches for the applicable constructor by analyzing the available constructors at the given class/implementation.<br />
    /// When no or multiple constructors are found an error will be created and null is returned.
    /// </summary>
    /// <param name="implementation"></param>
    /// <param name="errorManager"></param>
    /// <returns></returns>
    public static IMethodSymbol? FindConstructor(this INamedTypeSymbol implementation, ErrorManager errorManager) {
        switch (implementation.InstanceConstructors.Length) {
            case 0:
                errorManager.AddMissingClassOrConstructorError(implementation.ToDisplayString());
                return null;
            case 1:
                if (implementation.InstanceConstructors[0].DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal)) {
                    errorManager.AddMissingClassOrConstructorError(implementation.ToDisplayString());
                    return null;
                }
                return implementation.InstanceConstructors[0];
            default:
                IMethodSymbol? attributeConstructor = null;
                IMethodSymbol? lastAvailableConstructor = null;
                int availableConstructors = 0;
                foreach (IMethodSymbol ctor in implementation.InstanceConstructors) {
                    if (ctor.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
                        continue;

                    availableConstructors++;
                    lastAvailableConstructor = ctor;

                    if (ctor.GetAttribute("ConstructorAttribute") is not null)
                        if (attributeConstructor is null)
                            attributeConstructor = ctor;
                        else {
                            AttributeData firstAttribute = attributeConstructor.GetAttribute("ConstructorAttribute")!;
                            AttributeData secondAttribute = ctor.GetAttribute("ConstructorAttribute")!;
                            errorManager.AddMultipleConstructorAttributesError(firstAttribute, secondAttribute, implementation, implementation.ToDisplayString());
                            return null;
                        }
                }

                switch (availableConstructors) {
                    case 0:
                        errorManager.AddMissingClassOrConstructorError(implementation.ToDisplayString());
                        return null;
                    case 1:
                        return lastAvailableConstructor;
                    default:
                        if (attributeConstructor is null) {
                            errorManager.AddMissingConstructorAttributesError(implementation, implementation.ToDisplayString());
                            return null;
                        }
                        else
                            return attributeConstructor;
                }
        }
    }

    /// <summary>
    /// <para>Creates an array by mapping <see cref="IMethodSymbol.Parameters"/> to <see cref="ConstructorDependency"/>.</para>
    /// <para>
    /// Checks for attribute "DependencyAttribute" with argument "Name" -> [Dependency(Name = "...")]<br />
    /// If present, <see cref="IsNamed"/> is set to true and <see cref="ServiceIdentifier"/> is set to it's value.<br />
    /// Otherwise <see cref="IsNamed"/> is set to false, and <see cref="ServiceIdentifier"/> is set to parameter type.
    /// </para>
    /// </summary>
    /// <param name="constructor"></param>
    /// <returns></returns>
    public static List<ConstructorDependency> CreateConstructorDependencyList(this IMethodSymbol constructor) {
        List<ConstructorDependency> result = new(constructor.Parameters.Length);

        foreach (IParameterSymbol parameter in constructor.Parameters) {
            if (parameter.GetAttribute("DependencyAttribute") is AttributeData attributeData)
                if (attributeData.NamedArguments.GetArgument<string>("Name") is string dependencyName)
                    // AddNamedDependency
                    result.Add(new ConstructorDependency() {
                        Name = parameter.Name,
                        ServiceName = dependencyName,
                        ServiceType = null,
                        HasAttribute = true,
                        ByRef = parameter.RefKind
                    });
                else
                    AddTypedDependency(hasAttribute: true, parameter, result);
            else
                AddTypedDependency(hasAttribute: false, parameter, result);

            static void AddTypedDependency(bool hasAttribute, IParameterSymbol parameter, List<ConstructorDependency> result) {
                if (parameter.Type is not INamedTypeSymbol namedType)
                    return;

                foreach (ITypeSymbol argument in namedType.TypeArguments)
                    if (argument is IErrorTypeSymbol) // unbound is not allowed in this context -> invalid input
                        return; // no error needed, already syntax error

                result.Add(new ConstructorDependency() {
                    Name = parameter.Name,
                    ServiceName = string.Empty,
                    ServiceType = new TypeName(namedType),
                    HasAttribute = hasAttribute,
                    ByRef = parameter.RefKind
                });
            }
        }

        return result;
    }

    /// <summary>
    /// <para>First <see cref="FindConstructor(INamedTypeSymbol, AttributeData)">finds the constructor</see> and if found, <see cref="Extensions.SyntaxNodeExtensions.CreateConstructorDependencyList(IMethodSymbol)">creates the constructor dependency list.</see></para>
    /// <para>If no constructor found or an invalid constructor dependency was found, an error is created and the null is returned.</para>
    /// </summary>
    /// <param name="implementation"></param>
    /// <param name="errorManager"></param>
    /// <returns></returns>
    public static List<ConstructorDependency>? CreateConstructorDependencyList(this INamedTypeSymbol implementation, ErrorManager errorManager) {
        IMethodSymbol? constructor = FindConstructor(implementation, errorManager);
        if (constructor is not null)
            return CreateConstructorDependencyList(constructor);
        else
            return null;
    }


    /// <summary>
    /// Creates the PropertyDependencyList based on the given class/implementation.<br />
    /// Each property marked with 'required' or '[Dependency]' will be added to the list.
    /// If one of these properties have no set/init accessor, then an error will be created and null is returned.
    /// </summary>
    /// <param name="implementation"></param>
    /// <param name="errorManager"></param>
    /// <returns></returns>
    public static List<PropertyDependency>? CreatePropertyDependencyList(this INamedTypeSymbol implementation, ErrorManager errorManager) {
        List<PropertyDependency> propertyDependencyList = [];

        for (INamedTypeSymbol? baseType = implementation; baseType is not null; baseType = baseType.BaseType) {
            TypeName implementationBaseName = new(baseType);
            foreach (ISymbol member in baseType.GetMembers()) {
                if (member is not IPropertySymbol { Name.Length: > 0 } property)
                    continue;

                string serviceName;
                TypeName? serviceType;
                bool hasAttribute;
                if (property.GetAttribute("DependencyAttribute") is AttributeData propertyAttribute) {
                    if (property.SetMethod is null) {
                        errorManager.AddMissingSetAccessorError(property, baseType, property.ToDisplayString());
                        return null;
                    }

                    if (propertyAttribute.NamedArguments.GetArgument<string>("Name") is string dependencyName) {
                        serviceName = dependencyName;
                        serviceType = null;
                        hasAttribute = true;
                    }
                    else {
                        if (property.Type is not INamedTypeSymbol namedType)
                            continue;
                        foreach (ITypeSymbol argument in namedType.TypeArguments)
                            if (argument is IErrorTypeSymbol) // unbound is not allowed in this context -> invalid input
                                return null; // no error needed, already syntax error

                        serviceName = string.Empty;
                        serviceType = new TypeName(namedType);
                        hasAttribute = true;
                    }
                }
                else if (property.IsRequired) {
                    if (property.Type is not INamedTypeSymbol namedType)
                        continue;
                    foreach (ITypeSymbol argument in namedType.TypeArguments)
                        if (argument is IErrorTypeSymbol) // unbound is not allowed in this context -> invalid input
                            return null; // no error needed, already syntax error
                    if (property.SetMethod is null) {
                        errorManager.AddMissingSetAccessorError(property, baseType, property.ToDisplayString());
                        return null;
                    }

                    serviceName = string.Empty;
                    serviceType = new TypeName(namedType);
                    hasAttribute = false;
                }
                else
                    continue;

                propertyDependencyList.Add(new PropertyDependency() {
                    Name = property.Name,
                    ServiceName = serviceName,
                    ServiceType = serviceType,
                    HasAttribute = hasAttribute,
                    IsInit = property.SetMethod.IsInitOnly,
                    ImplementationBaseName = implementationBaseName,
                    IsRequired = property.IsRequired,
                });
            }
        }

        return propertyDependencyList;
    }
}
