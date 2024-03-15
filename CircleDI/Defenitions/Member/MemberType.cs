namespace CircleDI.Defenitions;

/// <summary>
/// The type of the implementation, it can be a field, property, method or none (null).
/// </summary>
public enum MemberType {
    /// <summary>
    /// Indicates that no implementation exists.<br />
    /// It means the same as 'null'.
    /// </summary>
    /// <remarks>It is important that this value is 0, so 'default' will be 'None'</remarks>
    None = 0,

    /// <summary>
    /// Member is a field.
    /// </summary>
    Field,

    /// <summary>
    /// Member is a property.
    /// </summary>
    Property,

    /// <summary>
    /// Member is a method.
    /// </summary>
    Method
}
