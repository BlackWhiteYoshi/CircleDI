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
                builder.AppendIndent(indent);
                builder.Append("public ");
                builder.Append(refOrEmpty);
                builder.Append("global::");
                builder.AppendClosedFullyQualified(service.ServiceType);
                builder.Append(' ');
                builder.AppendServiceGetter(service);

                if (service.Implementation.Type == MemberType.Field) {
                    builder.Append(" => ");
                    builder.Append(refOrEmpty);
                    if (service.Lifetime == ServiceLifetime.Scoped && !service.Implementation.IsScoped && !service.Implementation.IsStatic) {
                        builder.Append('_');
                        builder.AppendFirstLower(serviceProvider.Identifier.Name);
                        builder.Append('.');
                    }
                    builder.AppendImplementationName(service);
                    builder.Append(";\n");
                }
                else if (service.CreationTimeTransitive == CreationTiming.Constructor) {
                    builder.Append(" => ");
                    builder.Append(refOrEmpty);
                    builder.Append('_');
                    builder.AppendFirstLower(service.Name);
                    builder.Append(";\n");

                    builder.AppendIndent(indent);
                    builder.Append("private ");
                    builder.Append(readonlyStr);
                    builder.Append("global::");
                    builder.AppendClosedFullyQualified(service.ImplementationType);
                    builder.Append(" _");
                    builder.AppendFirstLower(service.Name);
                    builder.Append(";\n");
                }
                else {
                    builder.Append(" {\n");
                    indent.IncreaseLevel(); // 2

                    if (service.GetAccessor == GetAccess.Property) {
                        builder.AppendIndent(indent);
                        builder.Append("get {\n");
                        indent.IncreaseLevel(); // 3
                    }

                    AppendLazyService(service);

                    builder.AppendIndent(indent);
                    builder.Append("return ");
                    builder.Append(refOrEmpty);
                    builder.Append("(global::");
                    builder.AppendClosedFullyQualified(service.ServiceType);
                    builder.Append(")_");
                    builder.AppendFirstLower(service.Name);
                    builder.Append(";\n");

                    if (service.GetAccessor == GetAccess.Property) {
                        indent.DecreaseLevel(); // 2
                        builder.AppendIndent(indent);
                        builder.Append("}\n");
                    }

                    indent.DecreaseLevel(); // 1
                    builder.AppendIndent(indent);
                    builder.Append("}\n");

                    builder.AppendIndent(indent);
                    builder.Append("private ");
                    builder.Append("global::");
                    builder.AppendClosedFullyQualified(service.ImplementationType);
                    builder.Append("? _");
                    builder.AppendFirstLower(service.Name);
                    builder.Append(";\n");
                }

                builder.Append('\n');
            }

            builder.Append('\n');
        }
    }
}
