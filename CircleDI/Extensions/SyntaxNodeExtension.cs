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
    /// <param name="constructor"></param>
    /// <returns></returns>
    internal static ConstructorDependency[] CreateConstructorDependencyList(this IMethodSymbol constructor) {
        ConstructorDependency[] result = new ConstructorDependency[constructor.Parameters.Length];

        for (int i = 0; i < constructor.Parameters.Length; i++)
            if (constructor.Parameters[i].GetAttribute("DependencyAttribute") is AttributeData attributeData)
                if (attributeData.NamedArguments.GetArgument<string>("Name") is string dependencyName)
                    result[i] = new ConstructorDependency() {
                        Name = constructor.Parameters[i].Name,
                        IsNamed = true,
                        ServiceIdentifier = dependencyName,
                        HasAttribute = false
                    };
                else
                    result[i] = new ConstructorDependency() {
                        Name = constructor.Parameters[i].Name,
                        IsNamed = false,
                        ServiceIdentifier = constructor.Parameters[i].Type.ToFullQualifiedName(),
                        HasAttribute = false
                    };
            else
                result[i] = new ConstructorDependency() {
                    Name = constructor.Parameters[i].Name,
                    IsNamed = false,
                    ServiceIdentifier = constructor.Parameters[i].Type.ToFullQualifiedName(),
                    HasAttribute = true
                };

        return result;
    }

    /// <summary>
    /// <para>
    /// It executes <see cref="ISymbol.ToDisplayString(SymbolDisplayFormat?)"/> and then maps built-in types from '<i>C# type keyword</i>' to '<i>.NET type</i>'<br />
    /// e.g. string -> System.String
    /// </para>
    /// <para>If it is not a '<i>C# type keyword</i>', it just returns the output of <see cref="ISymbol.ToDisplayString(SymbolDisplayFormat?)"/>.</para>
    /// </summary>
    /// <param name="typeSymbol"
    /// <returns></returns>
    internal static string ToFullQualifiedName(this ITypeSymbol typeSymbol) {
        string type = typeSymbol.ToDisplayString();

        return type switch {
            "bool" => "System.Boolean",
            "byte" => "System.Byte",
            "sbyte" => "System.SByte",
            "char" => "System.Char",
            "decimal" => "System.Decimal",
            "double" => "System.Double",
            "float" => "System.Single",
            "int" => "System.Int32",
            "uint" => "System.UInt32",
            "nint" => "System.IntPtr",
            "nuint" => "System.UIntPtr",
            "long" => "System.Int64",
            "ulong" => "System.UInt64",
            "short" => "System.Int16",
            "ushort" => "System.UInt16",

            "object" => "System.Object",
            "string" => "System.String",
            "dynamic" => "System.Object",

            _ => type
        };
    }
}
