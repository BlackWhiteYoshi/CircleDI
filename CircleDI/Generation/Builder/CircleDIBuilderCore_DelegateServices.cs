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
            builder.AppendIndent(indent);
            builder.Append("public global::");
            builder.AppendClosedFullyQualified(service.ServiceType);
            builder.Append(' ');
            builder.AppendServiceGetter(service);
            builder.Append(" => ");
            if (isScopeProvider && !service.Implementation.IsScoped && !service.Implementation.IsStatic) {
                builder.Append('_');
                builder.AppendFirstLower(serviceProvider.Identifier.Name);
                builder.Append('.');
            }
            builder.AppendImplementationName(service);
            builder.Append(";\n\n");
        }

        if (builder.Length > currentPosition)
            builder.Append('\n');
    }
}
