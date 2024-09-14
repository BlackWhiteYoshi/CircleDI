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
        builder.AppendInterpolation($$"""
            {{indent}}/// <summary>
            {{indent}}/// <para>Finds all registered services of the given type.</para>
            {{indent}}/// <para>
            {{indent}}/// The method returns<br />
            {{indent}}/// - null (when registered zero times)<br />
            {{indent}}/// - given type (when registered ones)<br />
            {{indent}}/// - Array of given type (when registered many times)
            {{indent}}/// </para>
            {{indent}}/// </summary>
            {{indent}}object? IServiceProvider.GetService(Type serviceType) {

            """);
        indent.IncreaseLevel(); // 2

        builder.AppendInterpolation($"{indent}switch (serviceType.Name) {{\n");
        indent.IncreaseLevel(); // 3

        if (serviceEnumerator.MoveNext()) {
            Service? service = serviceEnumerator.Current;
            string currentserviceName = service.ServiceType.Name;
            int currentTypeParameterCount = service.ServiceType.TypeArgumentList.Count;

            if (currentTypeParameterCount == 0)
                builder.AppendInterpolation($"{indent}case \"{currentserviceName}\":\n");
            else
                builder.AppendInterpolation($"{indent}case \"{currentserviceName}`{currentTypeParameterCount}\":\n");
            indent.IncreaseLevel(); // 4

            do {
                if (service.ServiceType.Name != currentserviceName || service.ServiceType.TypeArgumentList.Count != currentTypeParameterCount) {
                    currentserviceName = service.ServiceType.Name;
                    currentTypeParameterCount = service.ServiceType.TypeArgumentList.Count;

                    builder.AppendInterpolation($"{indent}return null;\n");
                    indent.DecreaseLevel(); // 3

                    if (currentTypeParameterCount == 0)
                        builder.AppendInterpolation($"{indent}case \"{currentserviceName}\":\n");
                    else
                        builder.AppendInterpolation($"{indent}case \"{currentserviceName}`{currentTypeParameterCount}\":\n");
                    indent.IncreaseLevel(); // 4
                }

                builder.AppendInterpolation($"{indent}if (serviceType == typeof(global::{service.ServiceType.AsClosedFullyQualified()}))\n");
                indent.IncreaseLevel(); // 5

                Service? nextService = serviceEnumerator.MoveNext() ? serviceEnumerator.Current : null;
                if (service.ServiceType != nextService?.ServiceType)
                    builder.AppendInterpolation($"{indent}return {service.AsServiceGetter()};\n");
                else {
                    builder.AppendInterpolation($"{indent}return (global::{service.ServiceType.AsClosedFullyQualified()}[])[{service.AsServiceGetter()}");
                    do {
                        builder.AppendInterpolation($", {nextService!.AsServiceGetter()}");
                        nextService = serviceEnumerator.MoveNext() ? serviceEnumerator.Current : null;
                    }
                    while (service.ServiceType == nextService?.ServiceType);
                    builder.Append("];\n");
                }
                indent.DecreaseLevel(); // 4

                service = nextService;
            } while (service is not null);

            builder.AppendInterpolation($"{indent}return null;\n");
            indent.DecreaseLevel(); // 3
        }

        builder.AppendInterpolation($"{indent}default:\n");
        indent.IncreaseLevel(); // 4

        builder.AppendInterpolation($"{indent}return null;\n");
        indent.DecreaseLevel(); // 3

        indent.DecreaseLevel(); // 2
        builder.AppendInterpolation($"{indent}}}\n");

        indent.DecreaseLevel(); // 1
        builder.AppendInterpolation($"{indent}}}\n\n\n");

        serviceEnumerator.Dispose();
    }
}
