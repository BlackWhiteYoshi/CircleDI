using CircleDI.Defenitions;
using CircleDI.Extensions;

namespace CircleDI.Generation;

/// <summary>
/// Contains the method to build the Singleton/Scoped Fields and Getter.
/// </summary>
public partial struct CircleDIBuilderCore {
    /// <summary>
    /// Singletons/Scoped service Getter
    /// </summary>
    public void AppendSingletonScopedServices() {
        if (serviceList.Count > 0) {
            foreach (Service service in serviceList) {
                string refOrEmpty = (service.IsRefable && !keyword.HasFlag(TypeKeyword.Struct)) switch {
                    true => "ref ",
                    false => string.Empty
                };

                AppendServiceSummary(service);

                if (service.Implementation.Type == MemberType.Field)
                    if (service.Lifetime == ServiceLifetime.Scoped && !service.Implementation.IsScoped && !service.Implementation.IsStatic)
                        builder.AppendInterpolation($"{indent}public {refOrEmpty}global::{service.ServiceType.AsClosedFullyQualified} {service.AsServiceGetter} => {refOrEmpty}_{serviceProvider.Identifier.Name.AsFirstLower}.{service.AsImplementationName};\n\n");
                    else
                        builder.AppendInterpolation($"{indent}public {refOrEmpty}global::{service.ServiceType.AsClosedFullyQualified} {service.AsServiceGetter} => {refOrEmpty}{service.AsImplementationName};\n\n");

                else if (service.CreationTimeTransitive == CreationTiming.Constructor)
                    builder.AppendInterpolation($"""
                        {indent}public {refOrEmpty}global::{service.ServiceType.AsClosedFullyQualified} {service.AsServiceGetter} => {refOrEmpty}_{service.Name.AsFirstLower};
                        {indent}private {(!service.IsRefable ? readonlyStr : "")}global::{service.ImplementationType.AsClosedFullyQualified} _{service.Name.AsFirstLower};


                        """);

                // CreationTiming.Lazy
                else {
                    builder.AppendInterpolation($"{indent}public {refOrEmpty}global::{service.ServiceType.AsClosedFullyQualified} {service.AsServiceGetter} {{\n");
                    indent.IncreaseLevel(); // 2

                    if (service.GetAccessor == GetAccess.Property) {
                        builder.AppendInterpolation($"{indent}get {{\n");
                        indent.IncreaseLevel(); // 3
                    }

                    AppendLazyService(service);
                    builder.AppendInterpolation($"{indent}return {refOrEmpty}_{service.Name.AsFirstLower};\n");

                    if (service.GetAccessor == GetAccess.Property) {
                        indent.DecreaseLevel(); // 2
                        builder.AppendInterpolation($"{indent}}}\n");
                    }

                    indent.DecreaseLevel(); // 1
                    builder.AppendInterpolation($"{indent}}}\n");

                    if (!service.IsValueType)
                        builder.AppendInterpolation($"{indent}private global::{service.ImplementationType.AsClosedFullyQualified}? _{service.Name.AsFirstLower};\n\n");
                    else {
                        builder.AppendInterpolation($"{indent}private global::{service.ImplementationType.AsClosedFullyQualified} _{service.Name.AsFirstLower};\n");
                        builder.AppendInterpolation($"{indent}private global::System.Boolean _{service.Name.AsFirstLower}_hasValue = false;\n\n");
                    }
                }
            }

            builder.Append('\n');
        }
    }
}
