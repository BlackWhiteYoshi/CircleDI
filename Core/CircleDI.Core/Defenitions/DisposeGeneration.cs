namespace CircleDI.Defenitions;

/// <summary>
/// <para>Configuration for generating the Dispose methods:</para>
/// <para>
/// public void Dispose();<br />
/// public ValueTask DisposeAsync();
/// </para>
/// <para>It can be toggled that both are generated, only one of them or the generation is skipped entirely.</para>
/// </summary>
[Flags]
public enum DisposeGeneration {
    /// <summary>
    /// The generation of both dispose methods will be skipped.
    /// </summary>
    NoDisposing = 0b00,

    /// <summary>
    /// <para>The sync version of Dispose will be generated:</para>
    /// <para>public void Dispose();</para>
    /// </summary>
    Dispose = 0b01,

    /// <summary>
    /// <para>The async version DisposeAsync will be generated:</para>
    /// <para>public ValueTask DisposeAsync();</para>
    /// </summary>
    DisposeAsync = 0b10,

    /// <summary>
    /// <para>Both versions Dispose and DisposeAsync will be generated.</para>
    /// <para>
    /// public void Dispose();<br />
    /// public ValueTask DisposeAsync();
    /// </para>
    /// </summary>
    GenerateBoth = 0b11
}
