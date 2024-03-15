namespace CircleDI.Defenitions;

/// <summary>
/// Some Flags for creating, processing or consuming the dependency tree.
/// </summary>
[Flags]
public enum DependencyTreeFlags {
    /// <summary>
    /// The initial value, no flags are set.
    /// </summary>
    New = 0x0,

    /// <summary>
    /// This Flag is used for creating the dependency tree. It indicates that a service is visited during the process.
    /// </summary>
    Traversed = 0x1,

    /// <summary>
    /// This Flag is used for consuming the dependency tree. It indicates that a service is visited during the process.
    /// </summary>
    Generated = 0x2
}
