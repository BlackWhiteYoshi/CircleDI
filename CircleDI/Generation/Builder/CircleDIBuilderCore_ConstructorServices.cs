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
                builder.AppendIndent(indent);
                builder.Append("private ");
                builder.Append(readonlyStr);
                builder.Append("global::");
                builder.AppendOpenFullyQualified(serviceProvider.Identifier);
                builder.Append(" _");
                builder.AppendFirstLower(serviceProvider.Identifier.Name);
                builder.Append(";\n");
                i = 1;
            }
            else
                i = 0;

            for (; i < constructorParameterList.Count; i++) {
                ConstructorDependency dependency = constructorParameterList[i];
                builder.AppendIndent(indent);
                builder.Append("private ");
                builder.Append(readonlyStr);
                builder.Append("global::");
                // ConstructorParameterList items have always serviceType set
                builder.AppendClosedFullyQualified(dependency.ServiceType!);
                builder.Append(" _");
                builder.AppendFirstLower(dependency.Name);
                builder.Append(";\n");
            }

            if (i > 0)
                builder.Append('\n');
        }

        // <summary> + method name
        AppendConstructionSummary();
        indent.IncreaseLevel(); // 2

        // constructor parameters
        {
            builder.Append('(');

            foreach (Dependency dependency in constructorParameterList) {
                builder.Append("global::");
                builder.AppendClosedFullyQualified(dependency.ServiceType ?? dependency.Service!.ServiceType);
                builder.Append(' ');
                builder.AppendFirstLower(dependency.Name);
                builder.Append(", ");
            }
            if (builder[^1] == ' ')
                builder.Length -= 2;

            builder.Append(") {\n");
        }

        // parameter field = parameter
        {
            int i;
            if (isScopeProvider) {
                builder.AppendIndent(indent);
                builder.Append('_');
                builder.AppendFirstLower(serviceProvider.Identifier.Name);
                builder.Append(" = ");
                if (serviceProvider.HasInterface) {
                    builder.Append("(global::");
                    builder.AppendOpenFullyQualified(serviceProvider.Identifier);
                    builder.Append(')');
                }
                builder.AppendFirstLower(serviceProvider.Identifier.Name);
                builder.Append(";\n");
                i = 1;
            }
            else
                i = 0;

            for (; i < constructorParameterList.Count; i++) {
                ConstructorDependency dependency = constructorParameterList[i];
                builder.AppendIndent(indent);
                builder.Append('_');
                builder.AppendFirstLower(dependency.Name);
                builder.Append(" = ");
                builder.AppendFirstLower(dependency.Name);
                builder.Append(";\n");
            }

            if (i > 0)
                builder.Append('\n');
        }

        AppendConstructorServices();

        if (builder[^2] == '\n')
            builder.Length--;

        indent.DecreaseLevel(); // 1
        builder.AppendIndent(indent);
        builder.Append("}\n\n");
    }

    private void AppendConstructionSummary() {
        // constructor
        if (!hasConstructor) {
            // ServiceProvider
            if (!isScopeProvider) {
                builder.AppendIndent(indent);
                builder.Append("/// <summary>\n");
                builder.AppendIndent(indent);
                builder.Append("/// Creates an instance of a ServiceProvider together with all <see cref=\"global::CircleDIAttributes.CreationTiming.Constructor\">non-lazy</see> singleton services.\n");
                builder.AppendIndent(indent);
                builder.Append("/// </summary>\n");
                builder.AppendIndent(indent);
                builder.Append("public ");
                builder.Append(serviceProvider.Identifier.Name);
            }
            // ScopeProvider
            else {
                AppendCreateScopeSummary();
                builder.AppendIndent(indent);
                builder.Append("/// <param name=\"");
                builder.AppendFirstLower(serviceProvider.Identifier.Name);
                builder.Append("\">An instance of the service provider this provider is the scope of.");
                if (serviceProvider.HasInterface) {
                    builder.Append(" It must be an instance of <see cref=\"");
                    builder.Append(serviceProvider.Identifier.Name);
                    builder.Append("\"/>.");
                }
                builder.Append("</param>\n");
                builder.AppendIndent(indent);
                builder.Append("public Scope");
            }
        }
        // InitServices()
        else {
            // ServiceProvider
            if (!isScopeProvider) {
                builder.AppendIndent(indent);
                builder.Append("/// <summary>\n");
                builder.AppendIndent(indent);
                builder.Append("/// Constructs non-lazy singleton services. Should be called inside the constructor at the end.\n");
                builder.AppendIndent(indent);
                builder.Append("/// </summary>\n");
            }
            // ScopeProvider
            else {
                builder.AppendIndent(indent);
                builder.Append("/// <summary>\n");
                builder.AppendIndent(indent);
                builder.Append("/// Constructs non-lazy scoped services. Should be called inside the constructor at the end.\n");
                builder.AppendIndent(indent);
                builder.Append("/// </summary>\n");
                builder.AppendIndent(indent);
                builder.Append("/// <param name=\"");
                builder.AppendFirstLower(serviceProvider.Identifier.Name);
                builder.Append("\">\n");
                builder.AppendIndent(indent);
                builder.Append("/// The ServiceProvider this ScopedProvider is created from.");
                if (serviceProvider.HasInterface) {
                    builder.Append(" It must be an instance of <see cref=\"");
                    builder.Append(serviceProvider.Identifier.Name);
                    builder.Append("\"/>.");
                }
                builder.Append(" Usually it is the object you get injected to your constructor parameter:<br />\n");
                builder.AppendIndent(indent);
                builder.Append("/// public Scope([Dependency] ");
                if (serviceProvider.HasInterface)
                    builder.Append(serviceProvider.InterfaceIdentifier.Name);
                else
                    builder.Append(serviceProvider.Identifier.Name);
                builder.Append(' ');
                builder.AppendFirstLower(serviceProvider.Identifier.Name);
                builder.Append(") { ...\n");
                builder.AppendIndent(indent);
                builder.Append("/// </param>\n");
            }

            // MemberNotNullAttribute
            {
                int initialLength = builder.Length;
                builder.AppendIndent(indent);
                builder.Append("[System.Diagnostics.CodeAnalysis.MemberNotNull(");
                int startLength = builder.Length;

                foreach (ConstructorDependency dependency in constructorParameterList) {
                    builder.Append("nameof(_");
                    builder.AppendFirstLower(dependency.Name);
                    builder.Append("), ");
                }

                foreach (Service service in serviceList) {
                    if (service.CreationTimeTransitive == CreationTiming.Constructor && service.Implementation.Type != MemberType.Field) {
                        builder.Append("nameof(_");
                        builder.AppendFirstLower(service.Name);
                        builder.Append("), ");
                    }
                }

                // Dispose lists
                if (hasDisposeList) {
                    builder.Append("nameof(");
                    builder.AppendFirstLower(DISPOSE_LIST);
                    builder.Append("), ");
                }
                if (hasAsyncDisposeList) {
                    builder.Append("nameof(");
                    builder.AppendFirstLower(ASYNC_DISPOSE_LIST);
                    builder.Append("), ");
                }

                if (builder.Length > startLength) {
                    builder.Length -= 2;
                    builder.Append(")]\n");
                }
                else
                    builder.Length = initialLength;
            }

            builder.AppendIndent(indent);
            builder.Append("private void InitServices");
        }
    }
}
