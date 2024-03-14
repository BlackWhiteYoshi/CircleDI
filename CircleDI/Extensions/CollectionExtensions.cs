namespace CircleDI;

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
    private static string GetFullyQualifiedName(this List<string> namespaceList, ReadOnlySpan<char> name) {
        int charCount = name.Length;
        foreach (string namspace in namespaceList)
            charCount += namspace.Length;
        charCount += namespaceList.Count; // number of '.'

        Span<char> result = charCount < 1024 ? stackalloc char[charCount] : new char[charCount];
        int index = 0;
        for (int i = namespaceList.Count - 1; i >= 0; i--) {
            namespaceList[i].AsSpan().CopyTo(result[index..]);
            index += namespaceList[i].Length;
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
    public static string GetFullyQualifiedName(this List<string> namespaceList, string name) => namespaceList.GetFullyQualifiedName(name.AsSpan());

    /// <summary>
    /// Creates the fully-qualified name by combining the given parameters with '.'
    /// </summary>
    /// <param name="name"></param>
    /// <returns></returns>
    public static string GetFullyQualifiedName(this List<string> namespaceList, string name, string nestedName) {
        int charCount = name.Length + nestedName.Length + 1;
        Span<char> input = charCount < 1024 ? stackalloc char[charCount] : new char[charCount];

        name.AsSpan().CopyTo(input);
        input[name.Length] = '.';
        nestedName.AsSpan().CopyTo(input[(name.Length + 1)..]);

        return namespaceList.GetFullyQualifiedName(input);
    }


    /// <summary>
    /// Determines whether two sequences are equal by comparing the elements by using the default equality comparer for their type.
    /// </summary>
    /// <typeparam name="T">The type of the elements of the input sequences.</typeparam>
    /// <param name="first"></param>
    /// <param name="second"></param>
    /// <returns>
    /// true if both are null or the two source sequences are of equal length and their corresponding elements are equal according to the default equality comparer for their type<br />
    /// false if one source sequence is null or the item length or an item in the sequence differs.
    /// </returns>
    public static bool SequenceNullEqual<T>(this IEnumerable<T>? first, IEnumerable<T>? second)
        => (first, second) switch {
            (null, null) => true,
            (not null, null) or (null, not null) => false,
            (not null, not null) => first.SequenceEqual(second)
        };
}
