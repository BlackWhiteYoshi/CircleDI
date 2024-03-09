namespace CircleDI;

/// <summary>
/// The type of the ServiceProvider/ScopeProvider.
/// </summary>
public enum ClassStructKeyword : byte {
    /// <summary>
    /// A normal class, the default.
    /// </summary>
    Class,

    /// <summary>
    /// A normal struct, a value type.
    /// </summary>
    Struct,

    /// <summary>
    /// Same as <see cref="RecordClass"/>, but without the "class" in the keyword.
    /// </summary>
    Record,

    /// <summary>
    /// A class with value type behaviour.
    /// </summary>
    RecordClass,

    /// <summary>
    /// A valuetype with integrated Equals() and ToString().
    /// </summary>
    RecordStruct
}

public static class ClassStructKeywordExctension {
    public static string AsString(this ClassStructKeyword value)
        => value switch {
            ClassStructKeyword.Class => "class",
            ClassStructKeyword.Struct => "struct",
            ClassStructKeyword.Record => "record",
            ClassStructKeyword.RecordClass => "record class",
            ClassStructKeyword.RecordStruct => "record struct",
            _ => throw new Exception($"Invalid enum Type {nameof(ClassStructKeyword)}: {value}")
        };

    public static bool IsValueType(this ClassStructKeyword value) => value is ClassStructKeyword.Struct or ClassStructKeyword.RecordStruct;
}
