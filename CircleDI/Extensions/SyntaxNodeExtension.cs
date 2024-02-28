using Microsoft.CodeAnalysis;
using System.Collections.Immutable;

namespace CircleDI;

internal static class SyntaxNodeExtension {
    /// <summary>
    /// <para>Finds the attribute with the given name.</para>
    /// <para>If the given attribute is not present, it returns null.</para>
    /// </summary>
    /// <param name="symbol"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    internal static AttributeData? GetAttribute(this ISymbol symbol, string name) {
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
    internal static T? GetArgument<T>(this ImmutableArray<KeyValuePair<string, TypedConstant>> arguments, string name) {
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
    internal static bool HasInterface(this ITypeSymbol symbol, string name) {
        foreach (INamedTypeSymbol interfaceSymbol in symbol.AllInterfaces)
            if (interfaceSymbol.Name == name)
                return true;

        return false;
    }

    /// <summary>
    /// <para>Creates an array by mapping <see cref="IMethodSymbol.Parameters"/> to <see cref="ConstructorDependency"/>.</para>
    /// <para>
    /// Checks for attribute "DependencyAttribute" with argument "Name" -> [Dependency(Name = "...")]<br />
    /// If present, <see cref="IsNamed"/> is set to true and <see cref="ServiceIdentifier"/> is set to it's value.<br />
    /// Otherwise <see cref="IsNamed"/> is set to false, and <see cref="ServiceIdentifier"/> is set to parameter type.
    /// </para>
    /// </summary>
    /// <param name="methodSymbol"></param>
    /// <returns></returns>
    internal static ConstructorDependency[] CreateConstructorDependencyList(this IMethodSymbol methodSymbol) {
        ConstructorDependency[] result = new ConstructorDependency[methodSymbol.Parameters.Length];

        for (int i = 0; i < methodSymbol.Parameters.Length; i++)
            if (methodSymbol.Parameters[i].GetAttribute("DependencyAttribute") is AttributeData attributeData && attributeData.NamedArguments.GetArgument<string>("Name") is string dependencyName) {
                result[i] = new ConstructorDependency() {
                    IsNamed = true,
                    ServiceIdentifier = dependencyName
                };
            }
            else {
                result[i] = new ConstructorDependency() {
                    IsNamed = false,
                    ServiceIdentifier = methodSymbol.Parameters[i].Type.ToDisplayString()
                };
            }

        return result;
    }
}
