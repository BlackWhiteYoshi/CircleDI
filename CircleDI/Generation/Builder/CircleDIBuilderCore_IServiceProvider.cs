﻿using CircleDI.Defenitions;
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
        => AppendIServiceProvider(new ServiceListIterator(serviceProvider).GetNextNotScoped);

    /// <summary>
    /// <see cref="IServiceProvider.GetService(Type)">GetService</see> method inclusive summary with all services.
    /// </summary>
    public void AppendIServiceProviderAllServices()
        => AppendIServiceProvider(new ServiceListIterator(serviceProvider).GetNextService);

    private void AppendIServiceProvider(Func<Service?> GetNextService) {
        builder.AppendIndent(indent);
        builder.Append("/// <summary>\n");
        builder.AppendIndent(indent);
        builder.Append("/// <para>Finds all registered services of the given type.</para>\n");
        builder.AppendIndent(indent);
        builder.Append("/// <para>\n");
        builder.AppendIndent(indent);
        builder.Append("/// The method returns<br />\n");
        builder.AppendIndent(indent);
        builder.Append("/// - null (when registered zero times)<br />\n");
        builder.AppendIndent(indent);
        builder.Append("/// - given type (when registered ones)<br />\n");
        builder.AppendIndent(indent);
        builder.Append("/// - Array of given type (when registered many times)\n");
        builder.AppendIndent(indent);
        builder.Append("/// </para>\n");
        builder.AppendIndent(indent);
        builder.Append("/// </summary>\n");

        builder.AppendIndent(indent);
        builder.Append("object? IServiceProvider.GetService(Type serviceType) {\n");
        indent.IncreaseLevel(); // 2

        builder.AppendIndent(indent);
        builder.Append("switch (serviceType.Name) {\n");
        indent.IncreaseLevel(); // 3

        Service? service = GetNextService();
        if (service is not null) {
            string currentserviceName = service.ServiceType.Name;
            int currentTypeParameterCount = service.ServiceType.TypeArgumentList.Count;
            builder.AppendIndent(indent);
            builder.Append("case \"");
            builder.Append(currentserviceName);
            if (currentTypeParameterCount > 0) {
                builder.Append('`');
                builder.Append(currentTypeParameterCount);
            }
            builder.Append("\":\n");
            indent.IncreaseLevel(); // 4

            do {
                if (service.ServiceType.Name != currentserviceName || service.ServiceType.TypeArgumentList.Count != currentTypeParameterCount) {
                    currentserviceName = service.ServiceType.Name;
                    currentTypeParameterCount = service.ServiceType.TypeArgumentList.Count;

                    builder.AppendIndent(indent);
                    builder.Append("return null;\n");
                    indent.DecreaseLevel(); // 3

                    builder.AppendIndent(indent);
                    builder.Append("case \"");
                    builder.Append(currentserviceName);
                    if (currentTypeParameterCount > 0) {
                        builder.Append('`');
                        builder.Append(currentTypeParameterCount);
                    }
                    builder.Append("\":\n");
                    indent.IncreaseLevel(); // 4
                }

                builder.AppendIndent(indent);
                builder.Append("if (serviceType == typeof(global::");
                builder.AppendClosedFullyQualified(service.ServiceType);
                builder.Append("))\n");
                indent.IncreaseLevel(); // 5

                builder.AppendIndent(indent);
                builder.Append("return ");

                Service? nextService = GetNextService();
                if (service.ServiceType != nextService?.ServiceType) {
                    builder.AppendServiceGetter(service);
                    builder.Append(";\n");
                }
                else {
                    builder.Append("(global::");
                    builder.AppendClosedFullyQualified(service.ServiceType);
                    builder.Append("[])[");

                    builder.AppendServiceGetter(service);
                    do {
                        builder.Append(", ");
                        builder.AppendServiceGetter(nextService!);
                        nextService = GetNextService();
                    }
                    while (service.ServiceType == nextService?.ServiceType);

                    builder.Append("];\n");
                }
                indent.DecreaseLevel(); // 4

                service = nextService;
            } while (service is not null);

            builder.AppendIndent(indent);
            builder.Append("return null;\n");
            indent.DecreaseLevel(); // 3
        }

        builder.AppendIndent(indent);
        builder.Append("default:\n");
        indent.IncreaseLevel(); // 4

        builder.AppendIndent(indent);
        builder.Append("return null;\n");
        indent.DecreaseLevel(); // 3

        indent.DecreaseLevel(); // 2
        builder.AppendIndent(indent);
        builder.Append("}\n");

        indent.DecreaseLevel(); // 1
        builder.AppendIndent(indent);
        builder.Append("}\n\n\n");
    }

    private struct ServiceListIterator(ServiceProvider serviceProvider) {
        private int index = -1;

        public Service? GetNextNotScoped() {
            for (index++; index < serviceProvider.SortedServiceList.Count; index++) {
                Service service = serviceProvider.SortedServiceList[index];

                if (service.Lifetime.HasFlag(ServiceLifetime.Scoped))
                    continue;
                if (service.Implementation.Type != MemberType.None && service.Implementation.IsScoped)
                    continue;

                return service;
            }

            return null;
        }

        public Service? GetNextService() {
            index++;
            if (index < serviceProvider.SortedServiceList.Count)
                return serviceProvider.SortedServiceList[index];
            else
                return null;
        }
    }
}
