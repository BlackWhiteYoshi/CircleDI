namespace CircleDI.Defenitions;

/// <summary>
/// <para>Some Flags for creating and consuming the dependency tree.</para>
/// <para>The flag for creating the dependency tree is explicit, consuming happens multiple times and thesevalues are calculated dynamically, see <see cref="Generation.ServiceProvider.DependencyTreeFlag"/>.</para>
/// </summary>
[Flags]
public enum DependencyTreeFlags : long {
    /// <summary>
    /// The initial value, no flags are set.
    /// </summary>
    New = 0x0,

    /// <summary>
    /// This Flag is used for creating the dependency tree. It indicates that a service is visited during the process.
    /// </summary>
    Traversed = 0x1
}
