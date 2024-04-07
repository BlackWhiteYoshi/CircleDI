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
    /// Creates the ConstructorDependencyList by analyzing the available constructors at the given class/implementation.<br />
    /// When an applicable constructor is found, the list will be created based on that constructor, otherwise an error will be created and the list will be empty.
    /// </summary>
    /// <param name="implementation"></param>
    /// <param name="attributeData"></param>
    /// <returns></returns>
    public static (IMethodSymbol? constructor, Diagnostic? error) FindConstructor(this INamedTypeSymbol implementation, AttributeData attributeData) {
        switch (implementation.InstanceConstructors.Length) {
            case 0:
                return (null, attributeData.CreateMissingClassOrConstructorError(implementation.ToDisplayString()));
            case 1:
                if (implementation.InstanceConstructors[0].DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
                    return (null, attributeData.CreateMissingClassOrConstructorError(implementation.ToDisplayString()));
                return (implementation.InstanceConstructors[0], null);
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
                            return (null, firstAttribute.CreateMultipleConstructorAttributesError(secondAttribute, implementation, implementation.ToDisplayString()));
                        }
                }

                switch (availableConstructors) {
                    case 0:
                        return (null, attributeData.CreateMissingClassOrConstructorError(implementation.ToDisplayString()));
                    case 1:
                        return (lastAvailableConstructor, null);
                    default:
                        if (attributeConstructor is null)
                            return (null, attributeData.CreateMissingConstructorAttributesError(implementation, implementation.ToDisplayString()));
                        else
                            return (attributeConstructor, null);
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

        foreach (IParameterSymbol parameter in constructor.Parameters)
            if (parameter.GetAttribute("DependencyAttribute") is AttributeData attributeData)
                if (attributeData.NamedArguments.GetArgument<string>("Name") is string dependencyName)
                    result.Add(new ConstructorDependency() {
                        Name = parameter.Name,
                        ServiceName = dependencyName,
                        ServiceType = null,
                        HasAttribute = true,
                        ByRef = parameter.RefKind
                    });
                else {
                    if (parameter.Type is not INamedTypeSymbol namedType)
                        continue;

                    result.Add(new ConstructorDependency() {
                        Name = parameter.Name,
                        ServiceName = string.Empty,
                        ServiceType = new TypeName(namedType),
                        HasAttribute = true,
                        ByRef = parameter.RefKind
                    });
                }
            else {
                if (parameter.Type is not INamedTypeSymbol namedType)
                    continue;

                result.Add(new ConstructorDependency() {
                    Name = parameter.Name,
                    ServiceName = string.Empty,
                    ServiceType = new TypeName(namedType),
                    HasAttribute = false,
                    ByRef = parameter.RefKind
                });
            }

        return result;
    }

    /// <summary>
    /// <para>First <see cref="FindConstructor(INamedTypeSymbol, AttributeData)">finds the constructor</see> and if found, <see cref="Extensions.SyntaxNodeExtensions.CreateConstructorDependencyList(IMethodSymbol)">creates the constructor dependency list.</see></para>
    /// <para>If no constructor found or an invalid constructor dependency was found, an empty list and an error is returned.</para>
    /// </summary>
    /// <param name="implementation"></param>
    /// <param name="attributeData"></param>
    /// <returns></returns>
    public static (List<ConstructorDependency> constructorDependencyList, Diagnostic? error) CreateConstructorDependencyList(this INamedTypeSymbol implementation, AttributeData attributeData) {
        (IMethodSymbol? constructor, Diagnostic? constructorListError) = FindConstructor(implementation, attributeData);
        if (constructor is not null)
            return (CreateConstructorDependencyList(constructor), null);
        else
            return ([], constructorListError);
    }


    /// <summary>
    /// Creates the PropertyDependencyList based on the given class/implementation.<br />
    /// Each property marked with 'required' or '[Dependency]' will be added to the list.
    /// If one of these properties have no set/init accessor, then an error will be created and the list will be empty.
    /// </summary>
    /// <param name="implementation"></param>
    /// <param name="attributeData"></param>
    /// <returns></returns>
    public static (List<PropertyDependency> propertyDependencyList, Diagnostic? error) CreatePropertyDependencyList(this INamedTypeSymbol implementation, AttributeData attributeData) {
        List<PropertyDependency> propertyDependencyList = [];

        for (INamedTypeSymbol? baseType = implementation; baseType is not null; baseType = baseType.BaseType)
            foreach (ISymbol member in baseType.GetMembers()) {
                if (member is not IPropertySymbol { Name.Length: > 0 } property)
                    continue;

                string serviceName;
                TypeName? serviceType;
                bool hasAttribute;
                if (property.GetAttribute("DependencyAttribute") is AttributeData propertyAttribute) {
                    if (property.SetMethod is null)
                        return ([], attributeData.CreateMissingSetAccessorError(property, baseType, property.ToDisplayString()));

                    if (propertyAttribute.NamedArguments.GetArgument<string>("Name") is string dependencyName) {
                        serviceName = dependencyName;
                        serviceType = null;
                        hasAttribute = true;
                    }
                    else {
                        if (property.Type is not INamedTypeSymbol namedType)
                            continue;

                        serviceName = string.Empty;
                        serviceType = new TypeName(namedType);
                        hasAttribute = true;
                    }
                }
                else if (property.IsRequired) {
                    if (property.Type is not INamedTypeSymbol namedType)
                        continue;
                    if (property.SetMethod is null)
                        return ([], attributeData.CreateMissingSetAccessorError(property, baseType, property.ToDisplayString()));

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
                    IsRequired = property.IsRequired,
                });
            }

        return (propertyDependencyList, null);
    }
}
