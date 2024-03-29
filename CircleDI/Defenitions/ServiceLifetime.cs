﻿namespace CircleDI.Defenitions;

/// <summary>
/// Lifetime type of the service:<br />
/// Singleton, Scoped, Transient or TransientSingleton, TransientScoped or Delegate.
/// </summary>
[Flags]
public enum ServiceLifetime {
    /// <summary>
    /// Service living in ServiceProvider and is getting constructed once.
    /// </summary>
    Singleton = 0b001,

    /// <summary>
    /// Service living in ScopedProvider and is getting constructed once.
    /// </summary>
    Scoped = 0b010,

    /// <summary>
    /// Service available in ServiceProvider and ScopedProvider and is getting constructed every time requested.
    /// </summary>
    Transient = 0b100,

    /// <summary>
    /// Service available in Serviceprovider only and is getting constructed every time requested.<br />
    /// The only service that has this lifetime is 'CreateScope()'.
    /// </summary>
    TransientSingleton = 0b101,

    /// <summary>
    /// Service available in Scopedprovider only (because has scoped dependency) and is getting constructed every time requested.
    /// </summary>
    TransientScoped = 0b110,

    /// <summary>
    /// Service without lifetime, methods are constant executable data and therefore created at compile time.
    /// </summary>
    Delegate = 0b000
}
