namespace CircleDI.Defenitions;

/// <summary>
/// The type of the ServiceProvider/ScopeProvider.
/// </summary>
[Flags]
public enum ClassStructKeyword {
    /// <summary>
    /// A normal class, the default.
    /// </summary>
    Class = 0b001,

    /// <summary>
    /// A normal struct, a value type.
    /// </summary>
    Struct = 0b010,

    /// <summary>
    /// Same as <see cref="RecordClass"/>, but without the "class" in the keyword.
    /// </summary>
    Record = 0b100,

    /// <summary>
    /// A class with value type behaviour.
    /// </summary>
    RecordClass = 0b101,

    /// <summary>
    /// A valuetype with integrated Equals() and ToString().
    /// </summary>
    RecordStruct = 0b110
}
