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
            builder.AppendIndent(indent)
                .Append("public global::")
                .AppendClosedFullyQualified(service.ServiceType)
                .Append(' ');

            if (service.GetAccessor == GetAccess.Property) {
                builder.AppendInterpolation($"{service.Name} {{\n");
                indent.IncreaseLevel(); // 2
                builder.AppendIndent(indent)
                    .Append("get {\n");
            }
            else
                builder.AppendInterpolation($"Get{service.Name}() {{\n");
            indent.IncreaseLevel(); // 2 or 3

            int transientNumber = AppendTransientService(service);

            builder.AppendIndent(indent)
                .Append("return ")
                .AppendFirstLower(service.Name);
            if (transientNumber > 0)
                builder.AppendInterpolation($"_{transientNumber}");
            builder.Append(";\n");


            indent.DecreaseLevel(); // 1 or 2
            builder.AppendIndent(indent)
                .Append("}\n");

            if (service.GetAccessor == GetAccess.Property) {
                indent.DecreaseLevel(); // 1
                builder.AppendIndent(indent)
                    .Append("}\n");
            }

            builder.Append('\n');
        }

        if (builder.Length > currentPosition)
            builder.Append('\n');
    }
}
