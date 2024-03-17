﻿using CircleDI.Defenitions;
using CircleDI.Extensions;
using System.Text;

namespace CircleDI.Generation;

public struct StringBuilderExtension(StringBuilder builder, ServiceProvider serviceProvider) {
    private bool isScopeProvider = false;
    private List<Service> serviceList = serviceProvider.SingletonList;
    private ClassStructKeyword keyword = serviceProvider.Keyword;
    private DisposeGeneration generateDisposeMethods = serviceProvider.GenerateDisposeMethods;
    private bool hasDisposeMethod = serviceProvider.HasDisposeMethod;
    private bool hasDisposeAsyncMethod = serviceProvider.HasDisposeAsyncMethod;
    private bool threadSafe = serviceProvider.ThreadSafe;
    public Indent indent = new();

    public void SetToScope() {
        isScopeProvider = true;
        serviceList = serviceProvider.ScopedList;
        keyword = serviceProvider.KeywordScope;
        generateDisposeMethods = serviceProvider.GenerateDisposeMethodsScope;
        hasDisposeMethod = serviceProvider.HasDisposeMethodScope;
        hasDisposeAsyncMethod = serviceProvider.HasDisposeAsyncMethodScope;
        threadSafe = serviceProvider.ThreadSafeScope;
        indent.IncreaseLevel();
    }


    #region Constructor Services

    /// <summary>
    /// Appends MemberNotNullAttribute with all necessary members listed
    /// </summary>
    /// <param name="serviceList"><see cref="ServiceProvider.SingletonList"/> or <see cref="ServiceProvider.ScopedList"/></param>
    /// <param name="isScopeProvider"></param>
    public readonly void AppendInitServicesMemberNotNull() {
        // if serviceList has Any()
        for (int i = 0; i < serviceList.Count; i++) {
            Service service = serviceList[i];
            if (service.CreationTime == CreationTiming.Constructor && service.Implementation.Type != MemberType.Field) {
                builder.Append(indent.Sp4);
                builder.Append($"[System.Diagnostics.CodeAnalysis.MemberNotNull(");
                if (isScopeProvider)
                    builder.Append("nameof(__serviceProvider), ");

                while (true) {
                    builder.Append("nameof(");
                    builder.Append('_');
                    builder.AppendFirstLower(service.Name);
                    builder.Append(')');
                    builder.Append(',');
                    builder.Append(' ');

                    do {
                        i++;
                        if (i == serviceList.Count)
                            goto doubleBreak;
                        service = serviceList[i];
                    } while (service.CreationTime != CreationTiming.Constructor || service.Implementation.Type == MemberType.Field);
                }
                doubleBreak:

                builder.Length -= 2;
                builder.Append(')');
                builder.Append(']');
                builder.Append('\n');
                return;
            }
        }
        // else
        {
            if (isScopeProvider) {
                builder.Append(indent.Sp4);
                builder.Append($"[System.Diagnostics.CodeAnalysis.MemberNotNull(nameof(__serviceProvider))]\n");
            }
        }
    }

    /// <summary>
    /// Constructor service initialization
    /// </summary>
    /// <param name="serviceList"><see cref="ServiceProvider.SingletonList"/> or <see cref="ServiceProvider.ScopedList"/><</param>
    public readonly void AppendConstructorServices() {
        List<(Service, PropertyDependency)> circularDependencies = [];
        foreach (Service service in serviceList)
            AppendConstructorService(service, circularDependencies);

        AppendCircularDependencies(circularDependencies, indent.Sp8);

        builder.Append(indent.Sp4);
        builder.Append('}');
        builder.Append('\n');
        builder.Append('\n');
    }

    private readonly void AppendConstructorService(Service service, List<(Service, PropertyDependency)> circularDependencies) {
        if (service.TreeState.HasFlag(DependencyTreeFlags.Generated))
            return;
        service.TreeState |= DependencyTreeFlags.Generated;

        if (service.CreationTime == CreationTiming.Lazy)
            return;
        if (service.Implementation.Type == MemberType.Field)
            return;
        if (service.Lifetime.HasFlag(ServiceLifetime.Transient))
            return;
        if (service.Lifetime == ServiceLifetime.Delegate)
            return;


        foreach (Dependency dependency in service.Dependencies)
            AppendConstructorService(dependency.Service!, circularDependencies);

        builder.Append(indent.Sp8);
        builder.AppendServiceField(service);
        builder.Append(" = ");
        switch (service.Implementation.Type) {
            case MemberType.None:
                builder.Append("new global::");
                builder.Append(service.ImplementationType);
                AppendConstructorDependencyList(service);
                AppendPropertyDependencyList(service, circularDependencies, indent.Sp8);
                break;
            case MemberType.Method:
                if (service.Lifetime == ServiceLifetime.Scoped && !service.Implementation.IsScoped && !service.Implementation.IsStatic) {
                    builder.Append("((global::");
                    builder.AppendNamespaceList(serviceProvider.NameSpaceList);
                    builder.AppendContainingTypeList(serviceProvider.ContainingTypeList);
                    builder.Append(serviceProvider.Name);
                    builder.Append(")__serviceProvider).");
                }
                builder.Append(service.Implementation.Name);
                AppendConstructorDependencyList(service);
                break;
            case MemberType.Property:
                if (service.Lifetime == ServiceLifetime.Scoped && !service.Implementation.IsScoped && !service.Implementation.IsStatic) {
                    builder.Append("((global::");
                    builder.AppendNamespaceList(serviceProvider.NameSpaceList);
                    builder.AppendContainingTypeList(serviceProvider.ContainingTypeList);
                    builder.Append(serviceProvider.Name);
                    builder.Append(")__serviceProvider).");
                }
                builder.Append(service.Implementation.Name);
                break;
        }
        builder.Append(';');
        builder.Append('\n');
    }

    #endregion


    #region Singleton/Scoped Fields and Getter

    /// <summary>
    /// Singletons/Scoped service Getter
    /// </summary>
    /// <param name="serviceList"><see cref="ServiceProvider.SingletonList"/> or <see cref="ServiceProvider.ScopedList"/><</param>
    /// <param name="threadSafe"></param>
    public readonly void AppendServicesGetter() {
        if (serviceList.Count > 0) {
            foreach (Service service in serviceList) {
                string refOrEmpty = (service.IsRefable && !keyword.HasFlag(ClassStructKeyword.Struct)) switch {
                    true => "ref ",
                    false => string.Empty
                };

                AppendServiceSummary(service);
                builder.Append(indent.Sp4);
                builder.Append("public ");
                builder.Append(refOrEmpty);
                builder.Append("global::");
                builder.Append(service.ServiceType);
                builder.Append(' ');
                builder.AppendServiceGetter(service);

                if (service.Implementation.Type == MemberType.Field)
                    AppendServicesGetterField(service, refOrEmpty);
                else if (service.CreationTime == CreationTiming.Constructor)
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
        if (service.Lifetime == ServiceLifetime.Scoped && !service.Implementation.IsScoped && !service.Implementation.IsStatic) {
            builder.Append("((global::");
            builder.AppendNamespaceList(serviceProvider.NameSpaceList);
            builder.AppendContainingTypeList(serviceProvider.ContainingTypeList);
            builder.Append(serviceProvider.Name);
            builder.Append(")__serviceProvider).");
        }
        builder.Append(service.Implementation.Name);
        builder.Append(';');
        builder.Append('\n');
    }

    private readonly void AppendServicesGetterConstructor(Service service, string refOrEmpty) {
        builder.Append(" => ");
        builder.Append(refOrEmpty);
        builder.Append('_');
        builder.AppendFirstLower(service.Name);
        builder.Append(';');
        builder.Append('\n');

        builder.Append(indent.Sp4);
        builder.Append("private global::");
        builder.Append(service.ImplementationType);
        builder.Append(' ');
        builder.Append('_');
        builder.AppendFirstLower(service.Name);
        builder.Append(';');
        builder.Append('\n');
    }

    private readonly void AppendServicesGetterLazy(Service service, string refOrEmpty) {
        builder.Append(" {\n");

        string sp8 = indent.Sp8;
        string sp12 = indent.Sp12;
        string sp16 = indent.Sp16;
        string sp20 = indent.Sp20;
        if (service.GetAccessor == GetAccess.Property) {
            builder.Append(indent.Sp8);
            builder.Append("get {\n");
            sp8 = indent.Sp12;
            sp12 = indent.Sp16;
            sp16 = indent.Sp20;
            sp20 = indent.Sp24;
        }

        builder.Append(sp8);
        string indentCreation = sp12;
        string indentClosebracket = sp8;
        if (threadSafe) {
            indentCreation = sp20;
            indentClosebracket = sp16;

            builder.Append("if (_");
            builder.AppendFirstLower(service.Name);
            builder.Append(" == null)\n");

            builder.Append(sp12);
            builder.Append("lock (this)\n");

            builder.Append(sp16);
        }
        builder.Append("if (_");
        builder.AppendFirstLower(service.Name);
        builder.Append(" == null) {\n");

        builder.Append(indentCreation);
        builder.Append('_');
        builder.AppendFirstLower(service.Name);
        builder.Append(" = ");
        switch (service.Implementation.Type) {
            case MemberType.None:
                builder.Append("new global::");
                builder.Append(service.ImplementationType);
                AppendConstructorDependencyList(service);

                List<(Service, PropertyDependency)> circularDependencies = [];
                AppendPropertyDependencyList(service, circularDependencies, indentCreation);
                builder.Append(';');
                builder.Append('\n');

                AppendCircularDependencies(circularDependencies, indentCreation);
                break;
            case MemberType.Property:
                if (service.Lifetime == ServiceLifetime.Scoped && !service.Implementation.IsScoped && !service.Implementation.IsStatic) {
                    builder.Append("((global::");
                    builder.AppendNamespaceList(serviceProvider.NameSpaceList);
                    builder.AppendContainingTypeList(serviceProvider.ContainingTypeList);
                    builder.Append(serviceProvider.Name);
                    builder.Append(")__serviceProvider).");
                }
                builder.Append(service.Implementation.Name);
                builder.Append(';');
                builder.Append('\n');
                break;
            case MemberType.Method:
                if (service.Lifetime == ServiceLifetime.Scoped && !service.Implementation.IsScoped && !service.Implementation.IsStatic) {
                    builder.Append("((global::");
                    builder.AppendNamespaceList(serviceProvider.NameSpaceList);
                    builder.AppendContainingTypeList(serviceProvider.ContainingTypeList);
                    builder.Append(serviceProvider.Name);
                    builder.Append(")__serviceProvider).");
                }
                builder.Append(service.Implementation.Name);
                AppendConstructorDependencyList(service);
                builder.Append(';');
                builder.Append('\n');
                break;
        }

        builder.Append(indentClosebracket);
        builder.Append('}');
        builder.Append('\n');
        builder.Append('\n');

        builder.Append(sp8);
        builder.Append("return ");
        builder.Append(refOrEmpty);
        builder.Append("(global::");
        builder.Append(service.ServiceType);
        builder.Append(')');
        builder.Append('_');
        builder.AppendFirstLower(service.Name);
        builder.Append(';');
        builder.Append('\n');

        if (service.GetAccessor == GetAccess.Property) {
            builder.Append(indent.Sp8);
            builder.Append('}');
            builder.Append('\n');
        }

        builder.Append(indent.Sp4);
        builder.Append('}');
        builder.Append('\n');

        builder.Append(indent.Sp4);
        builder.Append("private ");
        builder.Append("global::");
        builder.Append(service.ImplementationType);
        builder.Append('?');
        builder.Append(' ');
        builder.Append('_');
        builder.AppendFirstLower(service.Name);
        builder.Append(';');
        builder.Append('\n');
    }

    #endregion


    #region Transient Getter

    /// <summary>
    /// Transients service Getter
    /// </summary>
    /// <param name="generateDisposeMethods"></param>
    /// <param name="isScopeProvider"></param>
    /// <returns></returns>
    public readonly (bool hasDisposeList, bool hasAsyncDisposeList) AppendServicesTransient() {
        bool hasDisposeList = false;
        bool hasAsyncDisposeList = false;
        int currentPosition = builder.Length;

        foreach (Service service in serviceProvider.TransientList) {
            if (!isScopeProvider && service.Lifetime == ServiceLifetime.TransientScoped)
                continue;

            AppendServiceSummary(service);
            builder.Append(indent.Sp4);
            builder.Append("public global::");
            builder.Append(service.ServiceType);
            builder.Append(' ');

            switch ((service.IsDisposable, service.IsAsyncDisposable, generateDisposeMethods)) {
                case (true, false, not DisposeGeneration.NoDisposing):
                    hasDisposeList = true;
                    AppendTransientGetterWithDisposeList("disposeList", service);
                    break;
                case (_, true, not DisposeGeneration.NoDisposing):
                    hasAsyncDisposeList = true;
                    AppendTransientGetterWithDisposeList("asyncDisposeList", service);
                    break;
                default:
                    builder.AppendServiceGetter(service);
                    builder.Append(" => ");
                    AppendServiceCreationTransient(service, indent.Sp4);
                    break;
            }

            builder.Append('\n');
            builder.Append('\n');
        }

        if (builder.Length > currentPosition)
            builder.Append('\n');

        return (hasDisposeList, hasAsyncDisposeList);
    }

    private readonly void AppendTransientGetterWithDisposeList(string disposeListName, Service service) {
        if (service.GetAccessor == GetAccess.Property) {
            builder.Append(service.Name);
            builder.Append(" {\n");
            builder.Append(indent.Sp8);
            builder.Append("get {\n");
            AppendTransientGetterWithDisposeListCore(disposeListName, service, indent.Sp12);
            builder.Append(indent.Sp8);
            builder.Append('}');
            builder.Append('\n');
            builder.Append(indent.Sp4);
            builder.Append('}');
        }
        else {
            builder.Append("Get");
            builder.Append(service.Name);
            builder.Append("()");
            builder.Append(" {\n");
            AppendTransientGetterWithDisposeListCore(disposeListName, service, indent.Sp8);
            builder.Append(indent.Sp4);
            builder.Append('}');
        }
    }

    private readonly void AppendTransientGetterWithDisposeListCore(string disposeListName, Service service, string indentation) {
        builder.Append(indentation);
        builder.Append("global::");
        builder.Append(service.ImplementationType);
        builder.Append(' ');
        builder.AppendFirstLower(service.Name);
        builder.Append(" = ");
        AppendServiceCreationTransient(service, indentation);
        builder.Append('\n');

        if (threadSafe) {
            builder.Append(indentation);
            builder.Append("lock (");
            builder.Append(disposeListName);
            builder.Append(')');
            builder.Append('\n');
            builder.Append(Indent.SP4);
        }
        builder.Append(indentation);
        builder.Append(disposeListName);
        builder.Append(".Add(");
        builder.AppendFirstLower(service.Name);
        builder.Append(");\n");

        builder.Append(indentation);
        builder.Append("return ");
        builder.AppendFirstLower(service.Name);
        builder.Append(';');
        builder.Append('\n');
    }


    private readonly void AppendServiceCreationTransient(Service service, string indentation) {
        if (service.Implementation.Type == MemberType.None) {
            builder.Append("new global::");
            builder.Append(service.ImplementationType);
            AppendConstructorDependencyList(service);
            AppendPropertyDependencyList(service, null!, indentation); // Transient is not allowed to have circular dependencies, so circularList is not accessed
            builder.Append(';');
            return;
        }

        if (isScopeProvider && !service.Implementation.IsScoped && !service.Implementation.IsStatic) {
            if (service.Lifetime == ServiceLifetime.Transient) {
                builder.Append("__serviceProvider.");
                builder.AppendServiceGetter(service);
                builder.Append(';');
                return;
            }

            builder.Append("((global::");
            builder.AppendNamespaceList(serviceProvider.NameSpaceList);
            builder.AppendContainingTypeList(serviceProvider.ContainingTypeList);
            builder.Append(serviceProvider.Name);
            builder.Append(")__serviceProvider).");
        }

        builder.Append(service.Implementation.Name);
        if (service.Implementation.Type == MemberType.Method)
            AppendConstructorDependencyList(service);
        builder.Append(';');
    }

    #endregion


    #region Delegate Getter

    /// <summary>
    /// Delegates service Getter
    /// </summary>
    /// <param name="isScopeProvider"></param>
    public readonly void AppendServicesDelegate() {
        int currentPosition = builder.Length;

        foreach (Service service in serviceProvider.DelegateList) {
            if (!isScopeProvider && service.Implementation.IsScoped)
                continue;

            AppendServiceSummary(service);
            builder.Append(indent.Sp4);
            builder.Append("public global::");
            builder.Append(service.ServiceType);
            builder.Append(' ');
            builder.AppendServiceGetter(service);
            builder.Append(" => ");
            if (isScopeProvider && !service.Implementation.IsScoped && !service.Implementation.IsStatic) {
                builder.Append("((global::");
                builder.AppendNamespaceList(serviceProvider.NameSpaceList);
                builder.AppendContainingTypeList(serviceProvider.ContainingTypeList);
                builder.Append(serviceProvider.Name);
                builder.Append(")__serviceProvider).");
            }
            builder.Append(service.Implementation.Name);
            builder.Append(';');
            builder.Append('\n');
            builder.Append('\n');
        }

        if (builder.Length > currentPosition)
            builder.Append('\n');
    }

    #endregion


    #region IServiceProvider Switch Expression

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
        builder.Append(indent.Sp4);
        builder.Append("/// <summary>\n");
        builder.Append(indent.Sp4);
        builder.Append("/// <para>Finds all registered services of the given type.</para>\n");
        builder.Append(indent.Sp4);
        builder.Append("/// <para>\n");
        builder.Append(indent.Sp4);
        builder.Append("/// The method returns<br />\n");
        builder.Append(indent.Sp4);
        builder.Append("/// - null (when registered zero times)<br />\n");
        builder.Append(indent.Sp4);
        builder.Append("/// - given type (when registered ones)<br />\n");
        builder.Append(indent.Sp4);
        builder.Append("/// - Array of given type (when registered many times)\n");
        builder.Append(indent.Sp4);
        builder.Append("/// </para>\n");
        builder.Append(indent.Sp4);
        builder.Append("/// </summary>\n");
        builder.Append(indent.Sp4);
        builder.Append("object? IServiceProvider.GetService(Type serviceType) {\n");

        Service? service = GetNextService();
        while (service is not null) {
            builder.Append(indent.Sp8);
            builder.Append("if (serviceType == typeof(global::");
            builder.Append(service.ServiceType);
            builder.Append(')');
            builder.Append(')');
            builder.Append('\n');

            builder.Append(indent.Sp12);
            builder.Append("return ");

            Service? nextService = GetNextService();
            if (service.ServiceType != nextService?.ServiceType) {
                builder.AppendServiceGetter(service);
                builder.Append(';');
                builder.Append('\n');
            }
            else {
                builder.Append("(global::");
                builder.Append(service.ServiceType);
                builder.Append('[');
                builder.Append(']');
                builder.Append(')');

                builder.Append('[');
                builder.AppendServiceGetter(service);
                do {
                    builder.Append(',');
                    builder.Append(' ');
                    builder.AppendServiceGetter(nextService!);
                    nextService = GetNextService();
                }
                while (service.ServiceType == nextService?.ServiceType);

                builder.Append(']');
                builder.Append(';');
                builder.Append('\n');
            }

            service = nextService;
        }

        builder.Append(indent.Sp8);
        builder.Append("return null;\n");

        builder.Append(indent.Sp4);
        builder.Append('}');
        builder.Append('\n');
        builder.Append('\n');
        builder.Append('\n');
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

    /// <summary>
    /// Generates the Dispose/AsyncDispose methods and the corresponding disposeLists for the transient services.
    /// </summary>
    /// <param name="serviceList"></param>
    /// <param name="generateDisposeMethods"></param>
    /// <param name="threadSafe"></param>
    /// <param name="hasDisposeList"></param>
    /// <param name="hasAsyncDisposeList"></param>
    /// <param name="hasDisposeMethod"></param>
    /// <param name="hasDisposeAsyncMethod"></param>
    public readonly void AppendDisposeMethods(bool hasDisposeList, bool hasAsyncDisposeList) {
        if (generateDisposeMethods == DisposeGeneration.NoDisposing)
            return;

        // disposeList
        if (hasDisposeList) {
            builder.Append(indent.Sp4);
            builder.Append("private global::System.Collections.Generic.List<IDisposable> disposeList = [];\n");
            builder.Append('\n');
        }

        // asyncDisposeList
        if (hasAsyncDisposeList) {
            builder.Append(indent.Sp4);
            builder.Append("private global::System.Collections.Generic.List<IAsyncDisposable> asyncDisposeList = [];\n");
            builder.Append('\n');
        }

        uint singeltonDisposablesCount = 0;
        uint singeltonAsyncDisposablesCount = 0;

        // Dispose()
        if (generateDisposeMethods.HasFlag(DisposeGeneration.Dispose)) {
            if (!hasDisposeMethod) {
                builder.Append(indent.Sp4);
                builder.Append("/// <summary>\n");
                builder.Append(indent.Sp4);
                builder.Append("/// Disposes all disposable services instantiated by this provider.\n");
                builder.Append(indent.Sp4);
                builder.Append("/// </summary>\n");
                builder.Append(indent.Sp4);
                builder.Append("public void Dispose() {\n");
            }
            else {
                builder.Append(indent.Sp4);
                builder.Append("/// <summary>\n");
                builder.Append(indent.Sp4);
                builder.Append("/// Disposes all disposable services instantiated by this provider. Should be called inside the Dispose() method.\n");
                builder.Append(indent.Sp4);
                builder.Append("/// </summary>\n");
                builder.Append(indent.Sp4);
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
                    builder.Append(indent.Sp8);
                    builder.Append("_ = ");
                    builder.Append('(');
                    if (service.CreationTime == CreationTiming.Constructor) {
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
            builder.Append(indent.Sp4);
            builder.Append('}');
            builder.Append('\n');
            builder.Append('\n');
        }

        // DisposeAsync()
        if (generateDisposeMethods.HasFlag(DisposeGeneration.DisposeAsync)) {
            if (!hasDisposeAsyncMethod) {
                builder.Append(indent.Sp4);
                builder.Append("/// <summary>\n");
                builder.Append(indent.Sp4);
                builder.Append("/// Disposes all disposable services instantiated by this provider asynchronously.\n");
                builder.Append(indent.Sp4);
                builder.Append("/// </summary>\n");
                builder.Append(indent.Sp4);
                builder.Append("public ValueTask DisposeAsync() {\n");
            }
            else {
                builder.Append(indent.Sp4);
                builder.Append("/// <summary>\n");
                builder.Append(indent.Sp4);
                builder.Append("/// Disposes all disposable services instantiated by this provider asynchronously. Should be called inside the DisposeAsync() method.\n");
                builder.Append(indent.Sp4);
                builder.Append("/// </summary>\n");
                builder.Append(indent.Sp4);
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

                    builder.Append(indent.Sp8);
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

                    builder.Append(indent.Sp8);
                    builder.Append("return ");
                    builder.Append('(');
                    if (asyncDisposableService.CreationTime == CreationTiming.Constructor) {
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

                    builder.Append(indent.Sp8);
                    builder.Append("Task[] disposeTasks = new Task[asyncDisposeList.Count];\n");
                    builder.Append('\n');

                    builder.Append(indent.Sp8);
                    builder.Append("int index = 0;\n");
                    AppendDisposingAsyncDisposeListArray();

                    builder.Append(indent.Sp8);
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

                    builder.Append(indent.Sp8);
                    builder.Append("Task[] disposeTasks = new Task[");
                    builder.Append(singeltonAsyncDisposablesCount);
                    builder.Append("];\n");
                    builder.Append('\n');

                    int index = 0;
                    foreach (Service service in serviceList)
                        if (service.IsAsyncDisposable)
                            AppendDisposeAsyncArray(service, index++);
                    builder.Append('\n');

                    builder.Append(indent.Sp8);
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

                    builder.Append(indent.Sp8);
                    builder.Append("Task[] disposeTasks = new Task[");
                    builder.Append(singeltonAsyncDisposablesCount);
                    builder.Append(" + asyncDisposeList.Count");
                    builder.Append("];\n");
                    builder.Append('\n');

                    int index = 0;
                    foreach (Service service in serviceList)
                        if (service.IsAsyncDisposable)
                            AppendDisposeAsyncArray(service, index++);
                    builder.Append('\n');

                    builder.Append(indent.Sp8);
                    builder.Append("int index = ");
                    builder.Append(singeltonAsyncDisposablesCount);
                    builder.Append(';');
                    builder.Append('\n');
                    AppendDisposingAsyncDisposeListArray();

                    builder.Append(indent.Sp8);
                    builder.Append("return new ValueTask(Task.WhenAll(disposeTasks));\n");
                    break;
                }
            }

            builder.Append(indent.Sp4);
            builder.Append('}');
            builder.Append('\n');
            builder.Append('\n');
        }

        builder.Append('\n');
    }


    private readonly void AppendDispose(Service service) {
        builder.Append(indent.Sp8);
        builder.Append('(');
        if (service.CreationTime == CreationTiming.Constructor) {
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
        builder.Append(indent.Sp8);
        builder.Append("disposeTasks[");
        builder.Append(index);
        builder.Append("] = ");
        builder.Append('(');
        if (service.CreationTime == CreationTiming.Constructor) {
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
        string sp8 = indent.Sp8;
        string sp12 = indent.Sp12;
        if (threadSafe) {
            builder.Append(indent.Sp8);
            builder.Append("lock (disposeList)\n");
            sp8 = indent.Sp12;
            sp12 = indent.Sp16;
        }

        builder.Append(sp8);
        builder.Append("foreach (IDisposable disposable in disposeList)\n");
        builder.Append(sp12);
        builder.Append("disposable.Dispose();\n");
        builder.Append('\n');
    }

    private readonly void AppendDisposingAsyncDisposeListDiscard() {
        string sp8 = indent.Sp8;
        string sp12 = indent.Sp12;
        string sp16 = indent.Sp16;
        if (threadSafe) {
            builder.Append(indent.Sp8);
            builder.Append("lock (asyncDisposeList)\n");
            sp8 = indent.Sp12;
            sp12 = indent.Sp16;
            sp16 = indent.Sp20;
        }

        builder.Append(sp8);
        builder.Append("foreach (IAsyncDisposable asyncDisposable in asyncDisposeList)\n");
        builder.Append(sp12);
        builder.Append("if (asyncDisposable is IDisposable disposable)\n");
        builder.Append(sp16);
        builder.Append("disposable.Dispose();\n");
        builder.Append(sp12);
        builder.Append("else\n");
        builder.Append(sp16);
        builder.Append("_ = asyncDisposable.DisposeAsync().Preserve();\n");
        builder.Append('\n');
    }

    private readonly void AppendDisposingAsyncDisposeListArray() {
        string sp8 = indent.Sp8;
        string sp12 = indent.Sp12;
        if (threadSafe) {
            builder.Append(indent.Sp8);
            builder.Append("lock (asyncDisposeList)\n");
            sp8 = indent.Sp12;
            sp12 = indent.Sp16;
        }

        builder.Append(sp8);
        builder.Append("foreach (IAsyncDisposable asyncDisposable in asyncDisposeList)\n");
        builder.Append(sp12);
        builder.Append("disposeTasks[index++] = asyncDisposable.DisposeAsync().AsTask();\n");
        builder.Append('\n');
    }

    #endregion


    #region UnsafeAccessor

    /// <summary>
    /// Generates the [UnsafeAccessor]-methods to access init-only circle dependencies.
    /// </summary>
    /// <param name="serviceList"></param>
    public readonly void AppendUnsafeAccessorMethods() {
        int currentPosition = builder.Length;

        foreach (Service service in serviceList)
            foreach (PropertyDependency dependency in service.PropertyDependencyList)
                if (dependency.IsCircular && dependency.IsInit) {
                    builder.Append(indent.Sp4);
                    builder.Append("[System.Runtime.CompilerServices.UnsafeAccessor(System.Runtime.CompilerServices.UnsafeAccessorKind.Method, Name = \"set_");
                    builder.Append(dependency.Name);
                    builder.Append("\")]\n");

                    builder.Append(indent.Sp4);
                    builder.Append("private extern static void Set_");
                    builder.Append(service.Name);
                    builder.Append('_');
                    builder.Append(dependency.Name);
                    builder.Append("(global::");
                    builder.Append(service.ImplementationType);
                    builder.Append(" instance, global::");
                    builder.Append(dependency.Service!.ServiceType);
                    builder.Append(" value);\n\n");
                }

        if (builder.Length > currentPosition)
            builder.Append('\n');
    }

    #endregion


    /// <summary>
    /// <para>Appends "(service1, service2, ..., serviceN)"</para>
    /// <para>If <see cref="Service.ConstructorDependencyList"/> is empty, only "()" is appended.</para>
    /// </summary>
    /// <param name="service"></param>
    public readonly void AppendConstructorDependencyList(Service service) {
        builder.Append('(');
        if (service.ConstructorDependencyList.Length > 0) {
            foreach (ConstructorDependency dependency in service.ConstructorDependencyList) {
                if (dependency.Service!.IsRefable && !keyword.HasFlag(ClassStructKeyword.Struct))
                    builder.Append(dependency.ByRef.AsString());
                builder.AppendServiceGetter(dependency.Service!);
                builder.Append(',');
                builder.Append(' ');
            }
            builder.Length -= 2;
        }
        builder.Append(')');
    }

    /// <summary>
    /// <para>Appends " { name1 = service1, name2 = service2, ..., nameN = serviceN }"</para>
    /// <para>In case of a circular dependency, "name1 = default!" is appended and the dependency is added to the given circularDependencies list.</para>
    /// <para>If <see cref="Service.PropertyDependencyList"/> is empty, nothing is appended.</para>
    /// </summary>
    /// <param name="service"></param>
    /// <param name="circularDependencies"></param>
    /// <param name="indentation"></param>
    public readonly void AppendPropertyDependencyList(Service service, List<(Service, PropertyDependency)> circularDependencies, string indentation) {
        if (service.PropertyDependencyList.Count > 0) {
            builder.Append(' ');
            builder.Append('{');

            int builderLength = builder.Length;
            foreach (PropertyDependency dependency in service.PropertyDependencyList) {
                if (dependency.IsCircular) {
                    if (dependency.IsRequired) {
                        builder.Append('\n');
                        builder.Append(indentation);
                        builder.Append(Indent.SP4);
                        builder.Append(dependency.Name);
                        builder.Append(" = default!,");
                    }
                    circularDependencies.Add((service, dependency));
                }
                else {
                    builder.Append('\n');
                    builder.Append(indentation);
                    builder.Append(Indent.SP4);
                    builder.Append(dependency.Name);
                    builder.Append(' ');
                    builder.Append('=');
                    builder.Append(' ');
                    builder.AppendServiceGetter(dependency.Service!);
                    builder.Append(',');
                }
            }

            if (builderLength != builder.Length) {
                // at least one item got appended
                builder.Length--; // remove ','
                builder.Append('\n');
                builder.Append(indentation);
                builder.Append('}');
            }
            else
                // nothing got appended
                builder.Length -= 2; // remove " {"
        }
    }

    /// <summary>
    /// <para>Appends foreach circularDependency:<br/>
    /// "Set_{serviceName}_{dependencyName}({serviceName}, {dependencyService})}" if init-accessor<br/>
    /// or<br/>
    /// "{serviceName}.{dependencyName} = {dependencyService}" if get-accessor
    /// </para>
    /// <para>If the given list is empty, nothing is appended.</para>
    /// </summary>
    /// <param name="circularDependencies"></param>
    /// /// <param name="indentation"></param>
    public readonly void AppendCircularDependencies(List<(Service, PropertyDependency)> circularDependencies, string indentation) {
        if (circularDependencies.Count > 0) {
            builder.Append('\n');

            foreach ((Service service, PropertyDependency dependency) in circularDependencies) {
                builder.Append(indentation);
                if (dependency.IsInit) {
                    builder.Append("Set_");
                    builder.Append(service.Name);
                    builder.Append('_');
                    builder.Append(dependency.Name);
                    builder.Append('(');
                    builder.AppendServiceField(service);
                    builder.Append(',');
                    builder.Append(' ');
                    builder.AppendServiceGetter(dependency.Service!);
                    builder.Append(')');
                }
                else {
                    builder.AppendServiceField(service);
                    builder.Append('.');
                    builder.Append(dependency.Name);
                    builder.Append(" = ");
                    builder.AppendServiceGetter(dependency.Service!);
                }
                builder.Append(';');
                builder.Append('\n');
            }
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
    /// <param name="generateDisposeMethods"></param>
    /// <param name="threadSafe"></param>
    public readonly void AppendClassSummary() {
        builder.Append(indent.Sp0);
        builder.Append("/// <summary>\n");


        builder.Append(indent.Sp0);
        builder.Append("/// <para>\n");

        builder.Append(indent.Sp0);
        builder.Append("/// Number of services registered: ");
        builder.Append(serviceProvider.SortedServiceList.Count);
        builder.Append("<br />\n");

        builder.Append(indent.Sp0);
        builder.Append("/// - Singleton: ");
        builder.Append(serviceProvider.SingletonList.Count);
        builder.Append("<br />\n");

        builder.Append(indent.Sp0);
        builder.Append("/// - Scoped: ");
        builder.Append(serviceProvider.ScopedList.Count);
        builder.Append("<br />\n");

        builder.Append(indent.Sp0);
        builder.Append("/// - Transient: ");
        builder.Append(serviceProvider.TransientList.Count);
        builder.Append("<br />\n");

        builder.Append(indent.Sp0);
        builder.Append("/// - Delegate: ");
        builder.Append(serviceProvider.DelegateList.Count);
        builder.Append('\n');

        builder.Append(indent.Sp0);
        builder.Append("/// </para>\n");


        builder.Append(indent.Sp0);
        builder.Append("/// <para>\n");

        builder.Append(indent.Sp0);
        builder.Append(serviceProvider.GenerateScope switch {
            true => "/// This provider can create a scope,",
            false => "/// This provider has no scope,"
        });
        builder.Append("<br />\n");

        builder.Append(indent.Sp0);
        builder.Append(generateDisposeMethods switch {
            DisposeGeneration.NoDisposing => "/// implements no Dispose methods",
            DisposeGeneration.Dispose => "/// implements only synchronous Dispose() method",
            DisposeGeneration.DisposeAsync => "/// implements only asynchronous DisposeAsync() method",
            DisposeGeneration.GenerateBoth => "/// implements both Dispose() and DisposeAsync() methods",
            _ => throw new Exception($"Invalid enum DisposeGeneration: {generateDisposeMethods}")
        });
        builder.Append("<br />\n");

        builder.Append(indent.Sp0);
        builder.Append(threadSafe switch {
            true => "/// and is thread safe.",
            false => "/// and is not thread safe."
        });
        builder.Append('\n');

        builder.Append(indent.Sp0);
        builder.Append("/// </para>\n");


        builder.Append(indent.Sp0);
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
        builder.Append(indent.Sp4);
        builder.Append("/// <summary>\n");

        builder.Append(indent.Sp4);
        builder.Append("/// Lifetime: <see cref=\"global::CircleDIAttributes.");
        builder.Append(service.Lifetime.AsString());
        builder.Append("Attribute{TService}\">");
        builder.Append(service.Lifetime.AsString());
        builder.Append("</see><br />\n");

        builder.Append(indent.Sp4);
        builder.Append("/// Service type: <see cref=\"global::");
        builder.Append(service.ServiceType);
        builder.Append("\"/><br />\n");

        builder.Append(indent.Sp4);
        builder.Append("/// Implementation type:  <see cref=\"global::");
        builder.Append(service.ImplementationType);
        builder.Append("\"/>\n");

        builder.Append(indent.Sp4);
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
        builder.Append(indent.Sp4);
        builder.Append("/// <summary>\n");

        builder.Append(indent.Sp4);
        builder.Append("/// Creates an instance of <see cref=\"global::");
        builder.AppendNamespaceList(serviceProvider.NameSpaceList);
        builder.AppendContainingTypeList(serviceProvider.ContainingTypeList);
        builder.Append(serviceProvider.Name);
        builder.Append(".Scope\"/> together with all <see cref=\"global::CircleDIAttributes.CreationTiming.Constructor\">non-lazy</see> scoped services.\n");

        builder.Append(indent.Sp4);
        builder.Append("/// </summary>\n");
    }
}