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
public readonly struct TypeName : IEquatable<TypeName>, IComparable<TypeName> {
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
    /// <para>A open/unbound generic has the <see cref="Name"/> set, but the 3 lists are empty.</para>
    /// <para>If the list itself is empty, this type is not generic.</para>
    /// </summary>
    public required List<(TypeName typeArgument, bool isClosed)> TypeArgumentList { get; init; }


    public TypeName() { }

    [SetsRequiredMembers]
    public TypeName(string name) {
        Name = name;
        NameSpaceList = [];
        ContainingTypeList = [];
        TypeArgumentList = [];
    }

    [SetsRequiredMembers]
    public TypeName(string name, List<string> nameSpaceList, List<ContainingType> containingTypeList, List<(TypeName typeArgument, bool isClosed)> typeParameterList) {
        Name = name;
        NameSpaceList = nameSpaceList;
        ContainingTypeList = containingTypeList;
        TypeArgumentList = typeParameterList;
    }
    
    [SetsRequiredMembers]
    public TypeName(INamedTypeSymbol type) {
        Name = type.Name;
        NameSpaceList = type.GetNamespaceList();
        ContainingTypeList = type.GetContainingTypeList();

        TypeArgumentList = new List<(TypeName typeArgument, bool isClosed)>(type.TypeParameters.Length);
        foreach (ITypeSymbol typeArgument in type.TypeArguments)
            if (typeArgument is INamedTypeSymbol namedTypeArgument)
                TypeArgumentList.Add((new TypeName(namedTypeArgument), true));
            else
                TypeArgumentList.Add((new TypeName(typeArgument.Name), false));
    }

    /// <summary>
    /// Fills the <see cref="TypeArgumentList"/> with <see cref="INamedTypeSymbol.TypeParameters"/> insead of <see cref="INamedTypeSymbol.TypeArguments"/>.
    /// </summary>
    /// <param name="type"></param>
    public static TypeName CreateWithOpenGenerics(INamedTypeSymbol type) {
        string name = type.Name;
        List<string> nameSpaceList = type.GetNamespaceList();
        List<ContainingType> containingTypeList = type.GetContainingTypeList();

        List<(TypeName typeParameter, bool isClosed)> typeParameterList = new(type.TypeParameters.Length);
        foreach (ITypeParameterSymbol typeParameter in type.TypeParameters)
            if (typeParameter is INamedTypeSymbol namedTypeParameter)
                typeParameterList.Add((CreateWithOpenGenerics(namedTypeParameter), false));
            else
                typeParameterList.Add((new TypeName(typeParameter.Name), false));

        return new TypeName(name, nameSpaceList, containingTypeList, typeParameterList);
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

        int index = CreateString(str, 0, '{', '}', '.');
        extension.AsSpan().CopyTo(str[index..]);

        return str.ToString();
    }


    /// <summary>
    /// Returns the number of characters needed for <see cref="CreateString(Span{char}, int, char, char)"/>
    /// </summary>
    /// <returns></returns>
    private int GetCharCount() {
        int charCount = Name.Length;

        foreach (string namspace in NameSpaceList)
            charCount += namspace.Length;
        charCount += NameSpaceList.Count; // trailing '.'

        foreach (ContainingType containingType in ContainingTypeList)
            charCount += containingType.GetCharCount();
        charCount += ContainingTypeList.Count; // trailing '.'

        foreach ((TypeName typeArgument, bool isClosed) in TypeArgumentList) {
            charCount += typeArgument.GetCharCount();
            if (isClosed)
                charCount += 8;
        }
        charCount += 2 * TypeArgumentList.Count; // ", " * (Count - 1) + '<' + '>'

        return charCount;
    }

    /// <summary>
    /// Appends the string in the given Span at the given position and returns the new position.
    /// </summary>
    /// <param name="str"></param>
    /// <param name="index"></param>
    /// <param name="openGeneric"></param>
    /// <param name="closeGeneric"></param>
    /// <returns></returns>
    private int CreateString(Span<char> str, int index, char openGeneric = '<', char closeGeneric = '>', char globalDot = ':') {
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

        if (TypeArgumentList.Count > 0) {
            str[index++] = openGeneric;
            if (TypeArgumentList[0].isClosed) {
                "global".AsSpan().CopyTo(str[index..]);
                index += 6;
                str[index++] = globalDot;
                str[index++] = globalDot;
            }
            
            index = TypeArgumentList[0].typeArgument.CreateString(str, index, openGeneric, closeGeneric);
            for (int i = 1; i < TypeArgumentList.Count; i++) {
                str[index++] = ',';
                str[index++] = ' ';
                if (TypeArgumentList[i].isClosed) {
                    "global".AsSpan().CopyTo(str[index..]);
                    index += 6;
                    str[index++] = globalDot;
                    str[index++] = globalDot;
                }
                index = TypeArgumentList[i].typeArgument.CreateString(str, index, openGeneric, closeGeneric);
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

        if (!TypeArgumentList.SequenceEqual(other.TypeArgumentList))
            return false;

        return true;
    }

    public override int GetHashCode() {
        int hashCode = Name.GetHashCode();
        hashCode = CombineList(hashCode, NameSpaceList);
        hashCode = CombineList(hashCode, ContainingTypeList);
        hashCode = CombineList(hashCode, TypeArgumentList);
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


    public int CompareTo(TypeName other) {
        int nameLength = Name.Length.CompareTo(other.Name.Length);
        if (nameLength != 0)
            return nameLength;

        int name = Name.CompareTo(other.Name);
        if (name != 0)
            return name;


        int nameSpaceList = NameSpaceList.Count.CompareTo(other.NameSpaceList.Count);
        if (nameSpaceList != 0)
            return nameSpaceList;

        for (int i = NameSpaceList.Count - 1; i >= 0; i--) {
            int namespaceLength = NameSpaceList[i].Length.CompareTo(other.NameSpaceList[i].Length);
            if (namespaceLength != 0)
                return namespaceLength;

            int namspace = NameSpaceList[i].CompareTo(other.NameSpaceList[i]);
            if (namspace != 0)
                return namspace;
        }


        int containingTypeList = ContainingTypeList.Count.CompareTo(other.ContainingTypeList.Count);
        if (containingTypeList != 0)
            return containingTypeList;

        for (int i = ContainingTypeList.Count - 1; i >= 0; i--) {
            int containingType = ContainingTypeList[i].CompareTo(other.ContainingTypeList[i]);
            if (containingType != 0)
                return containingType;
        }


        int typeParameterListDiff = TypeArgumentList.Count.CompareTo(other.TypeArgumentList.Count);
        if (typeParameterListDiff != 0)
            return typeParameterListDiff;

        for (int i = 1; i < TypeArgumentList.Count; i++) {
            int typeNameDiff = TypeArgumentList[i].CompareTo(other.TypeArgumentList[i]);
            if (typeNameDiff != 0)
                return typeNameDiff;
        }


        return 0;
    }

    #endregion
}
