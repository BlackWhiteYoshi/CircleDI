using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace CircleDI.Defenitions;

/// <summary>
/// <para>Datastructure holding all necessary information to construct the fully qualified name of a type or type declaration:</para>
/// <para>
/// - identifier/name<br />
/// - type keyword<br />
/// - list of namespaces<br />
/// - list of containing types<br />
/// - list of type parameters<br />
/// - list of type arguments
/// </para>
/// </summary>
public sealed class TypeName : IEquatable<TypeName>, IComparable<TypeName> {
    /// <summary>
    /// The name/identifier of this type
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The type of this type: class, struct, record, interface
    /// </summary>
    public required TypeKeyword Keyword { get; init; }

    /// <summary>
    /// <para>The namespace names this type is located.</para>
    /// <para>
    /// The first item is the most inner namespace and the last item is the most outer namespace.<br />
    /// So, to construct a fully-qualified name this list should be iterated backwards.
    /// </para>
    /// </summary>
    public required List<string> NameSpaceList { get; init; }

    /// <summary>
    /// <para>A list of all types this type is nested in.</para>
    /// <para>
    /// The first item is the most inner type and the last item is the most outer type.<br />
    /// So, to construct a fully-qualified name this list should be iterated backwards.
    /// </para>
    /// </summary>
    public required List<TypeName> ContainingTypeList { get; init; }

    /// <summary>
    /// <para>A list of all generic typeParameters (open generic) this type has.</para>
    /// <para>If the list itself is empty, this type is not generic.</para>
    /// </summary>
    public required List<string> TypeParameterList { get; init; }

    /// <summary>
    /// <para>A list of all generic arguments (closed generic) this type has.</para>
    /// <para>If the type is the same as the type at <see cref="TypeParameterList"/> with same index, the type is a open generic.</para>
    /// <para>If the list itself is empty, this type is not generic.</para>
    /// </summary>
    public required List<TypeName?> TypeArgumentList { get; init; }


    public TypeName() { }

    [SetsRequiredMembers]
    public TypeName(string name) {
        Name = name;
        Keyword = TypeKeyword.Class;
        NameSpaceList = [];
        ContainingTypeList = [];
        TypeParameterList = [];
        TypeArgumentList = [];
    }

    [SetsRequiredMembers]
    public TypeName(string name, TypeKeyword keyWord, List<string> nameSpaceList, List<TypeName> containingTypeList, List<string> typeParameterList, List<TypeName?> typeArgumentList) {
        Name = name;
        Keyword = keyWord;
        NameSpaceList = nameSpaceList;
        ContainingTypeList = containingTypeList;
        TypeParameterList = typeParameterList;
        TypeArgumentList = typeArgumentList;
    }

    [SetsRequiredMembers]
    public TypeName(INamedTypeSymbol typeSymbol) {
        Name = typeSymbol.Name;

        Keyword = (typeSymbol.IsRecord, typeSymbol.TypeKind) switch {
            (false, TypeKind.Class) => TypeKeyword.Class,
            (true, TypeKind.Class) => TypeKeyword.RecordClass,
            (false, TypeKind.Struct) => TypeKeyword.Struct,
            (true, TypeKind.Struct) => TypeKeyword.RecordStruct,
            (_, TypeKind.Interface) => TypeKeyword.Interface,
            _ => TypeKeyword.Unsupported
        };

        NameSpaceList = [];
        INamespaceSymbol namespaceSymbol = typeSymbol.ContainingNamespace;
        while (namespaceSymbol.Name != string.Empty) {
            NameSpaceList.Add(namespaceSymbol.Name);
            namespaceSymbol = namespaceSymbol.ContainingNamespace;
        }

        ContainingTypeList = [];
        INamedTypeSymbol containingtypeSymbol = typeSymbol.ContainingType;
        while (containingtypeSymbol is not null) {
            ContainingTypeList.Add(new TypeName(containingtypeSymbol));
            containingtypeSymbol = containingtypeSymbol.ContainingType;
        }

        TypeParameterList = new List<string>(typeSymbol.TypeParameters.Length);
        TypeArgumentList = new List<TypeName?>(typeSymbol.TypeArguments.Length);
        for (int i = 0; i < typeSymbol.TypeParameters.Length; i++)
            switch (typeSymbol.TypeArguments[i]) {
                case ITypeParameterSymbol:
                    // TypeArgument is a forwarded type parameter
                    TypeParameterList.Add(typeSymbol.TypeArguments[i].Name);
                    TypeArgumentList.Add(null);
                    break;
                case IErrorTypeSymbol namedTypeArgument: // IErrorTypeSymbol : INamedTypeSymbol
                    // unbound type parameter or invalid input
                    TypeParameterList.Add(typeSymbol.TypeParameters[i].Name);
                    TypeArgumentList.Add(null);
                    break;
                case INamedTypeSymbol namedTypeArgument:
                    // closed type parameter
                    TypeParameterList.Add(typeSymbol.TypeParameters[i].Name);
                    TypeArgumentList.Add(new TypeName(namedTypeArgument));
                    break;
                default:
                    throw new Exception($"Unreachable: typeParameter is not a 'ITypeParameterSymbol' nor a 'INamedTypeSymbol': {typeSymbol.TypeArguments[i].GetType()}");
            }
    }


    #region Equals

    public static bool operator ==(TypeName? left, TypeName? right)
         => (left, right) switch {
             (null, null) => true,
             (null, not null) => false,
             (not null, _) => left.Equals(right)
         };

    public static bool operator !=(TypeName? left, TypeName? right) => !(left == right);

    public override bool Equals(object? obj) => Equals(obj as TypeName);

    public bool Equals(TypeName? other) {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        if (Name != other.Name)
            return false;

        if (Keyword != other.Keyword)
            return false;

        if (!NameSpaceList.SequenceEqual(other.NameSpaceList))
            return false;

        if (!ContainingTypeList.SequenceEqual(other.ContainingTypeList))
            return false;

        // When type parameters differ, it is still the same Type, only arity matters
        if (TypeParameterList.Count != other.TypeParameterList.Count)
            return false;

        if (!TypeArgumentList.SequenceEqual(other.TypeArgumentList))
            return false;

        return true;
    }

    public override int GetHashCode() {
        int hashCode = Name.GetHashCode();
        hashCode = Combine(hashCode, Keyword.GetHashCode());

        hashCode = CombineList(hashCode, NameSpaceList);
        hashCode = CombineList(hashCode, ContainingTypeList);
        hashCode = Combine(hashCode, TypeParameterList.Count.GetHashCode());

        foreach (TypeName? typeName in TypeArgumentList)
            hashCode = Combine(hashCode, typeName?.GetHashCode() ?? 0);

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


        // When type parameters differ, it is still the same Type, only arity matters
        int typeParameterListDiff = TypeParameterList.Count.CompareTo(other.TypeParameterList.Count);
        if (typeParameterListDiff != 0)
            return typeParameterListDiff;


        int typeArgumentListDiff = TypeArgumentList.Count.CompareTo(other.TypeArgumentList.Count);
        if (typeArgumentListDiff != 0)
            return typeArgumentListDiff;

        for (int i = 0; i < TypeArgumentList.Count; i++)
            switch ((TypeArgumentList[i], other.TypeArgumentList[i])) {
                case (null, null):
                    break;
                case (not null, null) or (null, not null):
                    // allow match of closed generic with open/unbound generic
                    break;
                case (not null, not null):
                    int typeNameDiff = TypeArgumentList[i]!.CompareTo(other.TypeArgumentList[i]!);
                    if (typeNameDiff != 0)
                        return typeNameDiff;
                    break;
            }


        return 0;
    }

    #endregion
}
