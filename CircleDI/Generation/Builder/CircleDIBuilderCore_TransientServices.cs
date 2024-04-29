using CircleDI.Defenitions;
using CircleDI.Extensions;

namespace CircleDI.Generation;

/// <summary>
/// Contains the method to build the Transient Getter.
/// </summary>
public partial struct CircleDIBuilderCore {
    /// <summary>
    /// Transients service Getter
    /// </summary>
    public void AppendTransientServices() {
        int currentPosition = builder.Length;

        foreach (Service service in serviceProvider.TransientList) {
            if (!isScopeProvider && service.Lifetime is ServiceLifetime.TransientScoped)
                continue;

            AppendServiceSummary(service);
            builder.AppendIndent(indent);
            builder.Append("public global::");
            builder.AppendClosedFullyQualified(service.ServiceType);
            builder.Append(' ');

            if (service.GetAccessor == GetAccess.Property) {
                builder.Append(service.Name);
                builder.Append(" {\n");
                indent.IncreaseLevel(); // 2

                builder.AppendIndent(indent);
                builder.Append("get {\n");
                indent.IncreaseLevel(); // 3
            }
            else {
                builder.Append("Get");
                builder.Append(service.Name);
                builder.Append("() {\n");
                indent.IncreaseLevel(); // 2
            }

            int transientNumber = AppendTransientService(service);

            builder.AppendIndent(indent);
            builder.Append("return ");
            builder.AppendFirstLower(service.Name);
            if (transientNumber > 0) {
                builder.Append('_');
                builder.Append(transientNumber);
            }
            builder.Append(";\n");


            indent.DecreaseLevel(); // 1 or 2
            builder.AppendIndent(indent);
            builder.Append("}\n");

            if (service.GetAccessor == GetAccess.Property) {
                indent.DecreaseLevel(); // 1
                builder.AppendIndent(indent);
                builder.Append("}\n");
            }

            builder.Append('\n');
        }

        if (builder.Length > currentPosition)
            builder.Append('\n');
    }
}
