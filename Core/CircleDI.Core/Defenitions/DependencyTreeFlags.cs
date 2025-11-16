namespace CircleDI.Defenitions;

/// <summary>
/// <para>Some Flags for creating and consuming the dependency tree.</para>
/// <para>They indicate if a node is visited or initialized during tree traversal.</para>
/// <para>The flag for creating the dependency tree is explicit, consuming happens multiple times and thesevalues are calculated dynamically, see <see cref="Generation.ServiceProvider.DependencyTreeFlag"/>.</para>
/// </summary>
[Flags]
public enum DependencyTreeFlags {
    /// <summary>
    /// The first Flag/Bit.
    /// </summary>
    New = 0x1
}
