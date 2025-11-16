namespace CircleDI.Defenitions;

/// <summary>
/// Option for handling the instantiation of the module.
/// </summary>
public enum ImportMode {
    /// <summary>
    /// Chooses <see cref="Static"/> when type is interface, <see cref="Service"/> if constructed with parameterless constructor, <see cref="Parameter"/> otherwise.
    /// </summary>
    Auto,

    /// <summary>
    /// No instantiation needed, all members are static.
    /// </summary>
    Static,

    /// <summary>
    /// The module is registered as service.
    /// </summary>
    Service,

    /// <summary>
    /// An instance of the module is given as parameter.
    /// </summary>
    Parameter
}
