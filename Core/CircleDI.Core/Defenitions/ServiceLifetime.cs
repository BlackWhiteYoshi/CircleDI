namespace CircleDI.Defenitions;

/// <summary>
/// Lifetime type of the service:<br />
/// Singleton, Scoped, Transient or TransientSingleton, TransientScoped or Delegate.
/// </summary>
[Flags]
public enum ServiceLifetime {
    /// <summary>
    /// Service living in ServiceProvider and is getting constructed once.
    /// </summary>
    Singleton = 0b0001,

    /// <summary>
    /// Service living in ScopedProvider and is getting constructed once.
    /// </summary>
    Scoped = 0b0010,

    /// <summary>
    /// Service available in ServiceProvider and ScopedProvider and is getting constructed every time requested.
    /// </summary>
    Transient = 0b0100,

    /// <summary>
    /// Service available in Serviceprovider only and is getting constructed every time requested.<br />
    /// The only service that has this lifetime is 'CreateScope()'.
    /// </summary>
    TransientSingleton = 0b0101,

    /// <summary>
    /// Service available in Scopedprovider only (because has scoped dependency) and is getting constructed every time requested.
    /// </summary>
    TransientScoped = 0b0110,

    /// <summary>
    /// Service without lifetime, methods are constant executable data and therefore created at compile time.
    /// </summary>
    Delegate = 0b1000,

    /// <summary>
    /// Service without lifetime, method declared inside Scope and therefore only available for scoped services.
    /// </summary>
    DelegateScoped = 0b1010
}
