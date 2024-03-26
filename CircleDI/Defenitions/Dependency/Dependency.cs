using CircleDI.Generation;

namespace CircleDI.Defenitions;

/// <summary>
/// Base type for <see cref="ConstructorDependency"/> and <see cref="PropertyDependency"/>.
/// </summary>
public abstract class Dependency {
    /// <summary>
    /// The Service that will be used to satisfy the dependency.
    /// </summary>
    public Service? Service { get; set; }


    /// <summary>
    /// Name of the Parameter/Property
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// If the service is mapped by name this contains the name.<br />
    /// If the service is mapped by type this is an empty string.
    /// </summary>
    public required string ServiceName { get; init; }

    /// <summary>
    /// The type of the service.<br />
    /// Should only be used when <see cref="ServiceName"/> is empty.
    /// </summary>
    public required TypeName ServiceType { get; init; }

    /// <summary>
    /// Indicates if a [Dependency]-attribute is present.
    /// </summary>
    public required bool HasAttribute { get; init; } = false;


    #region Equals

    protected bool Equals(Dependency other) {
        if (Name != other.Name)
            return false;
        if (ServiceName != other.ServiceName)
            return false;
        if (ServiceType != other.ServiceType)
            return false;
        if (HasAttribute != other.HasAttribute)
            return false;

        return true;
    }

    public override int GetHashCode() {
        int hashCode = Name.GetHashCode();
        hashCode = Combine(hashCode, ServiceName.GetHashCode());
        hashCode = Combine(hashCode, ServiceType.GetHashCode());
        hashCode = Combine(hashCode, HasAttribute.GetHashCode());
        return hashCode;


        static int Combine(int h1, int h2) {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }

    #endregion
}
