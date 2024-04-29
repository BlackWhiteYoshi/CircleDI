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
                    builder.AppendIndent(indent);
                    builder.Append("[System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"set_");
                    builder.Append(dependency.Name);
                    builder.Append("\")]\n");

                    builder.AppendIndent(indent);
                    builder.Append("private extern static void Set_");
                    builder.Append(service.Name);
                    builder.Append('_');
                    builder.Append(dependency.Name);
                    builder.Append("(global::");
                    builder.AppendClosedFullyQualified(dependency.ImplementationBaseName);
                    builder.Append(" instance, global::");
                    builder.AppendClosedFullyQualified(dependency.Service!.ServiceType);
                    builder.Append(" value);\n\n");
                }

        if (builder.Length == currentPosition)
            builder.Length -= 3;
        else
            builder.Length--;
    }
}
