namespace CircleDI;

/// <summary>
/// The type of the ServiceProvider/ScopeProvider.
/// </summary>
[Flags]
public enum ClassStructKeyword : byte {
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

public static class ClassStructKeywordExctension {
    /// <summary>
    /// <para>Maps <see cref="ClassStructKeyword"/> to <see cref="string"/>.</para>
    /// <para>Actually <see cref="ClassStructKeyword"/> should be a enum of string in the first place, but C# does not support that.</para>
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static string AsString(this ClassStructKeyword value)
        => value switch {
            ClassStructKeyword.Class => "class",
            ClassStructKeyword.Struct => "struct",
            ClassStructKeyword.Record => "record",
            ClassStructKeyword.RecordClass => "record class",
            ClassStructKeyword.RecordStruct => "record struct",
            _ => throw new Exception($"Invalid enum Type '{nameof(ClassStructKeyword)}': {value}")
        };
}
