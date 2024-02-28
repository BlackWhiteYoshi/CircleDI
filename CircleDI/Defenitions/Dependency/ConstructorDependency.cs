namespace CircleDI;

/// <summary>
/// An Item in <see cref="Service.ConstructorDependencyList"/>,
/// </summary>
public sealed class ConstructorDependency : Dependency, IEquatable<ConstructorDependency> {
    #region Equals

    public static bool operator ==(ConstructorDependency left, ConstructorDependency right) => left.Equals(right);

    public static bool operator !=(ConstructorDependency left, ConstructorDependency right) => !(left == right);

    public override bool Equals(object? obj) {
        if (obj is not ConstructorDependency constructorDependency)
            return false;

        return Equals(constructorDependency);
    }

    public bool Equals(ConstructorDependency other) {
        if (ReferenceEquals(this, other))
            return true;

        return base.Equals(other);
    }

    public override int GetHashCode() => base.GetHashCode();

    #endregion
}
