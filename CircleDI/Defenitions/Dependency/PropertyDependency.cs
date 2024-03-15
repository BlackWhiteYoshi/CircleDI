namespace CircleDI.Defenitions;

/// <summary>
/// An Item in <see cref="Service.PropertyDependencyList"/>,
/// </summary>
public sealed class PropertyDependency : Dependency, IEquatable<PropertyDependency> {
    /// <summary>
    /// <para>Has Property a readonly setter or a normal setter.</para>
    /// <para>
    /// True -> init<br />
    /// False -> set
    /// </para>
    /// </summary>
    public required bool IsInit { get; init; }

    /// <summary>
    /// Has Property the required keyword.
    /// </summary>
    public required bool IsRequired { get; init; }

    /// <summary>
    /// If true, this is a valid circular dependency.
    /// </summary>
    public bool IsCircular { get; set; } = false;


    #region Equals

    public static bool operator ==(PropertyDependency? left, PropertyDependency? right)
        => (left, right) switch {
            (null, null) => true,
            (null, not null) => false,
            (not null, _) => left.Equals(right)
        };

    public static bool operator !=(PropertyDependency? left, PropertyDependency? right) => !(left == right);

    public override bool Equals(object? obj) => Equals(obj as PropertyDependency);

    public bool Equals(PropertyDependency? other) {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        if (IsInit != other.IsInit)
            return false;
        if (IsRequired != other.IsRequired)
            return false;

        return base.Equals(other);
    }

    public override int GetHashCode() {
        int hashCode = Combine(IsInit.GetHashCode(), IsRequired.GetHashCode());
        return Combine(hashCode, base.GetHashCode());


        static int Combine(int h1, int h2) {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }

    #endregion
}
