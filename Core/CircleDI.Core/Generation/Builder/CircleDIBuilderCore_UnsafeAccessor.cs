using CircleDI.Defenitions;
using CircleDI.Extensions;

namespace CircleDI.Generation;

/// <summary>
/// Contains the method to build the UnsafeAccessor methods for setting circular init-only properties..
/// </summary>
public partial struct CircleDIBuilderCore {
    /// <summary>
    /// Generates the UnsafeAccessor methods for setting circular init-only properties.
    /// </summary>
    public void AppendUnsafeAccessorMethods() {
        builder.Append("\n\n\n");
        int startPosition = builder.Length;

        foreach (Service service in serviceProvider.SortedServiceList)
            foreach (PropertyDependency dependency in service.PropertyDependencyList)
                if (dependency.IsCircular && dependency.IsInit)
                    builder.AppendInterpolation($"""
                        {indent}[System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = "set_{dependency.Name}")]
                        {indent}private extern static void Set_{service.Name}_{dependency.Name}(global::{dependency.ImplementationBaseName.AsClosedFullyQualified} instance, global::{dependency.Service!.ServiceType.AsClosedFullyQualified} value);


                        """);

        if (builder.Length == startPosition)
            builder.Length -= 3; // remove initial "\n\n\n"
        else
            builder.Length--; // remove last '\n'
    }
}
