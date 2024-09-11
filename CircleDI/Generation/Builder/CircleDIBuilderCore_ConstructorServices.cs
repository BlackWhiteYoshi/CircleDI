using CircleDI.Defenitions;
using CircleDI.Extensions;

namespace CircleDI.Generation;

/// <summary>
/// Contains the method to build the constructor or InitServices method, including the parameter fields and summary.
/// </summary>
public partial struct CircleDIBuilderCore {
    /// <summary>
    /// Appends<br />
    /// - the parameter fields<br />
    /// - constructor/InitServices() summary<br />
    /// - constructor parameterList<br />
    /// - singleton/scoped services initialization
    /// </summary>
    public void AppendConstructor() {
        // parameter fields
        {
            int i;
            if (isScopeProvider) {
                builder.AppendIndent(indent)
                    .AppendInterpolation($"private {readonlyStr}global::")
                    .AppendOpenFullyQualified(serviceProvider.Identifier)
                    .Append(" _")
                    .AppendFirstLower(serviceProvider.Identifier.Name)
                    .Append(";\n");
                i = 1;
            }
            else
                i = 0;

            for (; i < constructorParameterList.Count; i++)
                builder.AppendIndent(indent)
                    .AppendInterpolation($"private {readonlyStr}global::")
                    .AppendClosedFullyQualified(constructorParameterList[i].ServiceType!) // ConstructorParameterList items have always serviceType set
                    .Append(" _")
                    .AppendFirstLower(constructorParameterList[i].Name)
                    .Append(";\n");

            if (i > 0)
                builder.Append('\n');
        }

        // <summary> + method name
        AppendConstructionSummary();
        indent.IncreaseLevel(); // 2

        // constructor parameters
        {
            builder.Append('(');

            foreach (Dependency dependency in constructorParameterList)
                builder.Append("global::")
                    .AppendClosedFullyQualified(dependency.ServiceType ?? dependency.Service!.ServiceType)
                    .Append(' ')
                    .AppendFirstLower(dependency.Name)
                    .Append(", ");
            if (builder[^1] == ' ')
                builder.Length -= 2;

            builder.Append(") {\n");
        }

        // parameter field = parameter
        {
            int i;
            if (isScopeProvider) {
                builder.AppendIndent(indent)
                    .Append('_')
                    .AppendFirstLower(serviceProvider.Identifier.Name)
                    .Append(" = ");
                if (serviceProvider.HasInterface)
                    builder.Append("(global::")
                        .AppendOpenFullyQualified(serviceProvider.Identifier)
                        .Append(')');
                builder.AppendFirstLower(serviceProvider.Identifier.Name)
                    .Append(";\n");
                i = 1;
            }
            else
                i = 0;

            for (; i < constructorParameterList.Count; i++)
                builder.AppendIndent(indent)
                    .Append('_')
                    .AppendFirstLower(constructorParameterList[i].Name)
                    .Append(" = ")
                    .AppendFirstLower(constructorParameterList[i].Name)
                    .Append(";\n");

            if (i > 0)
                builder.Append('\n');
        }

        AppendConstructorServices();

        if (builder[^2] == '\n')
            builder.Length--;

        indent.DecreaseLevel(); // 1
        builder.AppendIndent(indent)
            .Append("}\n\n");
    }

    private void AppendConstructionSummary() {
        // constructor
        if (!hasConstructor) {
            // ServiceProvider
            if (!isScopeProvider) {
                builder.AppendIndent(indent)
                    .Append("/// <summary>\n");
                builder.AppendIndent(indent)
                    .Append("/// Creates an instance of a ServiceProvider together with all <see cref=\"global::CircleDIAttributes.CreationTiming.Constructor\">non-lazy</see> singleton services.\n");
                builder.AppendIndent(indent)
                    .Append("/// </summary>\n");
                builder.AppendIndent(indent)
                    .AppendInterpolation($"public {serviceProvider.Identifier.Name}");
            }
            // ScopeProvider
            else {
                AppendCreateScopeSummary();
                builder.AppendIndent(indent)
                    .Append("/// <param name=\"")
                    .AppendFirstLower(serviceProvider.Identifier.Name)
                    .Append("\">An instance of the service provider this provider is the scope of.");
                if (serviceProvider.HasInterface)
                    builder.Append(" It must be an instance of <see cref=\"")
                        .Append(serviceProvider.Identifier.Name)
                        .Append("\"/>.");
                builder.Append("</param>\n");

                builder.AppendIndent(indent)
                    .Append("public Scope");
            }
        }
        // InitServices()
        else {
            // ServiceProvider
            if (!isScopeProvider) {
                builder.AppendIndent(indent)
                    .Append("/// <summary>\n");
                builder.AppendIndent(indent)
                    .Append("/// Constructs non-lazy singleton services. Should be called inside the constructor at the end.\n");
                builder.AppendIndent(indent)
                    .Append("/// </summary>\n");
            }
            // ScopeProvider
            else {
                builder.AppendIndent(indent)
                    .Append("/// <summary>\n");
                builder.AppendIndent(indent)
                    .Append("/// Constructs non-lazy scoped services. Should be called inside the constructor at the end.\n");
                builder.AppendIndent(indent)
                    .Append("/// </summary>\n");
                builder.AppendIndent(indent)
                    .Append("/// <param name=\"")
                    .AppendFirstLower(serviceProvider.Identifier.Name)
                    .Append("\">\n");
                builder.AppendIndent(indent)
                    .Append("/// The ServiceProvider this ScopedProvider is created from.");
                if (serviceProvider.HasInterface) {
                    builder.AppendInterpolation($""" It must be an instance of <see cref="{serviceProvider.Identifier.Name}"/>.""");
                }
                builder.Append(" Usually it is the object you get injected to your constructor parameter:<br />\n");
                builder.AppendIndent(indent)
                    .AppendInterpolation($"/// public Scope([Dependency] {(serviceProvider.HasInterface ? serviceProvider.InterfaceIdentifier.Name : serviceProvider.Identifier.Name)} ")
                    .AppendFirstLower(serviceProvider.Identifier.Name)
                    .Append(") { ...\n");
                builder.AppendIndent(indent)
                    .Append("/// </param>\n");
            }

            // MemberNotNullAttribute
            {
                int initialLength = builder.Length;
                builder.AppendIndent(indent)
                    .Append("[System.Diagnostics.CodeAnalysis.MemberNotNull(");
                int startLength = builder.Length;

                foreach (ConstructorDependency dependency in constructorParameterList)
                    builder.Append("nameof(_")
                        .AppendFirstLower(dependency.Name)
                        .Append("), ");

                foreach (Service service in serviceList)
                    if (service.CreationTimeTransitive == CreationTiming.Constructor && service.Implementation.Type != MemberType.Field)
                        builder.Append("nameof(_")
                            .AppendFirstLower(service.Name)
                            .Append("), ");

                // Dispose lists
                if (hasDisposeList)
                    builder.Append("nameof(")
                        .AppendFirstLower(DISPOSE_LIST)
                        .Append("), ");
                if (hasAsyncDisposeList)
                    builder.Append("nameof(")
                        .AppendFirstLower(ASYNC_DISPOSE_LIST)
                        .Append("), ");

                if (builder.Length > startLength) {
                    builder.Length -= 2;
                    builder.Append(")]\n");
                }
                else
                    // rollback
                    builder.Length = initialLength;
            }

            builder.AppendIndent(indent)
                .Append("private void InitServices");
        }
    }
}
