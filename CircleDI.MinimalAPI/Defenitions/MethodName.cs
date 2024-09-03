using Microsoft.CodeAnalysis;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace CircleDI.MinimalAPI.Defenitions;

/// <summary>
/// <para>Datastructure holding all necessary information to construct the fully qualified name of a static, non-generic method:</para>
/// <para>
/// - identifier/name<br />
/// - list of namespaces<br />
/// - list of containing types
/// </para>
/// </summary>
public sealed class MethodName : IEquatable<MethodName> {
    /// <summary>
    /// The name/identifier of this method.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// <para>The namespace names this method is located.</para>
    /// <para>
    /// The first item is the most inner namespace and the last item is the most outer namespace.<br />
    /// So, to construct a fully-qualified name this list should be iterated backwards.
    /// </para>
    /// </summary>
    public required List<string> NameSpaceList { get; init; }

    /// <summary>
    /// <para>A list of all types this method is nested in.</para>
    /// <para>
    /// The first item is the most inner type and the last item is the most outer type.<br />
    /// So, to construct a fully-qualified name this list should be iterated backwards.
    /// </para>
    /// <para>Generics are not allowed.</para>
    /// </summary>
    public required List<string> ContainingTypeList { get; init; }


    public MethodName() { }

    [SetsRequiredMembers]
    public MethodName(string name, string className) {
        Name = name;
        NameSpaceList = [];
        ContainingTypeList = [className];
    }

    [SetsRequiredMembers]
    public MethodName(string name, List<string> nameSpaceList, List<string> containingTypeList) {
        Name = name;
        NameSpaceList = nameSpaceList;
        ContainingTypeList = containingTypeList;
    }

    [SetsRequiredMembers]
    public MethodName(IMethodSymbol methodSymbol) {
        Name = methodSymbol.Name;

        NameSpaceList = [];
        INamespaceSymbol namespaceSymbol = methodSymbol.ContainingNamespace;
        while (namespaceSymbol.Name != string.Empty) {
            NameSpaceList.Add(namespaceSymbol.Name);
            namespaceSymbol = namespaceSymbol.ContainingNamespace;
        }

        ContainingTypeList = [];
        INamedTypeSymbol containingtypeSymbol = methodSymbol.ContainingType;
        while (containingtypeSymbol is not null) {
            ContainingTypeList.Add(containingtypeSymbol.Name);
            containingtypeSymbol = containingtypeSymbol.ContainingType;
        }
    }


    /// <summary>
    /// Appends fully qualified method name:<br />
    /// "{namespace1}.{namespaceN}.{containingType1}.{containingTypeN}.{name}"
    /// </summary>
    /// <param name="builder"></param>
    public void AppendFullyQualifiedName(StringBuilder builder) {
        for (int i = NameSpaceList.Count - 1; i >= 0; i--) {
            builder.Append(NameSpaceList[i]);
            builder.Append('.');
        }

        for (int i = ContainingTypeList.Count - 1; i >= 0; i--) {
            builder.Append(ContainingTypeList[i]);
            builder.Append('.');
        }

        builder.Append(Name);
    }


    #region Equals

    public static bool operator ==(MethodName? left, MethodName? right)
         => (left, right) switch {
             (null, null) => true,
             (null, not null) => false,
             (not null, _) => left.Equals(right)
         };

    public static bool operator !=(MethodName? left, MethodName? right) => !(left == right);

    public override bool Equals(object? obj) => Equals(obj as MethodName);

    public bool Equals(MethodName? other) {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        if (Name != other.Name)
            return false;

        if (!NameSpaceList.SequenceEqual(other.NameSpaceList))
            return false;

        if (!ContainingTypeList.SequenceEqual(other.ContainingTypeList))
            return false;

        return true;
    }

    public override int GetHashCode() {
        int hashCode = Name.GetHashCode();
        hashCode = CombineList(hashCode, NameSpaceList);
        hashCode = CombineList(hashCode, ContainingTypeList);
        return hashCode;


        static int CombineList(int hashCode, List<string> list) {
            foreach (string item in list)
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
