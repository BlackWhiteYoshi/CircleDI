using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;

namespace CircleDI.Defenitions;

public readonly struct ContainingType : IEquatable<ContainingType> {
    /// <summary>
    /// The name/identifier of this type
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The type of this type: class, struct, record, interface
    /// </summary>
    public required TypeKeyword Keyword { get; init; }

    /// <summary>
    /// <para>A list of all generic arguments this type has.</para>
    /// <para>If a member in the list is null, it is an unbound generic e.g. "List&lt;&gt;"</para>
    /// <para>If list is empty, this type is not generic.</para>
    /// </summary>
    public required List<string> TypeParameterList { get; init; }


    public ContainingType() { }

    [SetsRequiredMembers]
    public ContainingType(string name, TypeKeyword keyword, List<string> typeParameterList) {
        Name = name;
        Keyword = keyword;
        TypeParameterList = typeParameterList;
    }

    [SetsRequiredMembers]
    public ContainingType(INamedTypeSymbol typeSymbol) {
        Name = typeSymbol.Name;
        Keyword = (typeSymbol.IsRecord, typeSymbol.TypeKind) switch {
            (false, TypeKind.Class) => TypeKeyword.Class,
            (true, TypeKind.Class) => TypeKeyword.RecordClass,
            (false, TypeKind.Struct) => TypeKeyword.Struct,
            (true, TypeKind.Struct) => TypeKeyword.RecordStruct,
            (_, TypeKind.Interface) => TypeKeyword.Interface,
            _ => throw new ArgumentException($"typeSymbol is not a class or struct: {typeSymbol.Name}", nameof(typeSymbol))
        };
        TypeParameterList = new List<string>(typeSymbol.TypeParameters.Length);
        foreach (ITypeParameterSymbol typeParameter in typeSymbol.TypeParameters)
            TypeParameterList.Add(typeParameter.Name);
    }


    public int GetCharCount() {
        int charCount = Name.Length;

        foreach (string typeParameter in TypeParameterList)
            charCount += typeParameter.Length;
        charCount += 2 * TypeParameterList.Count; // after each item ", ", subtracting omitting last one (-2), adding '<' and '>' (+2)

        return charCount;
    }

    public int CreateString(Span<char> str, int index, char openGeneric = '<', char closeGeneric = '>') {
        Name.AsSpan().CopyTo(str[index..]);
        index += Name.Length;

        if (TypeParameterList.Count > 0) {
            str[index++] = openGeneric;

            TypeParameterList[0].AsSpan().CopyTo(str[index..]);
            index += TypeParameterList[0].Length;
            for (int i = 1; i < TypeParameterList.Count; i++) {
                str[index++] = ',';
                str[index++] = ' ';
                TypeParameterList[i].AsSpan().CopyTo(str[index..]);
                index += TypeParameterList[i].Length;
            }

            str[index++] = closeGeneric;
        }

        return index;
    }


    #region Equals

    public static bool operator ==(ContainingType left, ContainingType right) => left.Equals(right);

    public static bool operator !=(ContainingType left, ContainingType right) => !(left == right);

    public override bool Equals(object? obj)
        => obj switch {
            ContainingType containingType => Equals(containingType),
            _ => false
        };

    public bool Equals(ContainingType other) {
        if (Name != other.Name)
            return false;

        if (Keyword != other.Keyword)
            return false;

        if (!TypeParameterList.SequenceEqual(other.TypeParameterList))
            return false;

        return true;
    }

    public override int GetHashCode() {
        int hashCode = Name.GetHashCode();

        hashCode = Combine(hashCode, Keyword.GetHashCode());

        foreach (string typeName in TypeParameterList)
            hashCode = Combine(hashCode, typeName.GetHashCode());

        return hashCode;


        static int Combine(int h1, int h2) {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }

    #endregion
}
