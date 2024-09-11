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
            builder.AppendIndent(indent)
                .Append("public global::")
                .AppendClosedFullyQualified(service.ServiceType)
                .Append(' ')
                .AppendServiceGetter(service)
                .Append(" => ");
            if (isScopeProvider && !service.Implementation.IsScoped && !service.Implementation.IsStatic)
                builder.Append('_')
                    .AppendFirstLower(serviceProvider.Identifier.Name)
                    .Append('.');
            builder.AppendImplementationName(service);
            if (service.IsGeneric)
                builder.AppendClosedGenerics(service.ImplementationType);
            builder.Append(";\n\n");
        }

        if (builder.Length > currentPosition)
            builder.Append('\n');
    }
}
