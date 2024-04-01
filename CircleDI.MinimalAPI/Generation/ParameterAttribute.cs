using CircleDI.Defenitions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Diagnostics.CodeAnalysis;

namespace CircleDI.MinimalAPI.Generation;

/// <summary>
/// Represents an attribute of a parameter in an endpoint.<br />
/// It contains its name, parameters and properties.
/// </summary>
public readonly struct ParameterAttribute : IEquatable<ParameterAttribute> {
    /// <summary>
    /// The name/identifier of this Attribute.
    /// </summary>
    public required TypeName Name { get; init; }

    /// <summary>
    /// The constructor parameters.
    /// </summary>
    public string[] ParameterList { get; init; }

    /// <summary>
    /// The properties with name and value.
    /// </summary>
    public (string name, string value)[] PropertyList { get; init; }


    [SetsRequiredMembers]
    public ParameterAttribute(AttributeData attribute) {
        if (attribute.AttributeClass is not null)
            Name = new TypeName(attribute.AttributeClass);
        else
            Name = new TypeName("UNNAMED");

        ParameterList = new string[attribute.ConstructorArguments.Length];
        for (int i = 0; i < attribute.ConstructorArguments.Length; i++)
            ParameterList[i] = attribute.ConstructorArguments[i].ToCSharpString();

        PropertyList = new (string, string)[attribute.NamedArguments.Length];
        for (int i = 0; i < attribute.NamedArguments.Length; i++) {
            KeyValuePair<string, TypedConstant> keyValuePair = attribute.NamedArguments[i];
            PropertyList[i] = (keyValuePair.Key, keyValuePair.Value.ToCSharpString());
        }
    }


    #region Equals

    public static bool operator ==(ParameterAttribute left, ParameterAttribute right) => left.Equals(right);

    public static bool operator !=(ParameterAttribute left, ParameterAttribute right) => !(left == right);

    public override bool Equals(object? obj) =>
        obj switch {
            ParameterAttribute parameterAttribute => Equals(parameterAttribute),
            _ => false
        };

    public bool Equals(ParameterAttribute other) {
        if (Name != other.Name)
            return false;

        if (!ParameterList.SequenceEqual(other.ParameterList))
            return false;

        if (!PropertyList.SequenceEqual(other.PropertyList))
            return false;

        return true;
    }

    public override int GetHashCode() {
        int hashCode = Name.GetHashCode();
        hashCode = CombineList(hashCode, ParameterList);
        hashCode = CombineList(hashCode, PropertyList);
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
