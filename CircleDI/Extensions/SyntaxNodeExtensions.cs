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
    /// <para>A list of namespace names the given type is located.</para>
    /// <para>
    /// The first item is the most inner namespace and the last item is the most outer namespace.<br />
    /// So, to construct a fully-qualified name this list should be iterated backwards.
    /// </para>
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <returns></returns>
    public static List<string> GetNamespaceList(this INamedTypeSymbol typeSymbol) {
        List<string> namcespaceList = [];

        INamespaceSymbol namespaceSymbol = typeSymbol.ContainingNamespace;
        while (namespaceSymbol.Name != string.Empty) {
            namcespaceList.Add(namespaceSymbol.Name);
            namespaceSymbol = namespaceSymbol.ContainingNamespace;
        }

        return namcespaceList;
    }

    /// <summary>
    /// <para>A list of all types (name and type) the given type is nested in.</para>
    /// <para>
    /// The first item is the most inner type and the last item is the most outer type.<br />
    /// So, to construct a fully-qualified name this list should be iterated backwards.
    /// </para>
    /// </summary>
    /// <param name="typeSymbol"></param>
    /// <returns></returns>
    public static List<ContainingType> GetContainingTypeList(this INamedTypeSymbol typeSymbol) {
        List<ContainingType> typeList = [];

        INamedTypeSymbol containingtypeSymbol = typeSymbol.ContainingType;
        while (containingtypeSymbol is not null) {
            typeList.Add(new ContainingType(containingtypeSymbol));
            containingtypeSymbol = containingtypeSymbol.ContainingType;
        }

        return typeList;
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
                        ServiceType = default,
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
}
