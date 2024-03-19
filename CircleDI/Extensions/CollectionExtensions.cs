using Microsoft.CodeAnalysis;

namespace CircleDI.Extensions;

/// <summary>
/// Extension methods on types that implement <see cref="IEnumerable{T}"/>.
/// </summary>
public static class CollectionExtensions {
    /// <summary>
    /// Creates the fully-qualified name by combining the given parameters with '.'
    /// </summary>
    /// <param name="namespaceList"></param>
    /// <param name="name"></param>
    /// <returns></returns>
    private static string GetFullyQualifiedName(this ReadOnlySpan<char> name, List<string> namespaceList, List<(string name, TypeKind type)> containingTypeList) {
        int charCount = name.Length;
        foreach (string namspace in namespaceList)
            charCount += namspace.Length;
        foreach ((string containingType, _) in containingTypeList)
            charCount += containingType.Length;
        charCount += namespaceList.Count + containingTypeList.Count; // number of '.'

        Span<char> result = charCount < 1024 ? stackalloc char[charCount] : new char[charCount];
        int index = 0;
        for (int i = namespaceList.Count - 1; i >= 0; i--) {
            namespaceList[i].AsSpan().CopyTo(result[index..]);
            index += namespaceList[i].Length;
            result[index] = '.';
            index++;
        }
        for (int i = containingTypeList.Count - 1; i >= 0; i--) {
            containingTypeList[i].name.AsSpan().CopyTo(result[index..]);
            index += containingTypeList[i].name.Length;
            result[index] = '.';
            index++;
        }
        name.CopyTo(result[index..]);

        return result.ToString();
    }

    /// <summary>
    /// Creates the fully-qualified name by combining the given parameters with '.'
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string GetFullyQualifiedName(this string name, List<string> namespaceList, List<(string, TypeKind)> containingTypeList) => GetFullyQualifiedName(name.AsSpan(), namespaceList, containingTypeList);

    /// <summary>
    /// Creates the fully-qualified name by combining the given parameters with '.'
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string GetFullyQualifiedName(this string name, string nestedName, List<string> namespaceList, List<(string, TypeKind)> containingTypeList) {
        int charCount = name.Length + nestedName.Length + 1;
        Span<char> input = charCount < 1024 ? stackalloc char[charCount] : new char[charCount];

        name.AsSpan().CopyTo(input);
        input[name.Length] = '.';
        nestedName.AsSpan().CopyTo(input[(name.Length + 1)..]);

        return GetFullyQualifiedName(input, namespaceList, containingTypeList);
    }
}
