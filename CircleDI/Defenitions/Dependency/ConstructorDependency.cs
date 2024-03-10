using Microsoft.CodeAnalysis;

namespace CircleDI;

/// <summary>
/// An Item in <see cref="Service.ConstructorDependencyList"/>,
/// </summary>
public sealed class ConstructorDependency : Dependency, IEquatable<ConstructorDependency> {
    /// <summary>
    /// Indicates if parameter has a "ref", "out", "in" or "ref readonly" modifier
    /// </summary>
    public required RefKind ByRef { get; init; }

    #region Equals

    public static bool operator ==(ConstructorDependency left, ConstructorDependency right) => left.Equals(right);

    public static bool operator !=(ConstructorDependency left, ConstructorDependency right) => !(left == right);

    public override bool Equals(object? obj)
        => obj switch {
            ConstructorDependency constructorDependency => Equals(constructorDependency),
            _ => false
        };

    public bool Equals(ConstructorDependency other) {
        if (ReferenceEquals(this, other))
            return true;

        if (ByRef != other.ByRef)
            return false;

        return base.Equals(other);
    }

    public override int GetHashCode() {
        return Combine(ByRef.GetHashCode(), base.GetHashCode());


        static int Combine(int h1, int h2) {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }

    #endregion
}

public static class RefKindExtension {
    /// <summary>
    /// <para>Maps <see cref="RefKind"/> to <see cref="string"/>.</para>
    /// <para>Actually <see cref="RefKind"/> should be a enum of string in the first place, but C# does not support that.</para>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string AsString(this RefKind value)
        => value switch {
            RefKind.None => string.Empty,
            RefKind.Ref or RefKind.RefReadOnlyParameter => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            RefKind.RefReadOnly or _ => throw new Exception($"Invalid enum Type '{nameof(RefKind)}': {value}")
        };
}
