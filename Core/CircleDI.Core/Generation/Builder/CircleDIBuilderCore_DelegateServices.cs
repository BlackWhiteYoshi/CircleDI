using CircleDI.Extensions;

namespace CircleDI.Generation;

/// <summary>
/// Contains the method to build the Delegate Getter.
/// </summary>
public partial struct CircleDIBuilderCore {
    /// <summary>
    /// Delegates service Getter
    /// </summary>
    public void AppendDelegateServices() {
        int currentPosition = builder.Length;

        foreach (Service service in serviceProvider.DelegateList) {
            if (!isScopeProvider && service.Implementation.IsScoped)
                continue;

            AppendServiceSummary(service);

            if (isScopeProvider && !service.Implementation.IsScoped && !service.Implementation.IsStatic)
                if (service.IsGeneric)
                    builder.AppendInterpolation($"{indent}public global::{service.ServiceType.AsClosedFullyQualified()} {service.AsServiceGetter()} => _{serviceProvider.Identifier.Name.AsFirstLower()}.{service.AsImplementationName()}{service.ImplementationType.AsClosedGenerics()};\n\n");
                else
                    builder.AppendInterpolation($"{indent}public global::{service.ServiceType.AsClosedFullyQualified()} {service.AsServiceGetter()} => _{serviceProvider.Identifier.Name.AsFirstLower()}.{service.AsImplementationName()};\n\n");
            else
                if (service.IsGeneric)
                    builder.AppendInterpolation($"{indent}public global::{service.ServiceType.AsClosedFullyQualified()} {service.AsServiceGetter()} => {service.AsImplementationName()}{service.ImplementationType.AsClosedGenerics()};\n\n");
                else
                    builder.AppendInterpolation($"{indent}public global::{service.ServiceType.AsClosedFullyQualified()} {service.AsServiceGetter()} => {service.AsImplementationName()};\n\n");
        }

        if (builder.Length > currentPosition)
            builder.Append('\n');
    }
}
