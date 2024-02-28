namespace CircleDI;

/// <summary>
/// Base type for <see cref="ConstructorDependency"/> and <see cref="PropertyDependency"/>.
/// </summary>
public abstract class Dependency {
    /// <summary>
    /// The Service that will be used to satisfy the dependency.
    /// </summary>
    public Service? Service { get; set; }

    /// <summary>
    /// <para>Whether the service is mapped by name or by type.</para>
    /// <para>
    /// True => <see cref="ServiceIdentifier"/> contains the name of the service.
    /// False => <see cref="ServiceIdentifier"/> contains the type of the service.
    /// </para>
    /// </summary>
    public required bool IsNamed { get; init; }

    /// <summary>
    /// The Identifier of the service. Can be either the name or the type of the service.<br />
    /// <see cref="IsNamed"/> specifies if this field contains the name or the type.
    /// </summary>
    public required string ServiceIdentifier { get; init; }


    #region Equals

    protected bool Equals(Dependency other) {
        if (IsNamed != other.IsNamed)
            return false;
        
        if (ServiceIdentifier != other.ServiceIdentifier)
            return false;

        return true;
    }

    public override int GetHashCode() {
        return Combine(IsNamed.GetHashCode(), ServiceIdentifier.GetHashCode());


        static int Combine(int h1, int h2) {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }

    #endregion
}
