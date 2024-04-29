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

            builder.AppendIndent(indent);
            builder.Append("public ");
            builder.Append(refOrEmpty);
            builder.Append("global::");
            builder.AppendClosedFullyQualified(service.ServiceType);
            builder.Append(' ');
            builder.AppendServiceGetter(service);
            builder.Append(" => ");
            builder.Append(refOrEmpty);
            builder.Append('_');
            builder.AppendFirstLower(serviceProvider.Identifier.Name);
            builder.Append('.');
            builder.AppendServiceGetter(service);
            builder.Append(";\n\n");
        }
        builder.Append('\n');
    }
}
