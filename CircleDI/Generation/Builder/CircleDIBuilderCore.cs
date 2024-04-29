#pragma warning disable IDE0251 // Member can be made 'readonly'

using CircleDI.Defenitions;
using CircleDI.Extensions;
using System.Text;

namespace CircleDI.Generation;

/// <summary>
/// Contains the build functionalities to build class and interface that have ServiceProvider and ScopeProvider in common.
/// </summary>
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

    private (List<(Service service, int number)> syncList, List<(Service service, int number)> asyncList) disposeLists;
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

        disposeLists = (transientDisposeList, transientAsyncDisposeList);

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

        serviceList = serviceProvider.ScopedList;
        constructorParameterList = serviceProvider.ConstructorParameterListScope;
        keyword = serviceProvider.KeywordScope;
        generateDisposeMethods = serviceProvider.GenerateDisposeMethodsScope;
        hasConstructor = serviceProvider.HasConstructorScope;
        hasDisposeMethod = serviceProvider.HasDisposeMethodScope;
        hasDisposeAsyncMethod = serviceProvider.HasDisposeAsyncMethodScope;
        threadSafe = serviceProvider.ThreadSafeScope;

        disposeLists = (scopeTransientDisposeList, scopeTransientAsyncDisposeList);

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
        AppendParameterDependencyList(constructorParameterList);
        builder.Append(" {\n");

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

    #endregion


    #region CreateScope()

    /// <summary>
    /// "special" method CreateScope()
    /// </summary>
    public void AppendCreateScope() {
        if (serviceProvider.GenerateScope) {
            AppendCreateScopeSummary();

            builder.AppendIndent(indent);
            builder.Append("public global::");
            if (serviceProvider.HasInterface)
                builder.AppendOpenFullyQualified(serviceProvider.InterfaceIdentifierScope);
            else
                builder.AppendOpenFullyQualified(serviceProvider.IdentifierScope);
            builder.Append(" CreateScope");
            builder.AppendOpenGenerics(serviceProvider.IdentifierScope);
            AppendParameterDependencyList(serviceProvider.CreateScope.ConstructorDependencyList.Concat<Dependency>(serviceProvider.CreateScope.PropertyDependencyList).Where((Dependency dependency) => !dependency.HasAttribute));
            builder.Append(" {\n");
            indent.IncreaseLevel(); // 2

            AppendCreateScopeServiceTree();

            indent.DecreaseLevel(); // 1
            builder.AppendIndent(indent);
            builder.Append("}\n\n\n");
        }
    }

    #endregion


    #region Singleton/Scoped Fields and Getter

    /// <summary>
    /// Singletons/Scoped service Getter
    /// </summary>
    public void AppendServicesGetter() {
        if (serviceList.Count > 0) {
            foreach (Service service in serviceList) {
                string refOrEmpty = (service.IsRefable && !keyword.HasFlag(TypeKeyword.Struct)) switch {
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

                if (service.Implementation.Type == MemberType.Field) {
                    builder.Append(" => ");
                    builder.Append(refOrEmpty);
                    if (service.Lifetime == ServiceLifetime.Scoped && !service.Implementation.IsScoped && !service.Implementation.IsStatic)
                        AppendServiceProviderField();
                    builder.AppendImplementationName(service);
                    builder.Append(";\n");
                }
                else if (service.CreationTimeTransitive == CreationTiming.Constructor) {
                    builder.Append(" => ");
                    builder.Append(refOrEmpty);
                    builder.Append('_');
                    builder.AppendFirstLower(service.Name);
                    builder.Append(";\n");

                    builder.AppendIndent(indent);
                    builder.Append("private ");
                    builder.Append(readonlyStr);
                    builder.Append("global::");
                    builder.AppendClosedFullyQualified(service.ImplementationType);
                    builder.Append(" _");
                    builder.AppendFirstLower(service.Name);
                    builder.Append(";\n");
                }
                else {
                    builder.Append(" {\n");
                    indent.IncreaseLevel(); // 2

                    if (service.GetAccessor == GetAccess.Property) {
                        builder.AppendIndent(indent);
                        builder.Append("get {\n");
                        indent.IncreaseLevel(); // 3
                    }

                    AppendLazyService(service);

                    builder.AppendIndent(indent);
                    builder.Append("return ");
                    builder.Append(refOrEmpty);
                    builder.Append("(global::");
                    builder.AppendClosedFullyQualified(service.ServiceType);
                    builder.Append(")_");
                    builder.AppendFirstLower(service.Name);
                    builder.Append(";\n");

                    if (service.GetAccessor == GetAccess.Property) {
                        indent.DecreaseLevel(); // 2
                        builder.AppendIndent(indent);
                        builder.Append("}\n");
                    }

                    indent.DecreaseLevel(); // 1
                    builder.AppendIndent(indent);
                    builder.Append("}\n");

                    builder.AppendIndent(indent);
                    builder.Append("private ");
                    builder.Append("global::");
                    builder.AppendClosedFullyQualified(service.ImplementationType);
                    builder.Append("? _");
                    builder.AppendFirstLower(service.Name);
                    builder.Append(";\n");
                }

                builder.Append('\n');
            }

            builder.Append('\n');
        }
    }

    #endregion


    #region Singleton Exposing

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
            AppendServiceProviderField();
            builder.AppendServiceGetter(service);
            builder.Append(";\n\n");
        }
        builder.Append('\n');
    }

    #endregion


    #region Transient Getter

    /// <summary>
    /// Transients service Getter
    /// </summary>
    public void AppendServicesTransient() {
        int currentPosition = builder.Length;

        foreach (Service service in serviceProvider.TransientList) {
            if (!isScopeProvider && service.Lifetime is ServiceLifetime.TransientScoped)
                continue;

            AppendServiceSummary(service);
            builder.AppendIndent(indent);
            builder.Append("public global::");
            builder.AppendClosedFullyQualified(service.ServiceType);
            builder.Append(' ');

            if (service.GetAccessor == GetAccess.Property) {
                builder.Append(service.Name);
                builder.Append(" {\n");
                indent.IncreaseLevel(); // 2

                builder.AppendIndent(indent);
                builder.Append("get {\n");
                indent.IncreaseLevel(); // 3

                int transientNumber = AppendTransientService(service);
                
                builder.AppendIndent(indent);
                builder.Append("return ");
                builder.AppendFirstLower(service.Name);
                if (transientNumber > 0) {
                    builder.Append('_');
                    builder.Append(transientNumber);
                }
                builder.Append(";\n");


                indent.DecreaseLevel(); // 2
                builder.AppendIndent(indent);
                builder.Append("}\n");

                indent.DecreaseLevel(); // 1
                builder.AppendIndent(indent);
                builder.Append("}\n");
            }
            else {
                builder.Append("Get");
                builder.Append(service.Name);
                builder.Append("() {\n");
                indent.IncreaseLevel(); // 2

                int transientNumber = AppendTransientService(service);
                
                builder.AppendIndent(indent);
                builder.Append("return ");
                builder.AppendFirstLower(service.Name);
                if (transientNumber > 0) {
                    builder.Append('_');
                    builder.Append(transientNumber);
                }
                builder.Append(";\n");


                indent.DecreaseLevel(); // 1
                builder.AppendIndent(indent);
                builder.Append("}\n");
            }

            builder.Append('\n');
        }

        if (builder.Length > currentPosition)
            builder.Append('\n');
    }

    #endregion


    #region Delegate Getter

    /// <summary>
    /// Delegates service Getter
    /// </summary>
    public void AppendServicesDelegate() {
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
    public void AppendIServiceProviderNotScoped()
        => AppendIServiceProvider(new ServiceListIterator(serviceProvider).GetNextNotScoped);

    /// <summary>
    /// <see cref="IServiceProvider.GetService(Type)"/> switch content with all services except ServiceProvider only services.
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

    #endregion


    #region Dispose

    private const string DISPOSE_LIST = "_disposeList";
    private const string ASYNC_DISPOSE_LIST = "_asyncDisposeList";

    /// <summary>
    /// Generates the Dispose/AsyncDispose methods and the corresponding disposeLists for the transient services.
    /// </summary>
    public void AppendDisposeMethods() {
        if (generateDisposeMethods == DisposeGeneration.NoDisposing)
            return;


        // disposeList
        if (hasDisposeList) {
            builder.AppendIndent(indent);
            builder.Append("private ");
            builder.Append(readonlyStr);
            builder.Append($"global::System.Collections.Generic.List<IDisposable> {DISPOSE_LIST};\n\n");
        }

        // asyncDisposeList
        if (hasAsyncDisposeList) {
            builder.AppendIndent(indent);
            builder.Append("private ");
            builder.Append(readonlyStr);
            builder.Append($"global::System.Collections.Generic.List<IAsyncDisposable> {ASYNC_DISPOSE_LIST};\n\n");
        }


        uint singeltonDisposablesCount = 0;
        uint singeltonAsyncDisposablesCount = 0;

        // Dispose()
        if (generateDisposeMethods.HasFlag(DisposeGeneration.Dispose)) {
            if (!hasDisposeMethod) {
                builder.AppendIndent(indent);
                builder.Append("/// <summary>\n");
                builder.AppendIndent(indent);
                builder.Append("/// Disposes all disposable services instantiated by this provider.\n");
                builder.AppendIndent(indent);
                builder.Append("/// </summary>\n");
                builder.AppendIndent(indent);
                builder.Append("public void Dispose() {\n");
            }
            else {
                builder.AppendIndent(indent);
                builder.Append("/// <summary>\n");
                builder.AppendIndent(indent);
                builder.Append("/// Disposes all disposable services instantiated by this provider. Should be called inside the Dispose() method.\n");
                builder.AppendIndent(indent);
                builder.Append("/// </summary>\n");
                builder.AppendIndent(indent);
                builder.Append("private void DisposeServices() {\n");
            }
            indent.IncreaseLevel(); // 2


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
                    builder.AppendIndent(indent);
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

            indent.DecreaseLevel(); // 1
            builder.AppendIndent(indent);
            builder.Append("}\n\n");
        }

        // DisposeAsync()
        if (generateDisposeMethods.HasFlag(DisposeGeneration.DisposeAsync)) {
            if (!hasDisposeAsyncMethod) {
                builder.AppendIndent(indent);
                builder.Append("/// <summary>\n");
                builder.AppendIndent(indent);
                builder.Append("/// Disposes all disposable services instantiated by this provider asynchronously.\n");
                builder.AppendIndent(indent);
                builder.Append("/// </summary>\n");
                builder.AppendIndent(indent);
                builder.Append("public ValueTask DisposeAsync() {\n");
            }
            else {
                builder.AppendIndent(indent);
                builder.Append("/// <summary>\n");
                builder.AppendIndent(indent);
                builder.Append("/// Disposes all disposable services instantiated by this provider asynchronously. Should be called inside the DisposeAsync() method.\n");
                builder.AppendIndent(indent);
                builder.Append("/// </summary>\n");
                builder.AppendIndent(indent);
                builder.Append("private ValueTask DisposeServicesAsync() {\n");
            }
            indent.IncreaseLevel(); // 2


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

                    builder.AppendIndent(indent);
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

                    builder.AppendIndent(indent);
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

                    builder.AppendIndent(indent);
                    builder.Append($"Task[] disposeTasks = new Task[{ASYNC_DISPOSE_LIST}.Count];\n\n");

                    builder.AppendIndent(indent);
                    builder.Append("int index = 0;\n");
                    AppendDisposingAsyncDisposeListArray();

                    builder.AppendIndent(indent);
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

                    builder.AppendIndent(indent);
                    builder.Append("Task[] disposeTasks = new Task[");
                    builder.Append(singeltonAsyncDisposablesCount);
                    builder.Append("];\n\n");

                    int index = 0;
                    foreach (Service service in serviceList)
                        if (service.IsAsyncDisposable)
                            AppendDisposeAsyncArray(service, index++);
                    builder.Append('\n');

                    builder.AppendIndent(indent);
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

                    builder.AppendIndent(indent);
                    builder.Append("Task[] disposeTasks = new Task[");
                    builder.Append(singeltonAsyncDisposablesCount);
                    builder.Append($" + {ASYNC_DISPOSE_LIST}.Count];\n\n");

                    int index = 0;
                    foreach (Service service in serviceList)
                        if (service.IsAsyncDisposable)
                            AppendDisposeAsyncArray(service, index++);
                    builder.Append('\n');

                    builder.AppendIndent(indent);
                    builder.Append("int index = ");
                    builder.Append(singeltonAsyncDisposablesCount);
                    builder.Append(";\n");
                    AppendDisposingAsyncDisposeListArray();

                    builder.AppendIndent(indent);
                    builder.Append("return new ValueTask(Task.WhenAll(disposeTasks));\n");
                    break;
                }
            }

            indent.DecreaseLevel(); // 1
            builder.AppendIndent(indent);
            builder.Append("}\n\n");
        }

        builder.Append('\n');
    }


    private void AppendDispose(Service service) {
        builder.AppendIndent(indent);
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

    private void AppendDisposeAsyncArray(Service service, int index) {
        builder.AppendIndent(indent);
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


    private void AppendDisposingDisposeList() {
        if (threadSafe) {
            builder.AppendIndent(indent);
            builder.Append($"lock ({DISPOSE_LIST})\n");
            indent.IncreaseLevel(); // 3
        }

        builder.AppendIndent(indent);
        builder.Append($"foreach (IDisposable disposable in {DISPOSE_LIST})\n");
        indent.IncreaseLevel(); // 3 or 4

        builder.AppendIndent(indent);
        builder.Append("disposable.Dispose();\n\n");
        indent.DecreaseLevel(); // 2 or 3

        if (threadSafe)
            indent.DecreaseLevel(); // 2
    }

    private void AppendDisposingAsyncDisposeListDiscard() {
        if (threadSafe) {
            builder.AppendIndent(indent);
            builder.Append($"lock ({ASYNC_DISPOSE_LIST})\n");
            indent.IncreaseLevel(); // 3
        }

        builder.AppendIndent(indent);
        builder.Append($"foreach (IAsyncDisposable asyncDisposable in {ASYNC_DISPOSE_LIST})\n");
        indent.IncreaseLevel(); // 3 or 4

        builder.AppendIndent(indent);
        builder.Append("if (asyncDisposable is IDisposable disposable)\n");
        indent.IncreaseLevel(); // 4 or 5

        builder.AppendIndent(indent);
        builder.Append("disposable.Dispose();\n");
        indent.DecreaseLevel(); // 3 or 4

        builder.AppendIndent(indent);
        builder.Append("else\n");
        indent.IncreaseLevel(); // 4 or 5

        builder.AppendIndent(indent);
        builder.Append("_ = asyncDisposable.DisposeAsync().Preserve();\n\n");
        indent.DecreaseLevel(); // 3 or 4
        indent.DecreaseLevel(); // 2 or 3

        if (threadSafe)
            indent.DecreaseLevel(); // 2
    }

    private void AppendDisposingAsyncDisposeListArray() {
        if (threadSafe) {
            builder.AppendIndent(indent);
            builder.Append($"lock ({ASYNC_DISPOSE_LIST})\n");
            indent.IncreaseLevel(); // 3
        }

        builder.AppendIndent(indent);
        builder.Append($"foreach (IAsyncDisposable asyncDisposable in {ASYNC_DISPOSE_LIST})\n");
        indent.IncreaseLevel(); // 3 or 4

        builder.AppendIndent(indent);
        builder.Append("disposeTasks[index++] = asyncDisposable.DisposeAsync().AsTask();\n\n");
        indent.DecreaseLevel(); // 2 or 3

        if (threadSafe)
            indent.DecreaseLevel(); // 2
    }

    #endregion


    #region UnsafeAccessor methods

    /// <summary>
    /// Generates the UnsafeAccessor methods for setting init-only properties.
    /// </summary>
    public void AppendUnsafeAccessorMethods() {
        builder.Append("\n\n\n");
        int currentPosition = builder.Length;

        foreach (Service service in serviceProvider.SortedServiceList)
            foreach (PropertyDependency dependency in service.PropertyDependencyList)
                if (dependency.IsCircular && dependency.IsInit) {
                    builder.AppendIndent(indent);
                    builder.Append("[System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"set_");
                    builder.Append(dependency.Name);
                    builder.Append("\")]\n");

                    builder.AppendIndent(indent);
                    builder.Append("private extern static void Set_");
                    builder.Append(service.Name);
                    builder.Append('_');
                    builder.Append(dependency.Name);
                    builder.Append("(global::");
                    builder.AppendClosedFullyQualified(dependency.ImplementationBaseName);
                    builder.Append(" instance, global::");
                    builder.AppendClosedFullyQualified(dependency.Service!.ServiceType);
                    builder.Append(" value);\n\n");
                }

        if (builder.Length == currentPosition)
            builder.Length -= 3;
        else
            builder.Length--;
    }

    #endregion


    #region ServiceTreeBuilder

    /// <summary>
    /// <para>Contains all circular dependencies where dependency is initialized before service.</para>
    /// <para>
    /// Segmented into stackframes:<br />
    /// - start of current stackframe = <see cref="circularDependencyStackIndex"/><br />
    /// - other stack frame locations are the local variables 'currentStackIndex'
    /// </para>
    /// <para>constructor service tree has only 1 segment/stackframe.</para>
    /// </summary>
    private readonly List<(Service service, int number, PropertyDependency dependency)> circularStack = [];

    /// <summary>
    /// <para>Contains all circular dependencies where dependency is not initialized when traversing service.</para>
    /// <para>It is only used in lazy service tree.</para>
    /// </summary>
    private readonly List<(Service service, int number, PropertyDependency dependency)> circularNotInitList = [];


    private readonly List<(Service service, int number)> transientDisposeList = [];
    private readonly List<(Service service, int number)> transientAsyncDisposeList = [];
    private readonly List<(Service service, int number)> scopeTransientDisposeList = [];
    private readonly List<(Service service, int number)> scopeTransientAsyncDisposeList = [];

    private (List<(Service service, int number)> syncList, List<(Service service, int number)> asyncList) currentDisposeLists;

    
    /// <summary>
    /// Counting the transient services to avoid naming conflicts.
    /// </summary>
    private int transientNumber;

    /// <summary>
    /// Flag to track that only the first Singleton gets locked
    /// </summary>
    private bool isLocked = false;

    /// <summary>
    /// Flag to track that only the first Scoped gets locked
    /// </summary>
    private bool isLockedScope = false;



    /// <summary>
    /// <para>
    /// Appends a construction chain of the subtree from every service in <see cref="serviceList"/><br />
    /// The chain starts with the leave nodes.
    /// </para>
    /// <para>Then the circlar dependencies are appended.</para>
    /// <para>And then the dispose list.</para>
    /// </summary>
    private void AppendConstructorServices() {
        transientNumber = 0;
        currentDisposeLists = disposeLists;
        serviceProvider.NextDependencyTreeFlag();

        {
            int initialLength = builder.Length;
                
            foreach (Service service in serviceList)
                if (service.CreationTimeTransitive is CreationTiming.Constructor) {
                    AppendCounstructorServiceTreeRecursion(service);

                    // init all circularDependency services - must be a for loop -> while iterating items could be added
                    for (int i = 0; i < circularStack.Count; i++)
                        AppendCounstructorServiceTreeRecursion(circularStack[i].dependency.Service!);
                }
                
            if (builder.Length > initialLength)
                builder.Append('\n');
        }

        if (circularStack.Count > 0) {
            AppendCircularDependencyList(0);

            builder.Append('\n');
        }

        if (hasDisposeList)
            AppendDiposeListConstructor(currentDisposeLists.syncList, DISPOSE_LIST);
        if (hasAsyncDisposeList)
            AppendDiposeListConstructor(currentDisposeLists.asyncList, ASYNC_DISPOSE_LIST);
        AppendDisposeListLazy(transientDisposeList, DISPOSE_LIST, appendServiceProviderField: true);
        AppendDisposeListLazy(transientAsyncDisposeList, ASYNC_DISPOSE_LIST, appendServiceProviderField: true);
    }

    /// <summary>
    /// Special method CreateScope()
    /// </summary>
    private void AppendCreateScopeServiceTree() {
        transientNumber = 0;
        currentDisposeLists = disposeLists;
        serviceProvider.NextDependencyTreeFlag();
        Service service = serviceProvider.CreateScope!;

        Span<int> transientNumberList = stackalloc int[service.ConstructorDependencyList.Count + service.PropertyDependencyList.Count];
        Span<int> transientNumberConstructor = transientNumberList[..service.ConstructorDependencyList.Count];
        Span<int> transientNumberProperty = transientNumberList[service.ConstructorDependencyList.Count..];
        for (int i = 0; i < service.ConstructorDependencyList.Count; i++) {
            ConstructorDependency dependency = service.ConstructorDependencyList[i];
            if (dependency.HasAttribute)
                transientNumberConstructor[i] = AppendLazyServiceTreeRecursion(dependency.Service!, 0);
        }
        for (int i = 0; i < service.PropertyDependencyList.Count; i++) {
            PropertyDependency dependency = service.PropertyDependencyList[i];
            if (dependency.HasAttribute)
                transientNumberProperty[i] = AppendLazyServiceTreeRecursion(dependency.Service!, 0);
        }

        AppendDisposeListLazy(transientDisposeList, DISPOSE_LIST, appendServiceProviderField: false);
        AppendDisposeListLazy(transientAsyncDisposeList, ASYNC_DISPOSE_LIST, appendServiceProviderField: false);

        builder.AppendIndent(indent);
        builder.Append("return new global::");
        builder.AppendOpenFullyQualified(serviceProvider.IdentifierScope);
        AppendConstructorDependencyListCreateScope(service, transientNumberConstructor);
        AppendPropertyDependencyListCreateScope(service, transientNumberProperty);
        builder.Append(";\n");
    }

    /// <summary>
    /// <para>
    /// Appends a construction chain of the subtree from the given node (node itself inclusive).<br />
    /// The chain starts with the leave nodes.
    /// </para>
    /// <para>Then the circlar dependencies are appended.</para>
    /// <para>And then the dispose list.</para>
    /// </summary>
    /// <param name="service"></param>
    private void AppendLazyService(Service service) {
        transientNumber = 0;
        currentDisposeLists = disposeLists;
        serviceProvider.NextDependencyTreeFlag();

        AppendLazyServiceTreeRecursion(service, 0);

        AppendDisposeListLazy(currentDisposeLists.syncList, DISPOSE_LIST, appendServiceProviderField: false);
        AppendDisposeListLazy(currentDisposeLists.asyncList, ASYNC_DISPOSE_LIST, appendServiceProviderField: false);
        AppendDisposeListLazy(transientDisposeList, DISPOSE_LIST, appendServiceProviderField: true);
        AppendDisposeListLazy(transientAsyncDisposeList, ASYNC_DISPOSE_LIST, appendServiceProviderField: true);
    }

    /// <summary>
    /// <para>
    /// Appends a construction chain of the subtree from the given node (node itself inclusive).<br />
    /// The chain starts with the leave nodes.
    /// </para>
    /// <para>Then the circlar dependencies are appended.</para>
    /// <para>And then the dispose list.</para>
    /// </summary>
    /// <param name="service"></param>
    /// <returns></returns>
    private int AppendTransientService(Service service) {
        transientNumber = 0;
        currentDisposeLists = disposeLists;
        serviceProvider.NextDependencyTreeFlag();

        int currentTransientNumber = AppendLazyServiceTreeRecursion(service, 0);

        AppendDisposeListLazy(currentDisposeLists.syncList, DISPOSE_LIST, appendServiceProviderField: false);
        AppendDisposeListLazy(currentDisposeLists.asyncList, ASYNC_DISPOSE_LIST, appendServiceProviderField: false);
        AppendDisposeListLazy(transientDisposeList, DISPOSE_LIST, appendServiceProviderField: true);
        AppendDisposeListLazy(transientAsyncDisposeList, ASYNC_DISPOSE_LIST, appendServiceProviderField: true);

        return currentTransientNumber;
    }


    private int AppendCounstructorServiceTreeRecursion(Service service) {
        if (service.TreeState.visited.HasFlag(serviceProvider.DependencyTreeFlag))
            return 0;

        if (!service.Lifetime.HasFlag(ServiceLifetime.Transient))
            service.TreeState.visited = serviceProvider.DependencyTreeFlag;


        if (service.Lifetime.HasFlag(ServiceLifetime.Delegate))
            return 0;

        if (service.Implementation.Type is MemberType.Field)
            return 0;


        Span<int> transientNumberList = stackalloc int[service.ConstructorDependencyList.Count + service.PropertyDependencyList.Count];
        Span<int> transientNumberConstructor = transientNumberList[..service.ConstructorDependencyList.Count];
        Span<int> transientNumberProperty = transientNumberList[service.ConstructorDependencyList.Count..];
        for (int i = 0; i < service.ConstructorDependencyList.Count; i++) {
            ConstructorDependency dependency = service.ConstructorDependencyList[i];
            if (isScopeProvider && dependency.Service!.Lifetime is ServiceLifetime.Singleton) {
                if (dependency.Service.CreationTimeTransitive is CreationTiming.Constructor)
                    continue;

                currentDisposeLists = (transientDisposeList, transientAsyncDisposeList);
                transientNumberConstructor[i] = AppendLazyServiceTreeRecursion(dependency.Service!, circularStack.Count);
                currentDisposeLists = (scopeTransientDisposeList, scopeTransientAsyncDisposeList);
            }
            else
                transientNumberConstructor[i] = AppendCounstructorServiceTreeRecursion(dependency.Service!);
        }
        for (int i = 0; i < service.PropertyDependencyList.Count; i++) {
            PropertyDependency dependency = service.PropertyDependencyList[i];
            if (!dependency.IsCircular)
                if (isScopeProvider && dependency.Service!.Lifetime is ServiceLifetime.Singleton) {
                    if (dependency.Service.CreationTimeTransitive is CreationTiming.Constructor)
                        continue;

                    currentDisposeLists = (transientDisposeList, transientAsyncDisposeList);
                    transientNumberProperty[i] = AppendLazyServiceTreeRecursion(dependency.Service!, circularStack.Count);
                    currentDisposeLists = (scopeTransientDisposeList, scopeTransientAsyncDisposeList);
                }
                else
                    transientNumberProperty[i] = AppendCounstructorServiceTreeRecursion(dependency.Service!);
            else
                circularStack.Add((service, transientNumber, dependency));
        }

        service.TreeState.init = serviceProvider.DependencyTreeFlag;
        return AppendServiceCreation(service, transientNumberConstructor, transientNumberProperty);
    }

    private int AppendLazyServiceTreeRecursion(Service service, int circularStackIndex) {
        if (service.TreeState.visited.HasFlag(serviceProvider.DependencyTreeFlag))
            return 0;

        if (!service.Lifetime.HasFlag(ServiceLifetime.Transient))
            service.TreeState.visited = serviceProvider.DependencyTreeFlag;


        if (service.Lifetime.HasFlag(ServiceLifetime.Delegate))
            return 0;

        if (service.Implementation.Type is MemberType.Field)
            return 0;

        if (service.CreationTimeTransitive is CreationTiming.Constructor)
            return 0;


        bool lockedOnThisLevel = false;
        if (service.Lifetime is ServiceLifetime.Singleton) {
            if (serviceProvider.ThreadSafe && !isLocked) {
                isLocked = true;
                lockedOnThisLevel = true;

                builder.AppendIndent(indent);
                builder.Append("if (");
                if (isScopeProvider)
                    AppendServiceProviderField();
                builder.Append('_');
                builder.AppendFirstLower(service.Name);
                builder.Append(" is null)\n");
                indent.IncreaseLevel(); // (+1)

                builder.AppendIndent(indent);
                builder.Append("lock (");
                if (!isScopeProvider)
                    builder.Append("this");
                else {
                    builder.Append('_');
                    builder.AppendFirstLower(serviceProvider.Identifier.Name);
                }
                builder.Append(")\n");
                indent.IncreaseLevel(); // (+2)
            }

            builder.AppendIndent(indent);
            builder.Append("if (");
            if (isScopeProvider)
                AppendServiceProviderField();
            builder.Append('_');
            builder.AppendFirstLower(service.Name);
            builder.Append(" is null) {\n");
            indent.IncreaseLevel(); // +1
        }
        else if (service.Lifetime is ServiceLifetime.Scoped) {
            if (serviceProvider.ThreadSafeScope && !isLockedScope) {
                isLockedScope = true;
                lockedOnThisLevel = true;

                builder.AppendIndent(indent);
                builder.Append("if (_");
                builder.AppendFirstLower(service.Name);
                builder.Append(" is null)\n");
                indent.IncreaseLevel(); // (+1)

                builder.AppendIndent(indent);
                builder.Append("lock (this)\n");
                indent.IncreaseLevel(); // (+2)
            }

            builder.AppendIndent(indent);
            builder.Append("if (_");
            builder.AppendFirstLower(service.Name);
            builder.Append(" is null) {\n");
            indent.IncreaseLevel(); // +1
        }


        Span<int> transientNumberList = stackalloc int[service.ConstructorDependencyList.Count + service.PropertyDependencyList.Count];
        Span<int> transientNumberConstructor = transientNumberList[..service.ConstructorDependencyList.Count];
        Span<int> transientNumberProperty = transientNumberList[service.ConstructorDependencyList.Count..];
        for (int i = 0; i < service.ConstructorDependencyList.Count; i++) {
            ConstructorDependency dependency = service.ConstructorDependencyList[i];
            if (service.Lifetime is ServiceLifetime.Singleton && currentDisposeLists.syncList == scopeTransientDisposeList) {
                currentDisposeLists = (transientDisposeList, transientAsyncDisposeList);
                transientNumberConstructor[i] = AppendLazyServiceTreeRecursion(dependency.Service!, circularStack.Count);
                currentDisposeLists = (scopeTransientDisposeList, scopeTransientAsyncDisposeList);
            }
            else
                transientNumberConstructor[i] = AppendLazyServiceTreeRecursion(dependency.Service!, circularStack.Count);
        }

        int circularNotInitDependencyListIndex = circularNotInitList.Count;
        for (int i = 0; i < service.PropertyDependencyList.Count; i++) {
            PropertyDependency dependency = service.PropertyDependencyList[i];
            if (!dependency.IsCircular) {
                if (service.Lifetime is ServiceLifetime.Singleton && currentDisposeLists.syncList == scopeTransientDisposeList) {
                    currentDisposeLists = (transientDisposeList, transientAsyncDisposeList);
                    transientNumberProperty[i] = AppendLazyServiceTreeRecursion(dependency.Service!, circularStack.Count);
                    currentDisposeLists = (scopeTransientDisposeList, scopeTransientAsyncDisposeList);
                }
                else
                    transientNumberProperty[i] = AppendLazyServiceTreeRecursion(dependency.Service!, circularStack.Count);
            }
            else {
                if (dependency.Service!.CreationTimeTransitive is CreationTiming.Constructor || dependency.Service.TreeState.init.HasFlag(serviceProvider.DependencyTreeFlag))
                    circularStack.Add((service, transientNumber, dependency));
                else
                    circularNotInitList.Add((service, transientNumber, dependency));
            }
        }

        int currentTransientNumber = AppendServiceCreation(service, transientNumberConstructor, transientNumberProperty);

        // init all circularDependency services - must be a for loop -> while iterating collection get modified, but Count stays the same
        {
            List<(Service service, int number, PropertyDependency dependency)> currentList = circularStack;
            int startIndex = circularStackIndex;
            for (int listCount = 0; listCount < 2; listCount++) {
                for (int i = startIndex; i < currentList.Count; i++) {
                    if (service.Lifetime is ServiceLifetime.Singleton && currentDisposeLists.syncList == scopeTransientDisposeList) {
                        currentDisposeLists = (transientDisposeList, transientAsyncDisposeList);
                        AppendLazyServiceTreeRecursion(currentList[i].dependency.Service!, circularStack.Count);
                        currentDisposeLists = (scopeTransientDisposeList, scopeTransientAsyncDisposeList);
                    }
                    else
                        AppendLazyServiceTreeRecursion(currentList[i].dependency.Service!, circularStack.Count);
                }
                currentList = circularNotInitList;
                startIndex = circularNotInitDependencyListIndex;
            }
        }

        service.TreeState.init = serviceProvider.DependencyTreeFlag;
        AppendCircularDependencyList(circularStackIndex);
        AppendCircularNotInitDependencyList(service);


        if (service.Lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped) {
            indent.DecreaseLevel(); // +0
            builder.AppendIndent(indent);
            builder.Append("}\n");

            if (lockedOnThisLevel) {
                indent.DecreaseLevel(); // (+1)
                indent.DecreaseLevel(); // (+0)

                if (service.Lifetime is ServiceLifetime.Singleton)
                    isLocked = false;
                else
                    isLockedScope = false;
            }
        }

        return currentTransientNumber;
    }


    /// <summary>
    /// Appends all service-dependencies at the given index to the end in <see cref="circularStack"/>.<br />
    /// The stackframe is cleared afterwards.
    /// </summary>
    /// <param name="circularStackIndex"></param>
    private void AppendCircularDependencyList(int circularStackIndex) {
        for (int i = circularStackIndex; i < circularStack.Count; i++) {
            (Service service, int number, PropertyDependency dependency) = circularStack[i];
            AppendCircularDependency(service, number, dependency);
        }

        circularStack.RemoveRange(circularStackIndex, circularStack.Count - circularStackIndex);
    }

    /// <summary>
    /// Appends all service-dependencies in <see cref="circularNotInitList"/> where currentService is the dependency.<br />
    /// The list is cleared afterwards.
    /// </summary>
    /// <param name="currentService"></param>
    private void AppendCircularNotInitDependencyList(Service currentService) {
        for (int i = circularNotInitList.Count - 1; i >= 0; i--) {
            (Service service, int number, PropertyDependency dependency) = circularNotInitList[i];
            if (ReferenceEquals(currentService, dependency.Service)) {
                AppendCircularDependency(service, number, dependency);
                circularNotInitList.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Should only be called from <see cref="AppendCircularDependencyList"/> or <see cref="AppendCircularNotInitDependencyList"/>
    /// </summary>
    private void AppendCircularDependency(Service service, int number, PropertyDependency dependency) {
        builder.AppendIndent(indent);
        if (dependency.IsInit) {
            builder.Append("Set_");
            builder.Append(service.Name);
            builder.Append('_');
            builder.Append(dependency.Name);
            builder.Append('(');

            switch (service.Lifetime) {
                case ServiceLifetime.Singleton:
                    if (isScopeProvider)
                        AppendServiceProviderField();
                    goto case ServiceLifetime.Scoped;
                case ServiceLifetime.Scoped:
                    builder.AppendServiceField(service);
                    break;
                default:
                    builder.AppendFirstLower(service.Name);
                    if (number > 0) {
                        builder.Append('_');
                        builder.Append(number);
                    }
                    break;
            }
            builder.Append(", ");
            AppendDependencyIdentifier(dependency, number);
            builder.Append(')');
        }
        else {
            switch (service.Lifetime) {
                case ServiceLifetime.Singleton:
                    if (isScopeProvider)
                        AppendServiceProviderField();
                    goto case ServiceLifetime.Scoped;
                case ServiceLifetime.Scoped:
                    builder.AppendServiceField(service);
                    break;
                default:
                    builder.AppendFirstLower(service.Name);
                    if (number > 0) {
                        builder.Append('_');
                        builder.Append(number);
                    }
                    break;
            }
            builder.Append('.');
            builder.Append(dependency.Name);
            builder.Append(" = ");
            AppendDependencyIdentifier(dependency, number);
        }
        builder.Append(";\n");
    }


    /// <summary>
    /// <para>Initializes the disposeList/asyncDisposeList with a collection expression containing all transients created in the constructor.</para>
    /// <para>DisposeList is created first, then asyncDisposeList, but if a service implements both, it is put in asyncDisposeList.</para>
    /// <para>The list is cleared afterwards.</para>
    /// </summary>
    /// <param name="disposeList"></param>
    /// <param name="disposeListName"></param>
    private void AppendDiposeListConstructor(List<(Service service, int number)> disposeList, string disposeListName) {
        builder.AppendIndent(indent);
        builder.Append(disposeListName);
        builder.Append(" = [");
        switch (disposeList.Count) {
            case 0:
                break;
            case 1:
                builder.AppendFirstLower(disposeList[0].service.Name);
                if (disposeList[0].number > 0) {
                    builder.Append('_');
                    builder.Append(disposeList[0].number);
                }
                break;
            default:
                indent.IncreaseLevel(); // +1
                foreach ((Service service, int number) in disposeList) {
                    builder.Append('\n');
                    builder.AppendIndent(indent);
                    builder.AppendFirstLower(service.Name);
                    if (number > 0) {
                        builder.Append('_');
                        builder.Append(number);
                    }
                    builder.Append(",");
                }
                indent.DecreaseLevel(); // +0
                builder.Length--;

                builder.Append('\n');
                builder.AppendIndent(indent);
                break;
        }

        builder.Append("];\n");

        disposeList.Clear();
    }

    /// <summary>
    /// <para>Appends for each disposable transient service an add statement to the corresponding disposeList.</para>
    /// <para>DisposeList is appended first, then asyncDisposeList, but if a service implements both, it is added to asyncDisposeList.</para>
    /// <para>The list is cleared afterwards.</para>
    /// </summary>
    /// <param name="disposeList"></param>
    /// <param name="disposeListName"></param>
    /// <param name="appendServiceProviderField"></param>
    private void AppendDisposeListLazy(List<(Service service, int number)> disposeList, string disposeListName, bool appendServiceProviderField) {
        if (disposeList.Count == 0)
            return;

        if (threadSafe) {
            builder.AppendIndent(indent);
            builder.Append("lock (");
            if (appendServiceProviderField)
                AppendServiceProviderField();
            builder.Append(disposeListName);
            builder.Append(") {\n");
            indent.IncreaseLevel(); // +1
        }

        foreach ((Service service, int number) in disposeList) {
            builder.AppendIndent(indent);
            if (appendServiceProviderField)
                AppendServiceProviderField();
            builder.Append(disposeListName);
            builder.Append(".Add(");
            builder.AppendFirstLower(service.Name);
            if (number > 0) {
                builder.Append('_');
                builder.Append(number);
            }
            builder.Append(");\n");
        }

        if (threadSafe) {
            indent.DecreaseLevel(); // +0
            builder.AppendIndent(indent);
            builder.Append("}\n");
        }

        disposeList.Clear();
    }


    /// <summary>
    /// service field gets initialized with the constructor or factory method<br />
    /// field = constructor / property / method
    /// </summary>
    /// <param name="service"></param>
    /// <param name="transientNumberConstructor"></param>
    /// <param name="transientNumberProperty"></param>
    /// <returns></returns>
    private int AppendServiceCreation(Service service, Span<int> transientNumberConstructor, Span<int> transientNumberProperty) {
        if (service.Lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped) {
            builder.AppendIndent(indent);
            if (isScopeProvider && service.Lifetime is ServiceLifetime.Singleton)
                AppendServiceProviderField();
            builder.AppendServiceField(service);
            builder.Append(" = ");
            if (service.Implementation.Type == MemberType.None) {
                builder.Append("new global::");
                builder.AppendClosedFullyQualified(service.ImplementationType);
                AppendConstructorDependencyList(service, transientNumberConstructor);
                AppendPropertyDependencyList(service, transientNumberProperty);
            }
            else {
                if (isScopeProvider && !service.Implementation.IsScoped && !service.Implementation.IsStatic)
                    AppendServiceProviderField();
                builder.AppendImplementationName(service);
                if (service.Implementation.Type == MemberType.Method)
                    AppendConstructorDependencyList(service, transientNumberConstructor);
            }
            builder.Append(";\n");
            return 0;
        }
        else { // ServiceLifetime.Transient
            builder.AppendIndent(indent);
            builder.Append("global::");
            builder.AppendClosedFullyQualified(service.ImplementationType);
            builder.Append(' ');
            builder.AppendFirstLower(service.Name);
            if (transientNumber > 0) {
                builder.Append('_');
                builder.Append(transientNumber);
            }
            builder.Append(" = ");

            if (service.Implementation.Type == MemberType.None) {
                builder.Append("new global::");
                builder.AppendClosedFullyQualified(service.ImplementationType);
                AppendConstructorDependencyList(service, transientNumberConstructor);
                AppendPropertyDependencyList(service, transientNumberProperty);
            }
            else {
                if (isScopeProvider && !service.Implementation.IsScoped && !service.Implementation.IsStatic)
                    AppendServiceProviderField();
                builder.AppendImplementationName(service);
                if (service.Implementation.Type == MemberType.Method)
                    AppendConstructorDependencyList(service, transientNumberConstructor);
            }
            builder.Append(";\n");

            if (generateDisposeMethods is not DisposeGeneration.NoDisposing)
                if (service.IsAsyncDisposable)
                    currentDisposeLists.asyncList.Add((service, transientNumber));
                else if (service.IsDisposable)
                    currentDisposeLists.syncList.Add((service, transientNumber));

            return transientNumber++;
        }
    }


    /// <summary>
    /// <para>Appends "(service1, service2, ..., serviceN)"</para>
    /// <para>If <see cref="Service.ConstructorDependencyList"/> is empty, only "()" is appended.</para>
    /// </summary>
    /// <param name="service"></param>
    /// <param name="numberList"></param>
    private void AppendConstructorDependencyList(Service service, Span<int> numberList) {
        builder.Append('(');
        
        if (service.ConstructorDependencyList.Count > 0) {
            for (int i = 0; i < service.ConstructorDependencyList.Count; i++) {
                ConstructorDependency dependency = service.ConstructorDependencyList[i];
                
                if (dependency.Service!.IsRefable && !keyword.HasFlag(TypeKeyword.Struct))
                    builder.Append(dependency.ByRef.AsString());
                AppendDependencyIdentifier(dependency, numberList[i]);
                builder.Append(", ");
            }
            builder.Length -= 2;
        }

        builder.Append(')');
    }

    /// <summary>
    /// <para>Appends "(service1, service2, ..., serviceN)"</para>
    /// <para>If <see cref="Service.ConstructorDependencyList"/> is empty, only "()" is appended.</para>
    /// </summary>
    /// <param name="service"></param>
    /// <param name="numberList"></param>
    private void AppendConstructorDependencyListCreateScope(Service service, Span<int> numberList) {
        builder.Append('(');

        if (service.ConstructorDependencyList.Count > 0) {
            for (int i = 0; i < service.ConstructorDependencyList.Count; i++) {
                ConstructorDependency dependency = service.ConstructorDependencyList[i];

                if (!dependency.HasAttribute)
                    builder.AppendFirstLower(dependency.Name);
                else {
                    if (dependency.Service!.IsRefable && !keyword.HasFlag(TypeKeyword.Struct))
                        builder.Append(dependency.ByRef.AsString());
                    AppendDependencyIdentifier(dependency, numberList[i]);
                }
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
    /// <param name="numberList"></param>
    private void AppendPropertyDependencyList(Service service, Span<int> numberList) {
        if (service.PropertyDependencyList.Count > 0) {
            builder.Append(" {");

            int builderLength = builder.Length;
            indent.IncreaseLevel(); // +1
            for (int i = 0; i < service.PropertyDependencyList.Count; i++) {
                PropertyDependency dependency = service.PropertyDependencyList[i];

                if (dependency.IsCircular) {
                    if (dependency.IsRequired) {
                        builder.Append('\n');
                        builder.AppendIndent(indent);
                        builder.Append(dependency.Name);
                        builder.Append(" = ");
                        if (dependency.Service!.CreationTimeTransitive is CreationTiming.Lazy)
                            AppendDependencyIdentifier(dependency, numberList[i]);
                        else
                            builder.Append("default!");
                        builder.Append(',');
                    }
                }
                else {
                    builder.Append('\n');
                    builder.AppendIndent(indent);
                    builder.Append(dependency.Name);
                    builder.Append(" = ");
                    AppendDependencyIdentifier(dependency, numberList[i]);
                    builder.Append(',');
                }
            }
            indent.DecreaseLevel(); // +0

            if (builderLength != builder.Length) {
                // at least one item got appended
                builder[^1] = '\n'; // remove last ','
                builder.AppendIndent(indent);
                builder.Append('}');
            }
            else
                // nothing got appended
                builder.Length -= 2; // remove " {"
        }
    }

    /// <summary>
    /// <para>Appends " { name1 = service1, name2 = service2, ..., nameN = serviceN }"</para>
    /// <para>If <see cref="Service.PropertyDependencyList"/> is empty, nothing is appended.</para>
    /// </summary>
    /// <param name="service"></param>
    /// <param name="numberList"></param>
    private void AppendPropertyDependencyListCreateScope(Service service, Span<int> numberList) {
        if (service.PropertyDependencyList.Count > 0) {
            builder.Append(" {");

            indent.IncreaseLevel(); // +1
            for (int i = 0; i < service.PropertyDependencyList.Count; i++) {
                PropertyDependency dependency = service.PropertyDependencyList[i];

                builder.Append('\n');
                builder.AppendIndent(indent);
                builder.Append(dependency.Name);
                builder.Append(" = ");
                if (!dependency.HasAttribute)
                    builder.AppendFirstLower(dependency.Name);
                else
                    AppendDependencyIdentifier(dependency, numberList[i]);
                builder.Append(',');
            }
            indent.DecreaseLevel(); // +0

            builder[^1] = '\n'; // remove last ','
            builder.AppendIndent(indent);
            builder.Append('}');
        }
    }


    /// <summary>
    /// if Singleton or Scoped -> ServiceField<br />
    /// if Transient -> localVariable<br />
    /// if Delegate -> ServiceGetter
    /// </summary>+
    /// <param name="dependency"></param>
    /// <param name="transientNumber"></param>
    private void AppendDependencyIdentifier(Dependency dependency, int transientNumber) {
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
            if (transientNumber > 0) {
                builder.Append('_');
                builder.Append(transientNumber);
            }
        }
        else // if (dependency.Service!.Lifetime.HasFlag(ServiceLifetime.Delegate))
            builder.AppendServiceGetter(dependency.Service!);
    }

    #endregion


    #region Shared

    /// <summary>
    /// Appends serviceProvider field with casting to implementation type with trailing '.':<br />
    /// "_{providerName}."
    /// </summary>
    private void AppendServiceProviderField() {
        builder.Append('_');
        builder.AppendFirstLower(serviceProvider.Identifier.Name);
        builder.Append('.');
    }

    /// <summary>
    /// <para>Appends "(service1Type service1Name, service2Type service2Name, ..., serviceNType serviceNName)"</para>
    /// <para>If dependencyList is empty, only "()" is appended.</para>
    /// </summary>
    /// <param name="dependencyList"></param>
    private void AppendParameterDependencyList(IEnumerable<Dependency> dependencyList) {
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
    public void AppendClassSummary() {
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
    public void AppendServiceSummary(Service service) {
        builder.AppendIndent(indent);
        builder.Append("/// <summary>\n");

        builder.AppendIndent(indent);
        builder.Append("/// Lifetime: <see cref=\"global::CircleDIAttributes.");
        builder.Append(service.Lifetime.AsString());
        builder.Append("Attribute{TService}\">");
        builder.Append(service.Lifetime.AsString());
        builder.Append("</see><br />\n");

        builder.AppendIndent(indent);
        builder.Append("/// Service type: <see cref=\"global::");
        {
            int startIndex = builder.Length;
            builder.AppendClosedFullyQualified(service.ServiceType);
            builder.Replace('<', '{', startIndex, builder.Length - startIndex);
            builder.Replace('>', '}', startIndex, builder.Length - startIndex);
        }
        builder.Append("\"/><br />\n");

        builder.AppendIndent(indent);
        builder.Append("/// Implementation type: <see cref=\"global::");
        {
            int startIndex = builder.Length;
            builder.AppendClosedFullyQualified(service.ImplementationType);
            builder.Replace('<', '{', startIndex, builder.Length - startIndex);
            builder.Replace('>', '}', startIndex, builder.Length - startIndex);
        }
        builder.Append("\"/>\n");

        builder.AppendIndent(indent);
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
    public void AppendCreateScopeSummary() {
        builder.AppendIndent(indent);
        builder.Append("/// <summary>\n");

        builder.AppendIndent(indent);
        builder.Append("/// Creates an instance of a ScopeProvider together with all <see cref=\"global::CircleDIAttributes.CreationTiming.Constructor\">non-lazy</see> scoped services.\n");

        builder.AppendIndent(indent);
        builder.Append("/// </summary>\n");
    }

    #endregion
}
