namespace CircleDI.MinimalAPI.Defenitions;

/// <summary>
/// <para>Configuration for generating getter methods for the default services from the built-in service provider.</para>
/// <para>
/// The built-in service provider has some services registered only in specific environments.<br />
/// It can be configured to have only services that are available in all environments, all services for a specific environment or disable generating any default services.
/// </para>
/// </summary>
[Flags]
public enum BlazorServiceGeneration {
    /// <summary>
    /// No default services will be generated.
    /// </summary>
    None = 0b000,

    /// <summary>
    /// All Blazor Webassembly default services will be generated.
    /// </summary>
    Webassembly = 0b001,

    /// <summary>
    /// All Blazor Server-side default services will be generated.
    /// </summary>
    Server = 0b010,

    /// <summary>
    /// Blazor Hybrid has the least default services and all these services also works in server and webassembly environment.
    /// </summary>
    Hybrid = 0b100,

    /// <summary>
    /// <para>All default services that are available in server and webassembly environment.</para>
    /// </summary>
    ServerAndWebassembly = Server | Webassembly
}
