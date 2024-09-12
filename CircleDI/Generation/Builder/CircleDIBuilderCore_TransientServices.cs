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

            if (service.GetAccessor == GetAccess.Property) {
                builder.AppendInterpolation($"{indent}public global::{service.ServiceType.AsClosedFullyQualified()} {service.Name} {{\n");
                indent.IncreaseLevel(); // 2

                builder.AppendInterpolation($"{indent}get {{\n");
                indent.IncreaseLevel(); // 3
            }
            else {
                builder.AppendInterpolation($"{indent}public global::{service.ServiceType.AsClosedFullyQualified()} Get{service.Name}() {{\n");
                indent.IncreaseLevel(); // 2
            }

            int transientNumber = AppendTransientService(service);
            if (transientNumber > 0)
                builder.AppendInterpolation($"{indent}return {service.Name.AsFirstLower()}_{transientNumber};\n");
            else
                builder.AppendInterpolation($"{indent}return {service.Name.AsFirstLower()};\n");

            if (service.GetAccessor == GetAccess.Property) {
                indent.DecreaseLevel(); //  2
                builder.AppendInterpolation($"{indent}}}\n");

                indent.DecreaseLevel(); // 1
                builder.AppendInterpolation($"{indent}}}\n\n");
            }
            else {
                indent.DecreaseLevel(); // 1
                builder.AppendInterpolation($"{indent}}}\n\n");
            }
        }

        if (builder.Length > currentPosition)
            builder.Append('\n');
    }
}
