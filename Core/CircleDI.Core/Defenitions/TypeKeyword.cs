namespace CircleDI.Defenitions;

/// <summary>
/// The type of the ServiceProvider/ScopeProvider.
/// </summary>
[Flags]
public enum TypeKeyword {
    /// <summary>
    /// A normal class, the default.
    /// </summary>
    Class = 0b0010,

    /// <summary>
    /// A normal struct, a value type.
    /// </summary>
    Struct = 0b0100,

    /// <summary>
    /// The interface type
    /// </summary>
    Interface = 0b1000,

    /// <summary>
    /// Same as <see cref="RecordClass"/>, but without the "class" in the keyword.
    /// </summary>
    Record = 0b0001,

    /// <summary>
    /// A class with value type behaviour.
    /// </summary>
    RecordClass = 0b0011,

    /// <summary>
    /// A valuetype with integrated Equals() and ToString().
    /// </summary>
    RecordStruct = 0b0101,

    /// <summary>
    /// Not a class, struct or interface
    /// </summary>
    Unsupported = 0
}
