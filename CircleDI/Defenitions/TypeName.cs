﻿using Microsoft.CodeAnalysis;
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
    public required List<TypeName> TypeParameterList { get; init; }

    /// <summary>
    /// <para>A list of all generic arguments (closed generic) this type has.</para>
    /// <para>If the type is the same as the type at <see cref="TypeParameterList"/> with same index, the type is a open generic.</para>
    /// <para>If the list itself is empty, this type is not generic.</para>
    /// </summary>
    public required List<TypeName> TypeArgumentList { get; init; }


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
    public TypeName(string name, TypeKeyword keyWord, List<string> nameSpaceList, List<TypeName> containingTypeList, List<TypeName> typeParameterList, List<TypeName> typeArgumentList) {
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

        TypeParameterList = new List<TypeName>(typeSymbol.TypeParameters.Length);
        foreach (ITypeParameterSymbol typeParameter in typeSymbol.TypeParameters)
            if (typeParameter is INamedTypeSymbol namedTypeArgument)
                TypeParameterList.Add(new TypeName(namedTypeArgument));
            else
                TypeParameterList.Add(new TypeName(typeParameter.Name));

        TypeArgumentList = new List<TypeName>(typeSymbol.TypeArguments.Length);
        foreach (ITypeSymbol typeArgument in typeSymbol.TypeArguments)
            if (typeArgument is INamedTypeSymbol namedTypeArgument)
                TypeArgumentList.Add(new TypeName(namedTypeArgument));
            else
                TypeArgumentList.Add(new TypeName(typeArgument.Name));
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

        if (!TypeParameterList.SequenceEqual(other.TypeParameterList))
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
        hashCode = CombineList(hashCode, TypeParameterList);
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


        int typeParameterListDiff = TypeParameterList.Count.CompareTo(other.TypeParameterList.Count);
        if (typeParameterListDiff != 0)
            return typeParameterListDiff;

        for (int i = 1; i < TypeParameterList.Count; i++) {
            int typeNameDiff = TypeParameterList[i].CompareTo(other.TypeParameterList[i]);
            if (typeNameDiff != 0)
                return typeNameDiff;
        }


        int typeArgumentListDiff = TypeArgumentList.Count.CompareTo(other.TypeArgumentList.Count);
        if (typeArgumentListDiff != 0)
            return typeArgumentListDiff;

        for (int i = 1; i < TypeArgumentList.Count; i++) {
            int typeNameDiff = TypeArgumentList[i].CompareTo(other.TypeArgumentList[i]);
            if (typeNameDiff != 0)
                return typeNameDiff;
        }


        return 0;
    }

    #endregion
}
