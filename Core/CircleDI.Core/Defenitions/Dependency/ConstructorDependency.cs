using Microsoft.CodeAnalysis;

namespace CircleDI.Defenitions;

/// <summary>
/// An Item in <see cref="Generation.Service.ConstructorDependencyList"/>,
/// </summary>
public sealed class ConstructorDependency : Dependency, IEquatable<ConstructorDependency> {
    /// <summary>
    /// Indicates if parameter has a "ref", "out", "in" or "ref readonly" modifier
    /// </summary>
    public required RefKind ByRef { get; init; }

    #region Equals

    public static bool operator ==(ConstructorDependency? left, ConstructorDependency? right)
        => (left, right) switch {
            (null, null) => true,
            (null, not null) => false,
            (not null, _) => left.Equals(right)
        };

    public static bool operator !=(ConstructorDependency? left, ConstructorDependency? right) => !(left == right);

    public override bool Equals(object? obj) => Equals(obj as ConstructorDependency);

    public bool Equals(ConstructorDependency? other) {
        if (other is null)
            return false;
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
