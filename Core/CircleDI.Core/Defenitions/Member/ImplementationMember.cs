namespace CircleDI.Defenitions;

/// <summary>
/// Represents a member in a ServiceProvider that returns or holds a service-implementation.<br />
/// It is either a field, property or method or it can also be just none (null).
/// </summary>
public readonly record struct ImplementationMember(MemberType Type, string Name, bool IsStatic, bool IsScoped) : IEquatable<ImplementationMember> {
    /// <summary>
    /// The kind of the member (field, property, method, none/null).<br />
    /// If it is <see cref="MemberType.None"/> (null), the other fields have no meaning and should not be used.
    /// </summary>
    public MemberType Type { get; } = Type;

    /// <summary>
    /// The identifier of the member.
    /// </summary>
    public string Name { get; } = Name;

    /// <summary>
    /// Indicates if the member is static
    /// </summary>
    public bool IsStatic { get; } = IsStatic;

    /// <summary>
    /// <para>Location of the implementation.</para>
    /// If false, it is located in the main Provider.<br />
    /// If true, it is located in the scoped Provider.
    /// </summary>
    public bool IsScoped { get; } = IsScoped;
}
