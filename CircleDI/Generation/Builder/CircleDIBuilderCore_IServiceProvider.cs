using CircleDI.Defenitions;
using CircleDI.Extensions;

namespace CircleDI.Generation;

/// <summary>
/// Contains 2 methods to build the <see cref="IServiceProvider.GetService(Type)">IServiceProvider.GetService()</see> method, inclusive summary.<br />
/// <see cref="AppendIServiceProviderNotScoped"/> is for the ServiceProvider, <see cref="AppendIServiceProviderAllServices"/> is for the ScopedProvider.
/// </summary>
public partial struct CircleDIBuilderCore {
    /// <summary>
    /// <see cref="IServiceProvider.GetService(Type)">GetService</see> method inclusive summary filtered to Singleton and Transient services.
    /// </summary>
    public void AppendIServiceProviderNotScoped()
        => AppendIServiceProvider(serviceProvider.SortedServiceList
            .Where((Service service) => !service.Lifetime.HasFlag(ServiceLifetime.Scoped) && !(service.Implementation.Type != MemberType.None && service.Implementation.IsScoped))
            .GetEnumerator());

    /// <summary>
    /// <see cref="IServiceProvider.GetService(Type)">GetService</see> method inclusive summary with all services.
    /// </summary>
    public void AppendIServiceProviderAllServices()
        => AppendIServiceProvider(serviceProvider.SortedServiceList.GetEnumerator());

    private void AppendIServiceProvider(IEnumerator<Service> serviceEnumerator) {
        builder.AppendIndent(indent)
            .Append("/// <summary>\n");
        builder.AppendIndent(indent)
            .Append("/// <para>Finds all registered services of the given type.</para>\n");
        builder.AppendIndent(indent)
            .Append("/// <para>\n");
        builder.AppendIndent(indent)
            .Append("/// The method returns<br />\n");
        builder.AppendIndent(indent)
            .Append("/// - null (when registered zero times)<br />\n");
        builder.AppendIndent(indent)
            .Append("/// - given type (when registered ones)<br />\n");
        builder.AppendIndent(indent)
            .Append("/// - Array of given type (when registered many times)\n");
        builder.AppendIndent(indent)
            .Append("/// </para>\n");
        builder.AppendIndent(indent)
            .Append("/// </summary>\n");

        builder.AppendIndent(indent)
            .Append("object? IServiceProvider.GetService(Type serviceType) {\n");
        indent.IncreaseLevel(); // 2

        builder.AppendIndent(indent)
            .Append("switch (serviceType.Name) {\n");
        indent.IncreaseLevel(); // 3

        if (serviceEnumerator.MoveNext()) {
            Service? service = serviceEnumerator.Current;
            string currentserviceName = service.ServiceType.Name;
            int currentTypeParameterCount = service.ServiceType.TypeArgumentList.Count;
            builder.AppendIndent(indent)
                .AppendInterpolation($"case \"{currentserviceName}");
            if (currentTypeParameterCount > 0)
                builder.AppendInterpolation($"`{currentTypeParameterCount}");
            builder.Append("\":\n");
            indent.IncreaseLevel(); // 4

            do {
                if (service.ServiceType.Name != currentserviceName || service.ServiceType.TypeArgumentList.Count != currentTypeParameterCount) {
                    currentserviceName = service.ServiceType.Name;
                    currentTypeParameterCount = service.ServiceType.TypeArgumentList.Count;

                    builder.AppendIndent(indent)
                        .Append("return null;\n");
                    indent.DecreaseLevel(); // 3

                    builder.AppendIndent(indent)
                        .AppendInterpolation($"case \"{currentserviceName}");
                    if (currentTypeParameterCount > 0)
                        builder.AppendInterpolation($"`{currentTypeParameterCount}");
                    builder.Append("\":\n");
                    indent.IncreaseLevel(); // 4
                }

                builder.AppendIndent(indent)
                    .Append("if (serviceType == typeof(global::")
                    .AppendClosedFullyQualified(service.ServiceType)
                    .Append("))\n");
                indent.IncreaseLevel(); // 5

                builder.AppendIndent(indent)
                    .Append("return ");

                Service? nextService = serviceEnumerator.MoveNext() ? serviceEnumerator.Current : null;
                if (service.ServiceType != nextService?.ServiceType)
                    builder.AppendServiceGetter(service)
                        .Append(";\n");
                else {
                    builder.Append("(global::")
                        .AppendClosedFullyQualified(service.ServiceType)
                        .Append("[])[");
                    builder.AppendServiceGetter(service);
                    do {
                        builder.Append(", ")
                            .AppendServiceGetter(nextService!);
                        nextService = serviceEnumerator.MoveNext() ? serviceEnumerator.Current : null;
                    }
                    while (service.ServiceType == nextService?.ServiceType);
                    builder.Append("];\n");
                }
                indent.DecreaseLevel(); // 4

                service = nextService;
            } while (service is not null);

            builder.AppendIndent(indent)
                .Append("return null;\n");
            indent.DecreaseLevel(); // 3
        }

        builder.AppendIndent(indent)
            .Append("default:\n");
        indent.IncreaseLevel(); // 4

        builder.AppendIndent(indent)
            .Append("return null;\n");
        indent.DecreaseLevel(); // 3

        indent.DecreaseLevel(); // 2
        builder.AppendIndent(indent)
            .Append("}\n");

        indent.DecreaseLevel(); // 1
        builder.AppendIndent(indent)
            .Append("}\n\n\n");
    }
}
