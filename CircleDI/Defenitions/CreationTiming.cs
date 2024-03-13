namespace CircleDI;

/// <summary>
/// <para>
/// Configuration when the instantiation of the service happens.<br />
/// At ServiceProvider instantiation (inside the constructor) or lazy (first time used).
/// </para>
/// </summary>
public enum CreationTiming {
    /// <summary>
    /// The instantiation of the service happens inside the constructor of the ServiceProvider.
    /// </summary>
    Constructor,

    /// <summary>
    /// The instantiation of the service happens the first time it is requested.
    /// </summary>
    Lazy
}
