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
}
