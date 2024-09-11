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
        int currentPosition = builder.Length;

        foreach (Service service in serviceProvider.SortedServiceList)
            foreach (PropertyDependency dependency in service.PropertyDependencyList)
                if (dependency.IsCircular && dependency.IsInit) {
                    builder.AppendIndent(indent)
                        .AppendInterpolation($"[System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"set_{dependency.Name}\")]\n");

                    builder.AppendIndent(indent)
                        .AppendInterpolation($"private extern static void Set_{service.Name}_{dependency.Name}(global::")
                        .AppendClosedFullyQualified(dependency.ImplementationBaseName)
                        .Append(" instance, global::")
                        .AppendClosedFullyQualified(dependency.Service!.ServiceType)
                        .Append(" value);\n\n");
                }

        if (builder.Length == currentPosition)
            builder.Length -= 3;
        else
            builder.Length--;
    }
}
