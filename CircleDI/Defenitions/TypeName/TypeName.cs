using CircleDI.Extensions;
using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace CircleDI.Defenitions;

/// <summary>
/// <para>Datastructure holding all necessary information to construct the fully qualified name of a type:</para>
/// <para>
/// - list of namespaces<br />
/// - list of containing types<br />
/// - identifier/name<br />
/// - list of type parameters
/// </para>
/// </summary>
public readonly struct TypeName : IEquatable<TypeName> {
    /// <summary>
    /// The name/identifier of this type
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// <para>The namespace names this type is located.</para>
    /// <para>
    /// The first item is the most inner namespace and the last item is the most outer namespace.<br />
    /// So, to construct a fully-qualified name this list should be iterated backwards.
    /// </para>
    /// </summary>
    public required List<string> NameSpaceList { get; init; }

    /// <summary>
    /// <para>A list of all types (name and type) this type is nested in.</para>
    /// <para>
    /// The first item is the most inner type and the last item is the most outer type.<br />
    /// So, to construct a fully-qualified name this list should be iterated backwards.
    /// </para>
    /// </summary>
    public required List<ContainingType> ContainingTypeList { get; init; }

    /// <summary>
    /// <para>A list of all generic arguments this type has.</para>
    /// <para>A open generic has the <see cref="Name"/> set, but the 3 lists are empty.</para>
    /// <para>A unbound generic is <see cref="Name"/> an empty string and the 3 lists are empty.</para>
    /// <para>If the list itself is empty, this type is not generic.</para>
    /// </summary>
    public required List<TypeName> TypeParameterList { get; init; }

    /// <summary>
    /// <para>Indicates if this type is an unbound generic or not.</para>
    /// <para>
    /// closed generic -> List&lt;int&gt;<br />
    /// open generic -> List&lt;T&gt;<br />
    /// unbound generic -> List&lt;&gt;
    /// </para>
    /// </summary>
    public bool IsUnbound => Name == string.Empty;


    public TypeName() { }

    [SetsRequiredMembers]
    public TypeName(string name) {
        Name = name;
        NameSpaceList = [];
        ContainingTypeList = [];
        TypeParameterList = [];
    }

    [SetsRequiredMembers]
    public TypeName(string name, List<string> nameSpaceList, List<ContainingType> containingTypeList, List<TypeName> typeParameterList) {
        Name = name;
        NameSpaceList = nameSpaceList;
        ContainingTypeList = containingTypeList;
        TypeParameterList = typeParameterList;
    }
    
    [SetsRequiredMembers]
    public TypeName(INamedTypeSymbol type) {
        Name = type.Name;
        NameSpaceList = type.GetNamespaceList();
        ContainingTypeList = type.GetContainingTypeList();

        TypeParameterList = new List<TypeName>(type.TypeParameters.Length);
        foreach (ITypeParameterSymbol typeParameter in type.TypeParameters)
            if (typeParameter is INamedTypeSymbol namedTypeParameter)
                TypeParameterList.Add(new TypeName(namedTypeParameter));
            else
                TypeParameterList.Add(new TypeName(typeParameter.Name));
    }


    #region FullyQualifiedName

    /// <summary>
    /// Creates the fully qualified name:<br />
    /// <see cref="NameSpaceList">NameSpaceList</see>.<see cref="ContainingTypeList">ContainingTypeList</see>.<see cref="Name">Name</see>&lt;<see cref="TypeParameterList">TypeParameterList</see>&gt;
    /// </summary>
    /// <returns></returns>
    public string CreateFullyQualifiedName() {
        int charCount = GetCharCount();
        Span<char> str = charCount < 1024 ? stackalloc char[charCount] : new char[charCount];

        CreateString(str, 0);
        
        return str.ToString();
    }

    /// <summary>
    /// Creates the fully qualified name, but '&lt;' and '&gt;' are replaced with '{' and '}' and the given extension is appended.
    /// </summary>
    /// <param name="extension">ending of the hintName, typically it is ".g.cs"</param>
    /// <returns></returns>
    public string CreateHintName(string extension) {
        int charCount = GetCharCount() + extension.Length;
        Span<char> str = charCount < 1024 ? stackalloc char[charCount] : new char[charCount];

        int index = CreateString(str, 0, '{', '}');
        extension.AsSpan().CopyTo(str[index..]);

        return str.ToString();
    }


    private int GetCharCount() {
        int charCount = Name.Length;

        foreach (string namspace in NameSpaceList)
            charCount += namspace.Length;
        charCount += NameSpaceList.Count; // trailing '.'

        foreach (ContainingType containingType in ContainingTypeList)
            charCount += containingType.GetCharCount();
        charCount += ContainingTypeList.Count; // trailing '.'

        foreach (TypeName typeParameter in TypeParameterList)
            charCount += typeParameter.GetCharCount();
        charCount += 2 * TypeParameterList.Count; // after each item ", ", subtracting omitting last one (-2), adding '<' and '>' (+2)

        return charCount;
    }

    private int CreateString(Span<char> str, int index, char openGeneric = '<', char closeGeneric = '>') {
        for (int i = NameSpaceList.Count - 1; i >= 0; i--) {
            NameSpaceList[i].AsSpan().CopyTo(str[index..]);
            index += NameSpaceList[i].Length;
            str[index++] = '.';
        }

        for (int i = ContainingTypeList.Count - 1; i >= 0; i--) {
            index = ContainingTypeList[i].CreateString(str, index, openGeneric, closeGeneric);
            str[index++] = '.';
        }

        Name.AsSpan().CopyTo(str[index..]);
        index += Name.Length;

        if (TypeParameterList.Count > 0) {
            str[index++] = openGeneric;
            
            index = TypeParameterList[0].CreateString(str, index, openGeneric, closeGeneric);
            for (int i = 1; i < TypeParameterList.Count; i++) {
                str[index++] = ',';
                str[index++] = ' ';
                index = TypeParameterList[i].CreateString(str, index, openGeneric, closeGeneric);
            }
            
            str[index++] = closeGeneric;
        }

        return index;
    }

    #endregion


    #region Equals

    public static bool operator ==(TypeName left, TypeName right) => left.Equals(right);

    public static bool operator !=(TypeName left, TypeName right) => !(left == right);

    public override bool Equals(object? obj)
        => obj switch {
            TypeName typeName => Equals(typeName),
            _ => false
        };

    public bool Equals(TypeName other) {
        if (Name != other.Name)
            return false;

        if (!NameSpaceList.SequenceEqual(other.NameSpaceList))
            return false;

        if (!ContainingTypeList.SequenceEqual(other.ContainingTypeList))
            return false;

        if (!TypeParameterList.SequenceEqual(other.TypeParameterList))
            return false;

        return true;
    }

    public override int GetHashCode() {
        int hashCode = Name.GetHashCode();
        hashCode = CombineList(hashCode, NameSpaceList);
        hashCode = CombineList(hashCode, ContainingTypeList);
        hashCode = CombineList(hashCode, TypeParameterList);
        return hashCode;


        static int CombineList<T>(int hashCode, IEnumerable<T> list) where T : notnull {
            foreach (T item in list)
                hashCode = Combine(hashCode, item.GetHashCode());
            return hashCode;
        }

        static int Combine(int h1, int h2) {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }

    #endregion
}
