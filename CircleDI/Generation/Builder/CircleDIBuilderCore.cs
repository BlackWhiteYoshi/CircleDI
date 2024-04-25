using CircleDI.Defenitions;
using CircleDI.Extensions;
using System.Text;

namespace CircleDI.Generation;

/// <summary>
/// Contains the build functionalities to build class and interface that have ServiceProvider and ScopeProvider in common.
/// </summary>
/// <param name="builder"></param>
/// <param name="serviceProvider"></param>
public struct CircleDIBuilderCore {
    private readonly StringBuilder builder;
    private readonly ServiceProvider serviceProvider;

    private List<Service> serviceList;
    private List<ConstructorDependency> constructorParameterList;
    private TypeKeyword keyword;
    private DisposeGeneration generateDisposeMethods;
    private bool hasConstructor;
    private bool hasDisposeMethod;
    private bool hasDisposeAsyncMethod;
    private bool threadSafe;

    private string readonlyStr;
    private bool hasDisposeList;
    private bool hasAsyncDisposeList;

    private bool isScopeProvider = false;
    public Indent indent = new();

    public CircleDIBuilderCore(StringBuilder builder, ServiceProvider serviceProvider) {
        this.builder = builder;
        this.serviceProvider = serviceProvider;

        serviceList = serviceProvider.SingletonList;
        constructorParameterList = serviceProvider.ConstructorParameterList;
        keyword = serviceProvider.Keyword;
        generateDisposeMethods = serviceProvider.GenerateDisposeMethods;
        hasConstructor = serviceProvider.HasConstructor;
        hasDisposeMethod = serviceProvider.HasDisposeMethod;
        hasDisposeAsyncMethod = serviceProvider.HasDisposeAsyncMethod;
        threadSafe = serviceProvider.ThreadSafe;

        readonlyStr = hasConstructor switch {
            true => "",
            false => "readonly "
        };

        if (generateDisposeMethods == DisposeGeneration.NoDisposing) {
            hasDisposeList = false;
            hasAsyncDisposeList = false;
        }
        else
            foreach (Service service in serviceProvider.TransientList) {
                if (service.Lifetime is ServiceLifetime.TransientScoped)
                    continue;

                if (service.IsAsyncDisposable) {
                    hasAsyncDisposeList = true;
                    if (hasDisposeList)
                        break;
                }
                else if (service.IsDisposable) {
                    hasDisposeList = true;
                    if (hasAsyncDisposeList)
                        break;
                }
            }
    }

    /// <summary>
    /// Sets all related ServiceProvider fields to ScopeProvider equivalents and increases indentation level by 1.
    /// </summary>
    public void SetToScope() {
        isScopeProvider = true;
        indent.IncreaseLevel();

        serviceList = serviceProvider.ScopedList;
        constructorParameterList = serviceProvider.ConstructorParameterListScope;
        keyword = serviceProvider.KeywordScope;
        generateDisposeMethods = serviceProvider.GenerateDisposeMethodsScope;
        hasConstructor = serviceProvider.HasConstructorScope;
        hasDisposeMethod = serviceProvider.HasDisposeMethodScope;
        hasDisposeAsyncMethod = serviceProvider.HasDisposeAsyncMethodScope;
        threadSafe = serviceProvider.ThreadSafeScope;

        readonlyStr = hasConstructor switch {
            true => "",
            false => "readonly "
        };

        if (generateDisposeMethods == DisposeGeneration.NoDisposing) {
            hasDisposeList = false;
            hasAsyncDisposeList = false;
        }
        else
            foreach (Service service in serviceProvider.TransientList)
                if (service.IsAsyncDisposable) {
                    hasAsyncDisposeList = true;
                    if (hasDisposeList)
                        break;
                }
                else if (service.IsDisposable) {
                    hasDisposeList = true;
                    if (hasAsyncDisposeList)
                        break;
                }
    }


    #region Constructor Services

    /// <summary>
    /// Appends<br />
    /// - the parameter fields<br />
    /// - constructor/InitServices() summary<br />
    /// - constructor parameterList<br />
    /// - singleton/scoped services initialization
    /// </summary>
    public readonly void AppendConstructor() {
        // parameter fields
        {
            int i;
            if (isScopeProvider) {
                builder.AppendIndent(indent, 1);
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
                builder.AppendIndent(indent, 1);
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

        // constructor parameters
        AppendParameterDependencyList(constructorParameterList);
        builder.Append(" {\n");

        // parameter field = parameter
        {
            int i;
            if (isScopeProvider) {
                builder.AppendIndent(indent, 2);
                builder.Append('_');
                builder.AppendFirstLower(serviceProvider.Identifier.Name);
                builder.Append(" = ");
                if (serviceProvider.HasInterface)
                {
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
                builder.AppendIndent(indent, 2);
                builder.Append('_');
                builder.AppendFirstLower(dependency.Name);
                builder.Append(" = ");
                builder.AppendFirstLower(dependency.Name);
                builder.Append(";\n");
            }

            if (i > 0)
                builder.Append('\n');
        }

        // singleton/scoped services inizialization
        {
            ServiceTreeBuilder serviceTreeBuilder = new(this, 2);
            int currentPosition = builder.Length;

            serviceProvider.NextDependencyTreeFlag();
            foreach (Service service in serviceList)
                if (service.CreationTimeTransitive is CreationTiming.Constructor)
                    serviceTreeBuilder.AppendServiceTree(service, CreationTiming.Constructor);

            if (builder.Length > currentPosition) {
                builder.Append('\n');
                currentPosition = builder.Length;
            }

            serviceProvider.NextDependencyTreeFlag();
            foreach (Service service in serviceList)
                if (service.CreationTimeTransitive is CreationTiming.Constructor)
                    serviceTreeBuilder.AppendCircularDependencies(service);

            if (builder.Length > currentPosition)
                builder.Append("\n");

            serviceTreeBuilder.AppendDiposeListsConstructor();
        }

        if (builder[^2] == '\n')
            builder.Length--;

        builder.AppendIndent(indent, 1);
        builder.Append("}\n\n");
    }

    private readonly void AppendConstructionSummary() {
        // constructor
        if (!hasConstructor) {
            // ServiceProvider
            if (!isScopeProvider) {
                builder.AppendIndent(indent, 1);
                builder.Append("/// <summary>\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// Creates an instance of a ServiceProvider together with all <see cref=\"global::CircleDIAttributes.CreationTiming.Constructor\">non-lazy</see> singleton services.\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// </summary>\n");
                builder.AppendIndent(indent, 1);
                builder.Append("public ");
                builder.Append(serviceProvider.Identifier.Name);
            }
            // ScopeProvider
            else {
                AppendCreateScopeSummary();
                builder.AppendIndent(indent, 1);
                builder.Append("/// <param name=\"");
                builder.AppendFirstLower(serviceProvider.Identifier.Name);
                builder.Append("\">An instance of the service provider this provider is the scope of.");
                if (serviceProvider.HasInterface) {
                    builder.Append(" It must be an instance of <see cref=\"");
                    builder.Append(serviceProvider.Identifier.Name);
                    builder.Append("\"/>.");
                }
                builder.Append("</param>\n");
                builder.AppendIndent(indent, 1);
                builder.Append("public Scope");
            }
        }
        // InitServices()
        else {
            // ServiceProvider
            if (!isScopeProvider) {
                builder.AppendIndent(indent, 1);
                builder.Append("/// <summary>\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// Constructs non-lazy singleton services. Should be called inside the constructor at the end.\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// </summary>\n");
            }
            // ScopeProvider
            else {
                builder.AppendIndent(indent, 1);
                builder.Append("/// <summary>\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// Constructs non-lazy scoped services. Should be called inside the constructor at the end.\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// </summary>\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// <param name=\"");
                builder.AppendFirstLower(serviceProvider.Identifier.Name);
                builder.Append("\">\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// The ServiceProvider this ScopedProvider is created from.");
                if (serviceProvider.HasInterface) {
                    builder.Append(" It must be an instance of <see cref=\"");
                    builder.Append(serviceProvider.Identifier.Name);
                    builder.Append("\"/>.");
                }
                builder.Append(" Usually it is the object you get injected to your constructor parameter:<br />\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// public Scope([Dependency] ");
                if (serviceProvider.HasInterface)
                    builder.Append(serviceProvider.InterfaceIdentifier.Name);
                else
                    builder.Append(serviceProvider.Identifier.Name);
                builder.Append(' ');
                builder.AppendFirstLower(serviceProvider.Identifier.Name);
                builder.Append(") { ...\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// </param>\n");
            }

            // MemberNotNullAttribute
            {
                int initialLength = builder.Length;
                builder.AppendIndent(indent, 1);
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

            builder.AppendIndent(indent, 1);
            builder.Append("private void InitServices");
        }
    }

    #endregion


    #region CreateScope()

    /// <summary>
    /// "special" method CreateScope()
    /// </summary>
    public readonly void AppendCreateScope() {
        if (serviceProvider.GenerateScope) {
            AppendCreateScopeSummary();

            builder.AppendIndent(indent, 1);
            builder.Append("public global::");
            if (serviceProvider.HasInterface)
                builder.AppendOpenFullyQualified(serviceProvider.InterfaceIdentifierScope);
            else
                builder.AppendOpenFullyQualified(serviceProvider.IdentifierScope);
            builder.Append(" CreateScope");
            builder.AppendOpenGenerics(serviceProvider.IdentifierScope);
            AppendParameterDependencyList(serviceProvider.CreateScope.ConstructorDependencyList.Concat<Dependency>(serviceProvider.CreateScope.PropertyDependencyList).Where((Dependency dependency) => !dependency.HasAttribute));
            builder.Append(" {\n");

            
            ServiceTreeBuilder serviceTreeBuilder = new(this, 2);

            serviceProvider.NextDependencyTreeFlag();
            serviceTreeBuilder.AppendCreateScopeServiceTree();

            serviceProvider.NextDependencyTreeFlag();
            serviceTreeBuilder.AppendCreateScopeCircularDependencies();

            serviceTreeBuilder.AppendDisposeLists();


            builder.AppendIndent(indent, 2);
            builder.Append("return new global::");
            builder.AppendOpenFullyQualified(serviceProvider.IdentifierScope);
            // AppendConstructorDependencyList of serviceProvider.CreateScope
            {
                builder.Append('(');
                if (serviceProvider.CreateScope.ConstructorDependencyList.Count > 0) {
                    foreach (ConstructorDependency dependency in serviceProvider.CreateScope.ConstructorDependencyList) {
                        if (!dependency.HasAttribute)
                            builder.AppendFirstLower(dependency.Name);
                        else {
                            if (dependency.Service!.IsRefable && !serviceProvider.Keyword.HasFlag(TypeKeyword.Struct))
                                builder.Append(dependency.ByRef.AsString());
                            else
                                AppendDependencyIdentifier(serviceProvider.CreateScope, dependency);
                        }
                        builder.Append(", ");
                    }
                    builder.Length -= 2;
                }
                builder.Append(')');
            }
            // AppendPropertyDependencyList of serviceProvider.CreateScope
            if (serviceProvider.CreateScope.PropertyDependencyList.Count > 0) {
                builder.Append(" {");
                foreach (PropertyDependency dependency in serviceProvider.CreateScope.PropertyDependencyList) {
                    builder.Append('\n');
                    builder.AppendIndent(indent, 3);
                    builder.Append(dependency.Name);
                    builder.Append(" = ");
                    if (!dependency.HasAttribute)
                        builder.AppendFirstLower(dependency.Name);
                    else
                        AppendDependencyIdentifier(serviceProvider.CreateScope, dependency);
                    builder.Append(',');
                }
                builder[^1] = '\n'; // remove last ','
                builder.AppendIndent(indent, 2);
                builder.Append('}');
            }
            builder.Append(";\n");

            builder.AppendIndent(indent, 1);
            builder.Append("}\n\n\n");
        }
    }

    #endregion


    #region Singleton/Scoped Fields and Getter

    /// <summary>
    /// Singletons/Scoped service Getter
    /// </summary>
    public readonly void AppendServicesGetter() {
        if (serviceList.Count > 0) {
            foreach (Service service in serviceList) {
                string refOrEmpty = (service.IsRefable && !keyword.HasFlag(TypeKeyword.Struct)) switch {
                    true => "ref ",
                    false => string.Empty
                };

                AppendServiceSummary(service);
                builder.AppendIndent(indent, 1);
                builder.Append("public ");
                builder.Append(refOrEmpty);
                builder.Append("global::");
                builder.AppendClosedFullyQualified(service.ServiceType);
                builder.Append(' ');
                builder.AppendServiceGetter(service);

                if (service.Implementation.Type == MemberType.Field)
                    AppendServicesGetterField(service, refOrEmpty);
                else if (service.CreationTimeTransitive == CreationTiming.Constructor)
                    AppendServicesGetterConstructor(service, refOrEmpty);
                else
                    AppendServicesGetterLazy(service, refOrEmpty);

                builder.Append('\n');
            }

            builder.Append('\n');
        }
    }

    private readonly void AppendServicesGetterField(Service service, string refOrEmpty) {
        builder.Append(" => ");
        builder.Append(refOrEmpty);
        if (service.Lifetime == ServiceLifetime.Scoped && !service.Implementation.IsScoped && !service.Implementation.IsStatic)
            AppendServiceProviderField();
        builder.AppendImplementationName(service);
        builder.Append(";\n");
    }

    private readonly void AppendServicesGetterConstructor(Service service, string refOrEmpty) {
        builder.Append(" => ");
        builder.Append(refOrEmpty);
        builder.Append('_');
        builder.AppendFirstLower(service.Name);
        builder.Append(";\n");

        builder.AppendIndent(indent, 1);
        builder.Append("private ");
        builder.Append(readonlyStr);
        builder.Append("global::");
        builder.AppendClosedFullyQualified(service.ImplementationType);
        builder.Append(" _");
        builder.AppendFirstLower(service.Name);
        builder.Append(";\n");
    }

    private readonly void AppendServicesGetterLazy(Service service, string refOrEmpty) {
        builder.Append(" {\n");

        int extraIndent = 0;
        if (service.GetAccessor == GetAccess.Property) {
            builder.AppendIndent(indent, 2);
            builder.Append("get {\n");
            extraIndent = 1;
        }


        int moreExtraIndent = 0;
        if (threadSafe) {
            builder.AppendIndent(indent, 2 + extraIndent);
            builder.Append("if (_");
            builder.AppendFirstLower(service.Name);
            builder.Append(" is null) {\n");

            builder.AppendIndent(indent, 3 + extraIndent);
            builder.Append("lock (this)\n");

            moreExtraIndent = 2;
        }

        builder.AppendIndent(indent, 2 + extraIndent + moreExtraIndent);
        builder.Append("if (_");
        builder.AppendFirstLower(service.Name);
        builder.Append(" is null) {\n");

        ServiceTreeBuilder serviceTreeBuilder = new(this, 3 + extraIndent + moreExtraIndent);

        serviceProvider.NextDependencyTreeFlag();
        serviceTreeBuilder.AppendServiceTree(service, CreationTiming.Lazy);

        if (threadSafe) {
            builder.AppendIndent(indent, 2 + extraIndent + moreExtraIndent);
            builder.Append("}\n");
            serviceTreeBuilder.indentation = 3 + extraIndent;
        }

        serviceProvider.NextDependencyTreeFlag();
        serviceTreeBuilder.AppendCircularDependencies(service);

        serviceTreeBuilder.AppendDisposeLists();

        builder.AppendIndent(indent, 2 + extraIndent);
        builder.Append("}\n");

        builder.AppendIndent(indent, 2 + extraIndent);
        builder.Append("return ");
        builder.Append(refOrEmpty);
        builder.Append("(global::");
        builder.AppendClosedFullyQualified(service.ServiceType);
        builder.Append(")_");
        builder.AppendFirstLower(service.Name);
        builder.Append(";\n");

        if (service.GetAccessor == GetAccess.Property) {
            builder.AppendIndent(indent, 2);
            builder.Append("}\n");
        }

        builder.AppendIndent(indent, 1);
        builder.Append("}\n");

        builder.AppendIndent(indent, 1);
        builder.Append("private ");
        builder.Append("global::");
        builder.AppendClosedFullyQualified(service.ImplementationType);
        builder.Append("? _");
        builder.AppendFirstLower(service.Name);
        builder.Append(";\n");
    }

    #endregion


    #region Transient Getter

    /// <summary>
    /// Transients service Getter
    /// </summary>
    /// <returns></returns>
    public readonly void AppendServicesTransient() {
        int currentPosition = builder.Length;

        foreach (Service service in serviceProvider.TransientList) {
            if (!isScopeProvider && service.Lifetime is ServiceLifetime.TransientScoped)
                continue;

            AppendServiceSummary(service);
            builder.AppendIndent(indent, 1);
            builder.Append("public global::");
            builder.AppendClosedFullyQualified(service.ServiceType);
            builder.Append(' ');

            if (service.GetAccessor == GetAccess.Property) {
                builder.Append(service.Name);
                builder.Append(" {\n");
                builder.AppendIndent(indent, 2);
                builder.Append("get {\n");
                AppendTransientGetter(service, 3);
                builder.AppendIndent(indent, 2);
                builder.Append("}\n");
                builder.AppendIndent(indent, 1);
                builder.Append("}\n");
            }
            else {
                builder.Append("Get");
                builder.Append(service.Name);
                builder.Append("() {\n");
                AppendTransientGetter(service, 2);
                builder.AppendIndent(indent, 1);
                builder.Append("}\n");
            }

            builder.Append('\n');
        }

        if (builder.Length > currentPosition)
            builder.Append('\n');
    }

    private readonly void AppendTransientGetter(Service service, int indentation) {
        ServiceTreeBuilder serviceTreeBuilder = new(this, indentation);

        serviceProvider.NextDependencyTreeFlag();
        serviceTreeBuilder.AppendServiceTree(service, CreationTiming.Lazy);

        serviceProvider.NextDependencyTreeFlag();
        serviceTreeBuilder.AppendCircularDependencies(service);

        serviceTreeBuilder.AppendDisposeLists();

        builder.AppendIndent(indent, indentation);
        builder.Append("return ");
        builder.AppendFirstLower(service.Name);
        builder.Append(";\n");
    }

    #endregion


    #region Delegate Getter

    /// <summary>
    /// Delegates service Getter
    /// </summary>
    public readonly void AppendServicesDelegate() {
        int currentPosition = builder.Length;

        foreach (Service service in serviceProvider.DelegateList) {
            if (!isScopeProvider && service.Implementation.IsScoped)
                continue;

            AppendServiceSummary(service);
            builder.AppendIndent(indent, 1);
            builder.Append("public global::");
            builder.AppendClosedFullyQualified(service.ServiceType);
            builder.Append(' ');
            builder.AppendServiceGetter(service);
            builder.Append(" => ");
            if (isScopeProvider && !service.Implementation.IsScoped && !service.Implementation.IsStatic)
                AppendServiceProviderField();
            builder.AppendImplementationName(service);
            builder.Append(";\n\n");
        }

        if (builder.Length > currentPosition)
            builder.Append('\n');
    }

    #endregion


    #region IServiceProvider GetService()

    /// <summary>
    /// <see cref="IServiceProvider.GetService(Type)"/> switch content filtered to Singleton and Transient services.
    /// </summary>
    public readonly void AppendIServiceProviderNotScoped()
        => AppendIServiceProvider(new ServiceListIterator(serviceProvider).GetNextNotScoped);

    /// <summary>
    /// <see cref="IServiceProvider.GetService(Type)"/> switch content with all services except ServiceProvider only services.
    /// </summary>
    public readonly void AppendIServiceProviderAllServices()
        => AppendIServiceProvider(new ServiceListIterator(serviceProvider).GetNextService);

    private readonly void AppendIServiceProvider(Func<Service?> GetNextService) {
        builder.AppendIndent(indent, 1);
        builder.Append("/// <summary>\n");
        builder.AppendIndent(indent, 1);
        builder.Append("/// <para>Finds all registered services of the given type.</para>\n");
        builder.AppendIndent(indent, 1);
        builder.Append("/// <para>\n");
        builder.AppendIndent(indent, 1);
        builder.Append("/// The method returns<br />\n");
        builder.AppendIndent(indent, 1);
        builder.Append("/// - null (when registered zero times)<br />\n");
        builder.AppendIndent(indent, 1);
        builder.Append("/// - given type (when registered ones)<br />\n");
        builder.AppendIndent(indent, 1);
        builder.Append("/// - Array of given type (when registered many times)\n");
        builder.AppendIndent(indent, 1);
        builder.Append("/// </para>\n");
        builder.AppendIndent(indent, 1);
        builder.Append("/// </summary>\n");
        builder.AppendIndent(indent, 1);
        builder.Append("object? IServiceProvider.GetService(Type serviceType) {\n");
        builder.AppendIndent(indent, 2);
        builder.Append("switch (serviceType.Name) {\n");

        Service? service = GetNextService();
        if (service is not null) {
            string currentserviceName = service.ServiceType.Name;
            int currentTypeParameterCount = service.ServiceType.TypeArgumentList.Count;
            builder.AppendIndent(indent, 3);
            builder.Append("case \"");
            builder.Append(currentserviceName);
            if (currentTypeParameterCount > 0) {
                builder.Append('`');
                builder.Append(currentTypeParameterCount);
            }
            builder.Append("\":\n");

            do {
                if (service.ServiceType.Name != currentserviceName || service.ServiceType.TypeArgumentList.Count != currentTypeParameterCount) {
                    currentserviceName = service.ServiceType.Name;
                    currentTypeParameterCount = service.ServiceType.TypeArgumentList.Count;

                    builder.AppendIndent(indent, 4);
                    builder.Append("return null;\n");
                    builder.AppendIndent(indent, 3);
                    builder.Append("case \"");
                    builder.Append(currentserviceName);
                    if (currentTypeParameterCount > 0) {
                        builder.Append('`');
                        builder.Append(currentTypeParameterCount);
                    }
                    builder.Append("\":\n");
                }

                builder.AppendIndent(indent, 4);
                builder.Append("if (serviceType == typeof(global::");
                builder.AppendClosedFullyQualified(service.ServiceType);
                builder.Append("))\n");

                builder.AppendIndent(indent, 5);
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

                service = nextService;
            } while (service is not null);

            builder.AppendIndent(indent, 4);
            builder.Append("return null;\n");
        }

        builder.AppendIndent(indent, 3);
        builder.Append("default:\n");
        builder.AppendIndent(indent, 4);
        builder.Append("return null;\n");

        builder.AppendIndent(indent, 2);
        builder.Append("}\n");

        builder.AppendIndent(indent, 1);
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

    #endregion


    #region Dispose

    private const string DISPOSE_LIST = "_disposeList";
    private const string ASYNC_DISPOSE_LIST = "_asyncDisposeList";

    /// <summary>
    /// Generates the Dispose/AsyncDispose methods and the corresponding disposeLists for the transient services.
    /// </summary>
    /// <param name="hasDisposeList"></param>
    /// <param name="hasAsyncDisposeList"></param>
    public readonly void AppendDisposeMethods() {
        if (generateDisposeMethods == DisposeGeneration.NoDisposing)
            return;


        // disposeList
        if (hasDisposeList) {
            builder.AppendIndent(indent, 1);
            builder.Append("private ");
            builder.Append(readonlyStr);
            builder.Append($"global::System.Collections.Generic.List<IDisposable> {DISPOSE_LIST};\n\n");
        }

        // asyncDisposeList
        if (hasAsyncDisposeList) {
            builder.AppendIndent(indent, 1);
            builder.Append("private ");
            builder.Append(readonlyStr);
            builder.Append($"global::System.Collections.Generic.List<IAsyncDisposable> {ASYNC_DISPOSE_LIST};\n\n");
        }


        uint singeltonDisposablesCount = 0;
        uint singeltonAsyncDisposablesCount = 0;

        // Dispose()
        if (generateDisposeMethods.HasFlag(DisposeGeneration.Dispose)) {
            if (!hasDisposeMethod) {
                builder.AppendIndent(indent, 1);
                builder.Append("/// <summary>\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// Disposes all disposable services instantiated by this provider.\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// </summary>\n");
                builder.AppendIndent(indent, 1);
                builder.Append("public void Dispose() {\n");
            }
            else {
                builder.AppendIndent(indent, 1);
                builder.Append("/// <summary>\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// Disposes all disposable services instantiated by this provider. Should be called inside the Dispose() method.\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// </summary>\n");
                builder.AppendIndent(indent, 1);
                builder.Append("private void DisposeServices() {\n");
            }


            foreach (Service service in serviceList)
                if (service.IsDisposable) {
                    if (service.IsAsyncDisposable)
                        singeltonAsyncDisposablesCount++;
                    else
                        singeltonDisposablesCount++;
                    AppendDispose(service);
                }
                else if (service.IsAsyncDisposable) {
                    singeltonAsyncDisposablesCount++;
                    builder.AppendIndent(indent, 2);
                    builder.Append("_ = (");
                    if (service.CreationTimeTransitive == CreationTiming.Constructor) {
                        builder.Append("(IAsyncDisposable)");
                        builder.AppendServiceField(service);
                        builder.Append(')');
                    }
                    else {
                        builder.AppendServiceField(service);
                        builder.Append(" as IAsyncDisposable)?");
                    }
                    builder.Append(".DisposeAsync().Preserve();\n");
                }
            if ((singeltonDisposablesCount | singeltonAsyncDisposablesCount) > 0)
                builder.Append('\n');

            if (hasDisposeList)
                AppendDisposingDisposeList();

            if (hasAsyncDisposeList)
                AppendDisposingAsyncDisposeListDiscard();

            if (builder[^2] == '\n')
                builder.Length--;
            builder.AppendIndent(indent, 1);
            builder.Append("}\n\n");
        }

        // DisposeAsync()
        if (generateDisposeMethods.HasFlag(DisposeGeneration.DisposeAsync)) {
            if (!hasDisposeAsyncMethod) {
                builder.AppendIndent(indent, 1);
                builder.Append("/// <summary>\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// Disposes all disposable services instantiated by this provider asynchronously.\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// </summary>\n");
                builder.AppendIndent(indent, 1);
                builder.Append("public ValueTask DisposeAsync() {\n");
            }
            else {
                builder.AppendIndent(indent, 1);
                builder.Append("/// <summary>\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// Disposes all disposable services instantiated by this provider asynchronously. Should be called inside the DisposeAsync() method.\n");
                builder.AppendIndent(indent, 1);
                builder.Append("/// </summary>\n");
                builder.AppendIndent(indent, 1);
                builder.Append("private ValueTask DisposeServicesAsync() {\n");
            }


            switch ((singeltonAsyncDisposablesCount, hasAsyncDisposeList)) {
                case (0, false): {
                    if (singeltonDisposablesCount > 0) {
                        foreach (Service service in serviceList)
                            if (service.IsDisposable)
                                AppendDispose(service);
                        builder.Append('\n');
                    }

                    if (hasDisposeList)
                        AppendDisposingDisposeList();

                    builder.AppendIndent(indent, 2);
                    builder.Append("return default;\n");
                    break;
                }
                case (1, false): {
                    Service asyncDisposableService = serviceList.First((Service service) => service.IsAsyncDisposable);
                    if (singeltonDisposablesCount > 0) {
                        foreach (Service service in serviceList)
                            if (service.IsDisposable && service != asyncDisposableService)
                                AppendDispose(service);
                        builder.Append('\n');
                    }

                    if (hasDisposeList)
                        AppendDisposingDisposeList();

                    builder.AppendIndent(indent, 2);
                    builder.Append("return (");
                    if (asyncDisposableService.CreationTimeTransitive == CreationTiming.Constructor) {
                        builder.Append("(IAsyncDisposable)");
                        builder.AppendServiceField(asyncDisposableService);
                        builder.Append(").DisposeAsync();\n");
                    }
                    else {
                        builder.AppendServiceField(asyncDisposableService);
                        builder.Append(" as IAsyncDisposable)?.DisposeAsync() ?? default;\n");
                    }
                    break;
                }
                case (0, true): {
                    if (singeltonDisposablesCount > 0) {
                        foreach (Service service in serviceList)
                            if (service.IsDisposable)
                                AppendDispose(service);
                        builder.Append('\n');
                    }

                    if (hasDisposeList)
                        AppendDisposingDisposeList();

                    builder.AppendIndent(indent, 2);
                    builder.Append($"Task[] disposeTasks = new Task[{ASYNC_DISPOSE_LIST}.Count];\n\n");

                    builder.AppendIndent(indent, 2);
                    builder.Append("int index = 0;\n");
                    AppendDisposingAsyncDisposeListArray();

                    builder.AppendIndent(indent, 2);
                    builder.Append("return new ValueTask(Task.WhenAll(disposeTasks));\n");
                    break;
                }
                case ( > 0, false): {
                    if (singeltonDisposablesCount > 0) {
                        foreach (Service service in serviceList)
                            if (service.IsDisposable && !service.IsAsyncDisposable)
                                AppendDispose(service);
                        builder.Append('\n');
                    }

                    if (hasDisposeList)
                        AppendDisposingDisposeList();

                    builder.AppendIndent(indent, 2);
                    builder.Append("Task[] disposeTasks = new Task[");
                    builder.Append(singeltonAsyncDisposablesCount);
                    builder.Append("];\n\n");

                    int index = 0;
                    foreach (Service service in serviceList)
                        if (service.IsAsyncDisposable)
                            AppendDisposeAsyncArray(service, index++);
                    builder.Append('\n');

                    builder.AppendIndent(indent, 2);
                    builder.Append("return new ValueTask(Task.WhenAll(disposeTasks));\n");
                    break;
                }
                case ( > 0, true): {
                    if (singeltonDisposablesCount > 0) {
                        foreach (Service service in serviceList)
                            if (service.IsDisposable && !service.IsAsyncDisposable)
                                AppendDispose(service);
                        builder.Append('\n');
                    }

                    if (hasDisposeList)
                        AppendDisposingDisposeList();

                    builder.AppendIndent(indent, 2);
                    builder.Append("Task[] disposeTasks = new Task[");
                    builder.Append(singeltonAsyncDisposablesCount);
                    builder.Append($" + {ASYNC_DISPOSE_LIST}.Count];\n\n");

                    int index = 0;
                    foreach (Service service in serviceList)
                        if (service.IsAsyncDisposable)
                            AppendDisposeAsyncArray(service, index++);
                    builder.Append('\n');

                    builder.AppendIndent(indent, 2);
                    builder.Append("int index = ");
                    builder.Append(singeltonAsyncDisposablesCount);
                    builder.Append(";\n");
                    AppendDisposingAsyncDisposeListArray();

                    builder.AppendIndent(indent, 2);
                    builder.Append("return new ValueTask(Task.WhenAll(disposeTasks));\n");
                    break;
                }
            }

            builder.AppendIndent(indent, 1);
            builder.Append("}\n\n");
        }

        builder.Append('\n');
    }


    private readonly void AppendDispose(Service service) {
        builder.AppendIndent(indent, 2);
        builder.Append('(');
        if (service.CreationTimeTransitive == CreationTiming.Constructor) {
            builder.Append("(IDisposable)");
            builder.AppendServiceField(service);
            builder.Append(')');
        }
        else {
            builder.AppendServiceField(service);
            builder.Append(" as IDisposable)?");
        }
        builder.Append(".Dispose();\n");
    }

    private readonly void AppendDisposeAsyncArray(Service service, int index) {
        builder.AppendIndent(indent, 2);
        builder.Append("disposeTasks[");
        builder.Append(index);
        builder.Append("] = (");
        if (service.CreationTimeTransitive == CreationTiming.Constructor) {
            builder.Append("(IAsyncDisposable)");
            builder.AppendServiceField(service);
            builder.Append(").DisposeAsync().AsTask();\n");
        }
        else {
            builder.AppendServiceField(service);
            builder.Append(" as IAsyncDisposable)?.DisposeAsync().AsTask() ?? Task.CompletedTask;\n");
        }
    }


    private readonly void AppendDisposingDisposeList() {
        int extraIndent = 0;
        if (threadSafe) {
            builder.AppendIndent(indent, 2);
            builder.Append($"lock ({DISPOSE_LIST})\n");
            extraIndent = 1;
        }

        builder.AppendIndent(indent, 2 + extraIndent);
        builder.Append($"foreach (IDisposable disposable in {DISPOSE_LIST})\n");
        builder.AppendIndent(indent, 3 + extraIndent);
        builder.Append("disposable.Dispose();\n\n");
    }

    private readonly void AppendDisposingAsyncDisposeListDiscard() {
        int extraIndent = 0;
        if (threadSafe) {
            builder.AppendIndent(indent, 2);
            builder.Append($"lock ({ASYNC_DISPOSE_LIST})\n");
            extraIndent = 1;
        }

        builder.AppendIndent(indent, 2 + extraIndent);
        builder.Append($"foreach (IAsyncDisposable asyncDisposable in {ASYNC_DISPOSE_LIST})\n");
        builder.AppendIndent(indent, 3 + extraIndent);
        builder.Append("if (asyncDisposable is IDisposable disposable)\n");
        builder.AppendIndent(indent, 4 + extraIndent);
        builder.Append("disposable.Dispose();\n");
        builder.AppendIndent(indent, 3 + extraIndent);
        builder.Append("else\n");
        builder.AppendIndent(indent, 4 + extraIndent);
        builder.Append("_ = asyncDisposable.DisposeAsync().Preserve();\n\n");
    }

    private readonly void AppendDisposingAsyncDisposeListArray() {
        int extraIndent = 0;
        if (threadSafe) {
            builder.AppendIndent(indent, 2);
            builder.Append($"lock ({ASYNC_DISPOSE_LIST})\n");
            extraIndent = 1;
        }

        builder.AppendIndent(indent, 2 + extraIndent);
        builder.Append($"foreach (IAsyncDisposable asyncDisposable in {ASYNC_DISPOSE_LIST})\n");
        builder.AppendIndent(indent, 3 + extraIndent);
        builder.Append("disposeTasks[index++] = asyncDisposable.DisposeAsync().AsTask();\n\n");
    }

    #endregion


    #region ServiceTreeBuilder

    private readonly Stack<Service> circularServiceList = [];
    private readonly List<(Service service, string parentName)> transientDisposeList = [];
    private readonly List<(Service service, string parentName)> transientAsyncDisposeList = [];

    private struct ServiceTreeBuilder(CircleDIBuilderCore core, int indentation) {
        public int indentation = indentation;

        /// <summary>
        /// filtering in AppendServiceTreeRecursion
        /// </summary>
        private CreationTiming creationTiming;
        /// <summary>
        /// First node of entire subtree.
        /// </summary>
        private Service? rootNode;
        /// <summary>
        /// First node of one circular tree search.
        /// </summary>
        private Service? firstNode;


        /// <summary>
        /// Special case variant of <see cref="AppendServiceTree(Service)"/> for CreateScope()
        /// </summary>
        public void AppendCreateScopeServiceTree() {
            Service service = core.serviceProvider.CreateScope!;
            rootNode = service;

            firstNode = service;
            foreach (Dependency dependency in service.Dependencies)
                AppendServiceTreeRecursion(dependency.Service!, service.Name);

            while (core.circularServiceList.Count > 0) {
                Service circularService = core.circularServiceList.Pop();

                firstNode = circularService;
                AppendServiceTreeRecursion(circularService, string.Empty);
            }
        }

        /// <summary>
        /// Appends a construction chain of the subtree from the given node (node itself inclusive).<br />
        /// The chain starts with the leave nodes.
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parentName"></param>
        public void AppendServiceTree(Service service, CreationTiming creationTiming) {
            this.creationTiming = creationTiming;
            rootNode = service;

            firstNode = service;
            AppendServiceTreeRecursion(service, string.Empty);

            while (core.circularServiceList.Count > 0) {
                Service circularService = core.circularServiceList.Pop();

                firstNode = circularService;
                AppendServiceTreeRecursion(circularService, string.Empty);
            }
        }

        private readonly void AppendServiceTreeRecursion(Service service, string parentName) {
            if (service.TreeState.HasFlag(core.serviceProvider.DependencyTreeFlag))
                return;
            if (!service.Lifetime.HasFlag(ServiceLifetime.Transient)) {
                service.TreeState |= core.serviceProvider.DependencyTreeFlag;

                if (service.CreationTimeTransitive != creationTiming)
                    return;
            }

            if (service.Lifetime.HasFlag(ServiceLifetime.Delegate))
                return;

            if (service.Implementation.Type is MemberType.Field)
                return;


            foreach (ConstructorDependency dependency in service.ConstructorDependencyList)
                AppendServiceTreeRecursion(dependency.Service!, service.Name);
            foreach (PropertyDependency dependency in service.PropertyDependencyList)
                if (!dependency.IsCircular)
                    AppendServiceTreeRecursion(dependency.Service!, service.Name);
                else
                    core.circularServiceList.Push(dependency.Service!);


            if (service.Lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped) {
                core.builder.AppendIndent(core.indent, indentation);
                if (core.isScopeProvider && service.Lifetime is ServiceLifetime.Singleton)
                    core.AppendServiceProviderField();
                core.builder.AppendServiceField(service);
                core.builder.Append(' ');
                if (service.CreationTimeTransitive is CreationTiming.Lazy && !ReferenceEquals(service, rootNode))
                    core.builder.Append("??");
                core.builder.Append("= ");
                if (service.Implementation.Type == MemberType.None) {
                    core.builder.Append("new global::");
                    core.builder.AppendClosedFullyQualified(service.ImplementationType);
                    core.AppendConstructorDependencyList(service);
                    core.AppendPropertyDependencyList(service, indentation);
                }
                else {
                    if (core.isScopeProvider && !service.Implementation.IsScoped && !service.Implementation.IsStatic)
                        core.AppendServiceProviderField();
                    core.builder.AppendImplementationName(service);
                    if (service.Implementation.Type == MemberType.Method)
                        core.AppendConstructorDependencyList(service);
                }
                core.builder.Append(";\n");
            }
            else { // ServiceLifetime.Transient
                core.builder.AppendIndent(core.indent, indentation);
                core.builder.Append("global::");
                core.builder.AppendClosedFullyQualified(service.ImplementationType);
                core.builder.Append(' ');
                core.builder.AppendFirstLower(service.Name);
                if (parentName != string.Empty && !ReferenceEquals(service, firstNode)) {
                    core.builder.Append('_');
                    core.builder.Append(parentName);
                }
                core.builder.Append(" = ");

                if (service.Implementation.Type == MemberType.None) {
                    core.builder.Append("new global::");
                    core.builder.AppendClosedFullyQualified(service.ImplementationType);
                    core.AppendConstructorDependencyList(service);
                    core.AppendPropertyDependencyList(service, indentation);
                }
                else {
                    if (core.isScopeProvider && !service.Implementation.IsScoped && !service.Implementation.IsStatic)
                        core.AppendServiceProviderField();
                    core.builder.AppendImplementationName(service);
                    if (service.Implementation.Type == MemberType.Method)
                        core.AppendConstructorDependencyList(service);
                }
                core.builder.Append(";\n");

                if (core.generateDisposeMethods is not DisposeGeneration.NoDisposing)
                    if (service.IsAsyncDisposable)
                        core.transientAsyncDisposeList.Add((service, parentName));
                    else if (service.IsDisposable)
                        core.transientDisposeList.Add((service, parentName));
            }
        }


        /// <summary>
        /// Special case variant of <see cref="AppendCircularDependencies(Service)"/> for CreateScope()
        /// </summary>
        public readonly void AppendCreateScopeCircularDependencies() {
            Service service = core.serviceProvider.CreateScope!;
            foreach (Dependency dependency in service.Dependencies)
                AppendCircularDependenciesRecursion(dependency.Service!, service.Name);
        }
        
        /// <summary>
        /// Appends for each circular dependency an unsafe accessor call in the given subtree (node itself inclusive).
        /// </summary>
        /// <param name="service"></param>
        /// <param name="parentName"></param>
        public readonly void AppendCircularDependencies(Service service) => AppendCircularDependenciesRecursion(service, string.Empty);

        private readonly void AppendCircularDependenciesRecursion(Service service, string parentName) {
            if (service.TreeState.HasFlag(core.serviceProvider.DependencyTreeFlag))
                return;
            service.TreeState |= core.serviceProvider.DependencyTreeFlag;

            foreach (ConstructorDependency dependency in service.ConstructorDependencyList)
                AppendCircularDependenciesRecursion(dependency.Service!, service.Name);
            foreach (PropertyDependency dependency in service.PropertyDependencyList) {
                AppendCircularDependenciesRecursion(dependency.Service!, service.Name);

                if (!dependency.IsCircular)
                    continue;

                core.builder.AppendIndent(core.indent, indentation);
                if (dependency.IsInit) {
                    core.builder.Append("Set_");
                    core.builder.Append(service.Name);
                    core.builder.Append('_');
                    core.builder.Append(dependency.Name);
                    core.builder.Append('(');

                    switch (service.Lifetime) {
                        case ServiceLifetime.Singleton:
                            if (core.isScopeProvider)
                                core.AppendServiceProviderField();
                            goto case ServiceLifetime.Scoped;
                        case ServiceLifetime.Scoped:
                            core.builder.AppendServiceField(service);
                            break;
                        default:
                            core.builder.AppendFirstLower(service.Name);
                            if (parentName != string.Empty) {
                                core.builder.Append('_');
                                core.builder.Append(parentName);
                            }
                            break;
                    }
                    core.builder.Append(", ");
                    core.AppendDependencyIdentifier(service, dependency);
                    core.builder.Append(')');
                }
                else {
                    switch (service.Lifetime) {
                        case ServiceLifetime.Singleton:
                            if (core.isScopeProvider)
                                core.AppendServiceProviderField();
                            goto case ServiceLifetime.Scoped;
                        case ServiceLifetime.Scoped:
                            core.builder.AppendServiceField(service);
                            break;
                        default:
                            core.builder.AppendFirstLower(service.Name);
                            if (parentName != string.Empty) {
                                core.builder.Append('_');
                                core.builder.Append(parentName);
                            }
                            break;
                    }
                    core.builder.Append('.');
                    core.builder.Append(dependency.Name);
                    core.builder.Append(" = ");
                    core.AppendDependencyIdentifier(service, dependency);
                }
                core.builder.Append(";\n");
            }
        }


        /// <summary>
        /// <para>Appends for each disposable transient service an add statement to the corresponding disposeList.</para>
        /// <para>DisposeList is appended first, then asyncDisposeList, but if a service implements both, it is added to asyncDisposeList.</para>
        /// </summary>
        public readonly void AppendDisposeLists() {
            if (core.transientDisposeList.Count > 0)
                AppendDisposeList(core.transientDisposeList, DISPOSE_LIST);
            if (core.transientAsyncDisposeList.Count > 0)
                AppendDisposeList(core.transientAsyncDisposeList, ASYNC_DISPOSE_LIST);
        }

        private readonly void AppendDisposeList(List<(Service service, string parentName)> disposeList, string disposeListName) {
            if (core.threadSafe) {
                core.builder.AppendIndent(core.indent, indentation);
                core.builder.Append("lock (");
                core.builder.Append(disposeListName);
                core.builder.Append(") {\n");

                foreach ((Service service, string parentName) in disposeList) {
                    core.builder.AppendIndent(core.indent, indentation + 1);
                    core.builder.Append(disposeListName);
                    core.builder.Append(".Add(");
                    core.builder.AppendFirstLower(service.Name);
                    if (parentName != string.Empty && !ReferenceEquals(service, firstNode)) {
                        core.builder.Append('_');
                        core.builder.Append(parentName);
                    }
                    core.builder.Append(");\n");
                }

                core.builder.AppendIndent(core.indent, indentation);
                core.builder.Append("}\n");
            }
            else {
                foreach ((Service service, string parentName) in disposeList) {
                    core.builder.AppendIndent(core.indent, indentation);
                    core.builder.Append(disposeListName);
                    core.builder.Append(".Add(");
                    core.builder.AppendFirstLower(service.Name);
                    if (parentName != string.Empty && !ReferenceEquals(service, firstNode)) {
                        core.builder.Append('_');
                        core.builder.Append(parentName);
                    }
                    core.builder.Append(");\n");
                }
            }

            disposeList.Clear();
        }


        /// <summary>
        /// <para>Initializes the disposeList/asyncDisposeList with a collection expression containing all transients created in the constructor.</para>
        /// <para>DisposeList is created first, then asyncDisposeList, but if a service implements both, it is put in asyncDisposeList.</para>
        /// </summary>
        public readonly void AppendDiposeListsConstructor() {
            if (core.hasDisposeList)
                AppendDiposeListConstructor(core.transientDisposeList, DISPOSE_LIST);
            if (core.hasAsyncDisposeList)
                AppendDiposeListConstructor(core.transientAsyncDisposeList, ASYNC_DISPOSE_LIST);
        }

        private readonly void AppendDiposeListConstructor(List<(Service service, string parentName)> disposeList, string disposeListName) {
            core.builder.AppendIndent(core.indent, 2);
            core.builder.Append(disposeListName);
            core.builder.Append(" = [");
            switch (disposeList.Count) {
                case 0:
                    break;
                case 1:
                    core.builder.AppendFirstLower(disposeList[0].service.Name);
                    if (disposeList[0].parentName != string.Empty && !ReferenceEquals(disposeList[0].service, firstNode)) {
                        core.builder.Append('_');
                        core.builder.Append(disposeList[0].parentName);
                    }
                    break;
                default:
                    foreach ((Service service, string parentName) in disposeList) {
                        core.builder.Append('\n');
                        core.builder.AppendIndent(core.indent, 3);
                        core.builder.AppendFirstLower(service.Name);
                        if (parentName != string.Empty && !ReferenceEquals(service, firstNode)) {
                            core.builder.Append('_');
                            core.builder.Append(parentName);
                        }
                        core.builder.Append(",");
                    }
                    core.builder.Length--;
                    core.builder.Append('\n');
                    core.builder.AppendIndent(core.indent, 2);
                    break;
            }

            core.builder.Append("];\n");

            disposeList.Clear();
        }
    }

    #endregion


    #region Shared

    /// <summary>
    /// Appends serviceProvider field with casting to implementation type with trailing '.':<br />
    /// "_{providerName}."
    /// </summary>
    public readonly void AppendServiceProviderField() {
        builder.Append('_');
        builder.AppendFirstLower(serviceProvider.Identifier.Name);
        builder.Append('.');
    }

    /// <summary>
    /// if Singleton or Scoped -> ServiceField<br />
    /// if Transient -> localVariable<br />
    /// if Delegate -> ServiceGetter
    /// </summary>
    /// <param name="service"></param>
    /// <param name="dependency"></param>
    public readonly void AppendDependencyIdentifier(Service service, Dependency dependency) {
        if (dependency.Service!.Lifetime is ServiceLifetime.Singleton) {
            if (isScopeProvider) {
                if (dependency.Service.Implementation.Name == "this") {
                    builder.Append('_');
                    builder.AppendFirstLower(serviceProvider.Identifier.Name);
                }
                else {
                    AppendServiceProviderField();
                    builder.AppendServiceField(dependency.Service);
                }
            }
            else
                builder.AppendServiceField(dependency.Service);
        }
        else if (dependency.Service!.Lifetime is ServiceLifetime.Scoped)
            builder.AppendServiceField(dependency.Service);
        else if (dependency.Service.Lifetime.HasFlag(ServiceLifetime.Transient)) {
            builder.AppendFirstLower(dependency.Service.Name);
            builder.Append('_');
            builder.Append(service.Name);
        }
        else // if (dependency.Service!.Lifetime.HasFlag(ServiceLifetime.Delegate))
            builder.AppendServiceGetter(dependency.Service!);
    }


    /// <summary>
    /// <para>Appends "(service1Type service1Name, service2Type service2Name, ..., serviceNType serviceNName)"</para>
    /// <para>If dependencyList is empty, only "()" is appended.</para>
    /// </summary>
    /// <param name="dependencyList"></param>
    public readonly void AppendParameterDependencyList(IEnumerable<Dependency> dependencyList) {
        builder.Append('(');

        foreach (Dependency dependency in dependencyList) {
            builder.Append("global::");
            builder.AppendClosedFullyQualified(dependency.ServiceType ?? dependency.Service!.ServiceType);
            builder.Append(' ');
            builder.AppendFirstLower(dependency.Name);
            builder.Append(", ");
        }
        if (builder[^1] == ' ')
            builder.Length -= 2;

        builder.Append(')');
    }

    /// <summary>
    /// <para>Appends "(service1, service2, ..., serviceN)"</para>
    /// <para>If <see cref="Service.ConstructorDependencyList"/> is empty, only "()" is appended.</para>
    /// </summary>
    /// <param name="service"></param>
    public readonly void AppendConstructorDependencyList(Service service) {
        builder.Append('(');
        if (service.ConstructorDependencyList.Count > 0) {
            foreach (ConstructorDependency dependency in service.ConstructorDependencyList) {
                if (dependency.Service!.IsRefable && !keyword.HasFlag(TypeKeyword.Struct))
                    builder.Append(dependency.ByRef.AsString());
                AppendDependencyIdentifier(service, dependency);
                builder.Append(", ");
            }
            builder.Length -= 2;
        }
        builder.Append(')');
    }

    /// <summary>
    /// <para>Appends " { name1 = service1, name2 = service2, ..., nameN = serviceN }"</para>
    /// <para>If <see cref="Service.PropertyDependencyList"/> is empty, nothing is appended.</para>
    /// </summary>
    /// <param name="service"></param>
    /// <param name="indentation"></param>
    public readonly void AppendPropertyDependencyList(Service service, int indentation) {
        if (service.PropertyDependencyList.Count > 0) {
            builder.Append(" {");

            int builderLength = builder.Length;
            foreach (PropertyDependency dependency in service.PropertyDependencyList) {
                if (dependency.IsCircular) {
                    if (dependency.IsRequired) {
                        builder.Append('\n');
                        builder.AppendIndent(indent, indentation + 1);
                        builder.Append(dependency.Name);
                        builder.Append(" = default!,");
                    }
                }
                else {
                    builder.Append('\n');
                    builder.AppendIndent(indent, indentation + 1);
                    builder.Append(dependency.Name);
                    builder.Append(" = ");
                    AppendDependencyIdentifier(service, dependency);
                    builder.Append(',');
                }
            }

            if (builderLength != builder.Length) {
                // at least one item got appended
                builder[^1] = '\n'; // remove last ','
                builder.AppendIndent(indent, indentation);
                builder.Append('}');
            }
            else
                // nothing got appended
                builder.Length -= 2; // remove " {"
        }
    }


    /// <summary>
    /// <para>Appends ServiceProvider summary:</para>
    /// <para>
    /// &lt;summary&gt;<br />
    /// Number of services registered: {}<br />
    /// - Singleton: {}<br />
    /// - Scoped: {}<br />
    /// - Transient: {}<br />
    /// - Delegate: {}
    /// </para>
    /// <para>
    /// This provider [can create a scope | has no scope],<br />
    /// implements [no Dispose methods | only synchronous Dispose() method | only asynchronous DisposeAsync() method | both Dispose() and DisposeAsync() methods]<br />
    /// and is {not} thread safe.<br />
    /// &lt;/summary&gt;
    /// </para>
    /// </summary>
    public readonly void AppendClassSummary() {
        builder.AppendIndent(indent);
        builder.Append("/// <summary>\n");


        builder.AppendIndent(indent);
        builder.Append("/// <para>\n");

        builder.AppendIndent(indent);
        builder.Append("/// Number of services registered: ");
        builder.Append(serviceProvider.SortedServiceList.Count);
        builder.Append("<br />\n");

        builder.AppendIndent(indent);
        builder.Append("/// - Singleton: ");
        builder.Append(serviceProvider.SingletonList.Count);
        builder.Append("<br />\n");

        builder.AppendIndent(indent);
        builder.Append("/// - Scoped: ");
        builder.Append(serviceProvider.ScopedList.Count);
        builder.Append("<br />\n");

        builder.AppendIndent(indent);
        builder.Append("/// - Transient: ");
        builder.Append(serviceProvider.TransientList.Count);
        builder.Append("<br />\n");

        builder.AppendIndent(indent);
        builder.Append("/// - Delegate: ");
        builder.Append(serviceProvider.DelegateList.Count);
        builder.Append('\n');

        builder.AppendIndent(indent);
        builder.Append("/// </para>\n");


        builder.AppendIndent(indent);
        builder.Append("/// <para>\n");

        builder.AppendIndent(indent);
        builder.Append(serviceProvider.GenerateScope switch {
            true => "/// This provider can create a scope,",
            false => "/// This provider has no scope,"
        });
        builder.Append("<br />\n");

        builder.AppendIndent(indent);
        builder.Append(generateDisposeMethods switch {
            DisposeGeneration.NoDisposing => "/// implements no Dispose methods",
            DisposeGeneration.Dispose => "/// implements only synchronous Dispose() method",
            DisposeGeneration.DisposeAsync => "/// implements only asynchronous DisposeAsync() method",
            DisposeGeneration.GenerateBoth => "/// implements both Dispose() and DisposeAsync() methods",
            _ => throw new Exception($"Invalid enum DisposeGeneration: {generateDisposeMethods}")
        });
        builder.Append("<br />\n");

        builder.AppendIndent(indent);
        builder.Append(threadSafe switch {
            true => "/// and is thread safe.",
            false => "/// and is not thread safe."
        });
        builder.Append('\n');

        builder.AppendIndent(indent);
        builder.Append("/// </para>\n");


        builder.AppendIndent(indent);
        builder.Append("/// </summary>\n");
    }

    /// <summary>
    /// <para>Appends service information:</para>
    /// <para>
    /// &lt;summary&gt;<br />
    /// Lifetime: {lifetime}<br />
    /// Service type: {serviceType}<br />
    /// Implementation type: {implementationType}<br />
    /// &lt;/summary&gt;
    /// </para>
    /// </summary>
    /// <param name="service"></param>
    public readonly void AppendServiceSummary(Service service) {
        builder.AppendIndent(indent, 1);
        builder.Append("/// <summary>\n");

        builder.AppendIndent(indent, 1);
        builder.Append("/// Lifetime: <see cref=\"global::CircleDIAttributes.");
        builder.Append(service.Lifetime.AsString());
        builder.Append("Attribute{TService}\">");
        builder.Append(service.Lifetime.AsString());
        builder.Append("</see><br />\n");

        builder.AppendIndent(indent, 1);
        builder.Append("/// Service type: <see cref=\"global::");
        {
            int startIndex = builder.Length;
            builder.AppendClosedFullyQualified(service.ServiceType);
            builder.Replace('<', '{', startIndex, builder.Length - startIndex);
            builder.Replace('>', '}', startIndex, builder.Length - startIndex);
        }
        builder.Append("\"/><br />\n");

        builder.AppendIndent(indent, 1);
        builder.Append("/// Implementation type: <see cref=\"global::");
        {
            int startIndex = builder.Length;
            builder.AppendClosedFullyQualified(service.ImplementationType);
            builder.Replace('<', '{', startIndex, builder.Length - startIndex);
            builder.Replace('>', '}', startIndex, builder.Length - startIndex);
        }
        builder.Append("\"/>\n");

        builder.AppendIndent(indent, 1);
        builder.Append("/// </summary>\n");
    }

    /// <summary>
    /// <para>Summary for CreateScope()-method and Scope-constructor:</para>
    /// <para>
    /// &lt;summary&gt;<br />
    /// Creates an instance of {classname}.Scope together with all non-lazy scoped services.<br />
    /// &lt;/summary&gt;
    /// </para>
    /// </summary>
    public readonly void AppendCreateScopeSummary() {
        builder.AppendIndent(indent, 1);
        builder.Append("/// <summary>\n");

        builder.AppendIndent(indent, 1);
        builder.Append("/// Creates an instance of a ScopeProvider together with all <see cref=\"global::CircleDIAttributes.CreationTiming.Constructor\">non-lazy</see> scoped services.\n");

        builder.AppendIndent(indent, 1);
        builder.Append("/// </summary>\n");
    }

    #endregion
}
