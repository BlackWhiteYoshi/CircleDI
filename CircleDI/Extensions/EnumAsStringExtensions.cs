using CircleDI.Defenitions;
using Microsoft.CodeAnalysis;

namespace CircleDI.Extensions;

/// <summary>
/// Extension methods for mapping enum types to string.
/// </summary>
public static class EnumAsStringExtension {
    /// <summary>
    /// Maps <see cref="ServiceLifetime"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string AsString(this ServiceLifetime value)
       => value switch {
           ServiceLifetime.Singleton => nameof(ServiceLifetime.Singleton),
           ServiceLifetime.Scoped => nameof(ServiceLifetime.Scoped),
           ServiceLifetime.Transient or ServiceLifetime.TransientSingleton or ServiceLifetime.TransientScoped => nameof(ServiceLifetime.Transient),
           ServiceLifetime.Delegate => nameof(ServiceLifetime.Delegate),
           _ => ((int)value).ToString()
       };

    /// <summary>
    /// Maps <see cref="TypeKeyword"/> to <see cref="string"/>.
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    /// <exception cref="Exception"></exception>
    public static string AsString(this TypeKeyword value)
        => value switch {
            TypeKeyword.Class => "class",
            TypeKeyword.Struct => "struct",
            TypeKeyword.Record => "record",
            TypeKeyword.RecordClass => "record class",
            TypeKeyword.RecordStruct => "record struct",
            TypeKeyword.Interface => "interface",
            _ => throw new Exception($"Invalid enum Type '{nameof(TypeKeyword)}': {value}")
        };

    /// <summary>
    /// Maps <see cref="RefKind"/> to <see cref="string"/> (with trailing space).
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string AsString(this RefKind value)
        => value switch {
            RefKind.None => string.Empty,
            RefKind.Ref or RefKind.RefReadOnlyParameter => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            RefKind.RefReadOnly or _ => throw new Exception($"Invalid enum Type '{nameof(RefKind)}': {value}")
        };

    /// <summary>
    /// Maps <see cref="Accessibility"/> to <see cref="string"/> (with trailing space).
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static string AsString(this Accessibility value)
        => value switch {
            Accessibility.Public => "public ",
            Accessibility.Private => "private ",
            Accessibility.Internal => "internal ",
            Accessibility.Protected => "protected ",
            Accessibility.ProtectedAndInternal => "private protected ",
            Accessibility.ProtectedOrInternal => "protected internal ",
            Accessibility.NotApplicable => "",
            _ => throw new Exception($"Invalid enum Type '{nameof(Accessibility)}': {value}")
        };
}
