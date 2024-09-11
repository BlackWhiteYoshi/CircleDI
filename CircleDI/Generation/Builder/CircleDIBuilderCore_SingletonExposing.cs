using CircleDI.Defenitions;
using CircleDI.Extensions;

namespace CircleDI.Generation;

/// <summary>
/// Contains the method to build the Singleton exposing getters for the ScopedProvider.
/// </summary>
public partial struct CircleDIBuilderCore {
    /// <summary>
    /// Singletons in ScopedProvider
    /// </summary>
    public void AppendSingletonExposing() {
        foreach (Service service in serviceProvider.SingletonList) {
            string refOrEmpty = (service.IsRefable && !serviceProvider.KeywordScope.HasFlag(TypeKeyword.Struct) && !serviceProvider.Keyword.HasFlag(TypeKeyword.Struct)) switch {
                true => "ref ",
                false => string.Empty
            };

            AppendServiceSummary(service);

            builder.AppendIndent(indent)
                .AppendInterpolation($"public {refOrEmpty}global::")
                .AppendClosedFullyQualified(service.ServiceType)
                .Append(' ')
                .AppendServiceGetter(service)
                .AppendInterpolation($" => {refOrEmpty}_")
                .AppendFirstLower(serviceProvider.Identifier.Name)
                .Append('.')
                .AppendServiceGetter(service)
                .Append(";\n\n");
        }
        builder.Append('\n');
    }
}
