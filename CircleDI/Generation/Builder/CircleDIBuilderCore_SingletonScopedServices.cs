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
                builder.AppendIndent(indent)
                    .AppendInterpolation($"public {refOrEmpty}global::")
                    .AppendClosedFullyQualified(service.ServiceType)
                    .Append(' ')
                    .AppendServiceGetter(service);

                if (service.Implementation.Type == MemberType.Field) {
                    builder.AppendInterpolation($" => {refOrEmpty}");
                    if (service.Lifetime == ServiceLifetime.Scoped && !service.Implementation.IsScoped && !service.Implementation.IsStatic)
                        builder.Append('_')
                            .AppendFirstLower(serviceProvider.Identifier.Name)
                            .Append('.');
                    builder.AppendImplementationName(service)
                        .Append(";\n");
                }
                else if (service.CreationTimeTransitive == CreationTiming.Constructor) {
                    builder.AppendInterpolation($" => {refOrEmpty}_")
                        .AppendFirstLower(service.Name)
                        .Append(";\n");

                    builder.AppendIndent(indent)
                        .AppendInterpolation($"private {readonlyStr}global::")
                        .AppendClosedFullyQualified(service.ImplementationType)
                        .Append(" _")
                        .AppendFirstLower(service.Name)
                        .Append(";\n");
                }
                else {
                    builder.Append(" {\n");
                    indent.IncreaseLevel(); // 2

                    if (service.GetAccessor == GetAccess.Property) {
                        builder.AppendIndent(indent)
                            .Append("get {\n");
                        indent.IncreaseLevel(); // 3
                    }

                    AppendLazyService(service);

                    builder.AppendIndent(indent)
                        .AppendInterpolation($"return {refOrEmpty}(global::")
                        .AppendClosedFullyQualified(service.ServiceType)
                        .Append(")_")
                        .AppendFirstLower(service.Name)
                        .Append(";\n");

                    if (service.GetAccessor == GetAccess.Property) {
                        indent.DecreaseLevel(); // 2
                        builder.AppendIndent(indent)
                            .Append("}\n");
                    }

                    indent.DecreaseLevel(); // 1
                    builder.AppendIndent(indent)
                        .Append("}\n");

                    builder.AppendIndent(indent)
                        .Append("private global::")
                        .AppendClosedFullyQualified(service.ImplementationType)
                        .Append("? _")
                        .AppendFirstLower(service.Name)
                        .Append(";\n");
                }

                builder.Append('\n');
            }

            builder.Append('\n');
        }
    }
}
