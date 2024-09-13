using CircleDI.Defenitions;
using CircleDI.Extensions;

namespace CircleDI.Generation;

/// <summary>
/// <para>ServiceTreeBuilder</para>
/// <para>
/// Contains the logic for traversing the service tree (The tree is created at <see cref="ServiceProvider.DependencyTreeInitializer.InitNodeRecursion"/> to initialize the services in the right order.<br />
/// It is used for building the initialitation for the constructor, for the CreateScope() method and for the getter of lazy services (includes also Transients).
/// </para>
/// </summary>
public partial struct CircleDIBuilderCore {
    /** struct public - file private
     * Only these methods below should be called outside of this file,
     * all other members (fields and methods) are private to this file. 
     **/
    #region public

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
            AppendDiposeStackConstructor(transientDisposeStack, DISPOSE_LIST);
        if (hasAsyncDisposeList)
            AppendDiposeStackConstructor(transientAsyncDisposeStack, ASYNC_DISPOSE_LIST);
    }

    /// <summary>
    /// Special method CreateScope()
    /// </summary>
    private void AppendCreateScopeServiceTree() {
        transientNumber = 0;
        serviceProvider.NextDependencyTreeFlag();
        Service service = serviceProvider.CreateScope!;

        Span<int> transientNumberList = stackalloc int[service.ConstructorDependencyList.Count + service.PropertyDependencyList.Count];
        Span<int> transientNumberConstructor = transientNumberList[..service.ConstructorDependencyList.Count];
        Span<int> transientNumberProperty = transientNumberList[service.ConstructorDependencyList.Count..];
        for (int i = 0; i < service.ConstructorDependencyList.Count; i++) {
            ConstructorDependency dependency = service.ConstructorDependencyList[i];
            if (dependency.HasAttribute)
                transientNumberConstructor[i] = AppendLazyServiceTreeRecursion(dependency.Service!, 0, 0, 0);
        }
        for (int i = 0; i < service.PropertyDependencyList.Count; i++) {
            PropertyDependency dependency = service.PropertyDependencyList[i];
            if (dependency.HasAttribute)
                transientNumberProperty[i] = AppendLazyServiceTreeRecursion(dependency.Service!, 0, 0, 0);
        }

        AppendDisposeStackLazy(transientDisposeStack, 0, DISPOSE_LIST, appendServiceProviderField: false);
        AppendDisposeStackLazy(transientAsyncDisposeStack, 0, ASYNC_DISPOSE_LIST, appendServiceProviderField: false);

        builder.AppendInterpolation($"{indent}return new global::{serviceProvider.IdentifierScope.AsOpenFullyQualified()}");
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
        serviceProvider.NextDependencyTreeFlag();

        AppendLazyServiceTreeRecursion(service, 0, 0, 0);
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
        serviceProvider.NextDependencyTreeFlag();

        int currentTransientNumber = AppendLazyServiceTreeRecursion(service, 0, 0, 0);

        AppendDisposeStackLazy(transientDisposeStack, 0, DISPOSE_LIST, appendServiceProviderField: false);
        AppendDisposeStackLazy(transientAsyncDisposeStack, 0, ASYNC_DISPOSE_LIST, appendServiceProviderField: false);

        return currentTransientNumber;
    }

    #endregion


    /// <summary>
    /// <para>Contains all circular dependencies where dependency is initialized before service.</para>
    /// <para>
    /// Segmented into stackframes where the start of the current stackframe is the local variable 'circularStackIndex'.<br />
    /// Constructor service tree has only 1 segment/stackframe.
    /// </para>
    /// </summary>
    private readonly List<(Service service, int number, PropertyDependency dependency)> circularStack = [];

    /// <summary>
    /// <para>Contains all circular dependencies where dependency is not initialized when traversing service.</para>
    /// <para>It is only used in lazy service tree.</para>
    /// </summary>
    private readonly List<(Service service, int number, PropertyDependency dependency)> circularNotInitList = [];


    /// <summary>
    /// <para>Contains all transient services that are instantiated and implementing dispose and not asyncDispose.</para>
    /// <para>
    /// Segmented into stackframes where the start of the current stackframe is the local variable 'disposeStackIndex'.<br />
    /// Constructor service tree has only 1 segment/stackframe.
    /// </para>
    /// </summary>
    private readonly List<(Service service, int number)> transientDisposeStack = [];

    /// <summary>
    /// <para>Contains all transient services that are instantiated and implementing asyncDispose.</para>
    /// <para>
    /// Segmented into stackframes where the start of the current stackframe is the local variable 'asyncDisposeStackIndex'.<br />
    /// Constructor service tree has only 1 segment/stackframe.
    /// </para>
    /// </summary>
    private readonly List<(Service service, int number)> transientAsyncDisposeStack = [];


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
    /// Tree recursion for building content for constructor.
    /// </summary>
    /// <param name="service"></param>
    /// <returns></returns>
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
                if (dependency.Service.CreationTimeTransitive is CreationTiming.Lazy)
                    transientNumberConstructor[i] = AppendLazyServiceTreeRecursion(dependency.Service!, circularStack.Count, transientDisposeStack.Count, transientAsyncDisposeStack.Count);
            }
            else
                transientNumberConstructor[i] = AppendCounstructorServiceTreeRecursion(dependency.Service!);
        }
        for (int i = 0; i < service.PropertyDependencyList.Count; i++) {
            PropertyDependency dependency = service.PropertyDependencyList[i];
            if (!dependency.IsCircular)
                if (isScopeProvider && dependency.Service!.Lifetime is ServiceLifetime.Singleton) {
                    if (dependency.Service.CreationTimeTransitive is CreationTiming.Lazy)
                        transientNumberProperty[i] = AppendLazyServiceTreeRecursion(dependency.Service!, circularStack.Count, transientDisposeStack.Count, transientAsyncDisposeStack.Count);
                }
                else
                    transientNumberProperty[i] = AppendCounstructorServiceTreeRecursion(dependency.Service!);
            else
                circularStack.Add((service, transientNumber, dependency));
        }

        service.TreeState.init = serviceProvider.DependencyTreeFlag;
        return AppendServiceCreation(service, transientNumberConstructor, transientNumberProperty);
    }

    /// <summary>
    /// Tree recursion for building content for lazy initialized services (including Transient services).
    /// </summary>
    /// <param name="service"></param>
    /// <param name="circularStackIndex"></param>
    /// <returns></returns>
    private int AppendLazyServiceTreeRecursion(Service service, int circularStackIndex, int disposeStackIndex, int asyncDisposeStackIndex) {
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

                if (!isScopeProvider) {
                    if (!service.IsValueType)
                        builder.AppendInterpolation($"{indent}if (_{service.Name.AsFirstLower()} is null)\n");
                    else
                        builder.AppendInterpolation($"{indent}if (!_{service.Name.AsFirstLower()}_hasValue)\n");
                    indent.IncreaseLevel(); // (+1)
                    builder.AppendInterpolation($"{indent}lock (this)\n");
                    indent.IncreaseLevel(); // (+2)
                }
                else {
                    if (!service.IsValueType)
                        builder.AppendInterpolation($"{indent}if (_{serviceProvider.Identifier.Name.AsFirstLower()}._{service.Name.AsFirstLower()} is null)\n");
                    else
                        builder.AppendInterpolation($"{indent}if (!_{serviceProvider.Identifier.Name.AsFirstLower()}._{service.Name.AsFirstLower()}_hasValue)\n");
                    indent.IncreaseLevel(); // (+1)
                    builder.AppendInterpolation($"{indent}lock (_{serviceProvider.Identifier.Name.AsFirstLower()})\n");
                    indent.IncreaseLevel(); // (+2)
                }
            }


            if (!isScopeProvider)
                if (!service.IsValueType)
                    builder.AppendInterpolation($"{indent}if (_{service.Name.AsFirstLower()} is null) {{\n");
                else
                    builder.AppendInterpolation($"{indent}if (!_{service.Name.AsFirstLower()}_hasValue) {{\n");
            else
                if (!service.IsValueType)
                    builder.AppendInterpolation($"{indent}if (_{serviceProvider.Identifier.Name.AsFirstLower()}._{service.Name.AsFirstLower()} is null) {{\n");
                else
                    builder.AppendInterpolation($"{indent}if (!_{serviceProvider.Identifier.Name.AsFirstLower()}._{service.Name.AsFirstLower()}_hasValue) {{\n");
            indent.IncreaseLevel(); // +1
        }
        else if (service.Lifetime is ServiceLifetime.Scoped) {
            if (serviceProvider.ThreadSafeScope && !isLockedScope) {
                isLockedScope = true;
                lockedOnThisLevel = true;

                if (!service.IsValueType)
                    builder.AppendInterpolation($"{indent}if (_{service.Name.AsFirstLower()} is null)\n");
                else
                    builder.AppendInterpolation($"{indent}if (!_{service.Name.AsFirstLower()}_hasValue)\n");
                indent.IncreaseLevel(); // (+1)

                builder.AppendInterpolation($"{indent}lock (this)\n");
                indent.IncreaseLevel(); // (+2)
            }

            if (!service.IsValueType)
                builder.AppendInterpolation($"{indent}if (_{service.Name.AsFirstLower()} is null) {{\n");
            else
                builder.AppendInterpolation($"{indent}if (!_{service.Name.AsFirstLower()}_hasValue) {{\n");
            indent.IncreaseLevel(); // +1
        }


        Span<int> transientNumberList = stackalloc int[service.ConstructorDependencyList.Count + service.PropertyDependencyList.Count];
        Span<int> transientNumberConstructor = transientNumberList[..service.ConstructorDependencyList.Count];
        Span<int> transientNumberProperty = transientNumberList[service.ConstructorDependencyList.Count..];
        for (int i = 0; i < service.ConstructorDependencyList.Count; i++) {
            ConstructorDependency dependency = service.ConstructorDependencyList[i];
            transientNumberConstructor[i] = AppendLazyServiceTreeRecursion(dependency.Service!, circularStack.Count, transientDisposeStack.Count, transientAsyncDisposeStack.Count);
        }

        int circularNotInitDependencyListIndex = circularNotInitList.Count;
        for (int i = 0; i < service.PropertyDependencyList.Count; i++) {
            PropertyDependency dependency = service.PropertyDependencyList[i];
            if (!dependency.IsCircular)
                transientNumberProperty[i] = AppendLazyServiceTreeRecursion(dependency.Service!, circularStack.Count, transientDisposeStack.Count, transientAsyncDisposeStack.Count);
            else
                if (dependency.Service!.CreationTimeTransitive is CreationTiming.Constructor || dependency.Service.TreeState.init.HasFlag(serviceProvider.DependencyTreeFlag))
                    circularStack.Add((service, transientNumber, dependency));
                else
                    circularNotInitList.Add((service, transientNumber, dependency));
        }

        int currentTransientNumber = AppendServiceCreation(service, transientNumberConstructor, transientNumberProperty);

        // init all circularDependency services - must be a for loop -> while iterating collection get modified, but Count stays the same
        {
            List<(Service service, int number, PropertyDependency dependency)> currentList = circularStack;
            int startIndex = circularStackIndex;
            for (int listCount = 0; listCount < 2; listCount++) {
                for (int i = startIndex; i < currentList.Count; i++)
                    AppendLazyServiceTreeRecursion(currentList[i].dependency.Service!, circularStack.Count, transientDisposeStack.Count, transientAsyncDisposeStack.Count);
                currentList = circularNotInitList;
                startIndex = circularNotInitDependencyListIndex;
            }
        }

        service.TreeState.init = serviceProvider.DependencyTreeFlag;
        AppendCircularDependencyList(circularStackIndex);
        AppendCircularNotInitDependencyList(service);


        if (service.Lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped) {
            bool appendServiceProviderField = isScopeProvider && service.Lifetime is ServiceLifetime.Singleton;
            AppendDisposeStackLazy(transientDisposeStack, disposeStackIndex, DISPOSE_LIST, appendServiceProviderField);
            AppendDisposeStackLazy(transientAsyncDisposeStack, asyncDisposeStackIndex, ASYNC_DISPOSE_LIST, appendServiceProviderField);

            indent.DecreaseLevel(); // +0
            builder.AppendInterpolation($"{indent}}}\n");

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

        if (dependency.IsInit)
            builder.AppendInterpolation($"Set_{service.Name}_{dependency.Name}(");

        switch (service.Lifetime) {
            case ServiceLifetime.Singleton:
                if (isScopeProvider)
                    builder.AppendInterpolation($"_{serviceProvider.Identifier.Name.AsFirstLower()}.");
                goto case ServiceLifetime.Scoped;
            case ServiceLifetime.Scoped:
                builder.AppendServiceField(service);
                break;
            default:
                builder.AppendFirstLower(service.Name);
                if (number > 0)
                    builder.AppendInterpolation($"_{number}");
                break;
        }

        if (dependency.IsInit) {
            builder.Append(", ");
            AppendDependencyIdentifier(dependency, number);
            builder.Append(')');
        }
        else {
            builder.AppendInterpolation($".{dependency.Name} = ");
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
    private void AppendDiposeStackConstructor(List<(Service service, int number)> disposeList, string disposeListName) {
        switch (disposeList.Count) {
            case 0:
                builder.AppendInterpolation($"{indent}{disposeListName} = [];\n");
                break;
            case 1:
                if (disposeList[0].number > 0)
                    builder.AppendInterpolation($"{indent}{disposeListName} = [{disposeList[0].service.Name.AsFirstLower()}_{disposeList[0].number}];\n");
                else
                    builder.AppendInterpolation($"{indent}{disposeListName} = [{disposeList[0].service.Name.AsFirstLower()}];\n");
                break;
            default:
                builder.AppendInterpolation($"{indent}{disposeListName} = [\n");
                indent.IncreaseLevel(); // +1

                foreach ((Service service, int number) in disposeList)
                    if (number > 0)
                        builder.AppendInterpolation($"{indent}{service.Name.AsFirstLower()}_{number},\n");
                    else
                        builder.AppendInterpolation($"{indent}{service.Name.AsFirstLower()},\n");

                indent.DecreaseLevel(); // +0
                builder.Length -= 2; // remove ",\n"
                builder.AppendInterpolation($"\n{indent}];\n");
                break;
        }

        disposeList.Clear();
    }

    /// <summary>
    /// <para>Appends for each disposable transient service an add statement to the corresponding disposeList.</para>
    /// <para>DisposeList is appended first, then asyncDisposeList, but if a service implements both, it is added to asyncDisposeList.</para>
    /// <para>The list is cleared afterwards.</para>
    /// </summary>
    /// <param name="disposeStack"></param>
    /// <param name="disposeStackName"></param>
    /// <param name="appendServiceProviderField"></param>
    private void AppendDisposeStackLazy(List<(Service service, int number)> disposeStack, int disposeStackIndex, string disposeStackName, bool appendServiceProviderField) {
        if (disposeStack.Count == 0)
            return;

        if (threadSafe) {
            if (appendServiceProviderField)
                builder.AppendInterpolation($"{indent}lock (_{serviceProvider.Identifier.Name.AsFirstLower()}.{disposeStackName}) {{\n");
            else
                builder.AppendInterpolation($"{indent}lock ({disposeStackName}) {{\n");
            indent.IncreaseLevel(); // +1
        }

        for (int i = disposeStackIndex; i < disposeStack.Count; i++) {
            (Service service, int number) = disposeStack[i];
            if (appendServiceProviderField)
                if (number > 0)
                    builder.AppendInterpolation($"{indent}_{serviceProvider.Identifier.Name.AsFirstLower()}.{disposeStackName}.Add({service.Name.AsFirstLower()}_{number});\n");
                else
                    builder.AppendInterpolation($"{indent}_{serviceProvider.Identifier.Name.AsFirstLower()}.{disposeStackName}.Add({service.Name.AsFirstLower()});\n");
            else
                if (number > 0)
                    builder.AppendInterpolation($"{indent}{disposeStackName}.Add({service.Name.AsFirstLower()}_{number});\n");
                else
                    builder.AppendInterpolation($"{indent}{disposeStackName}.Add({service.Name.AsFirstLower()});\n");
        }

        if (threadSafe) {
            indent.DecreaseLevel(); // +0
            builder.AppendInterpolation($"{indent}}}\n");
        }

        disposeStack.RemoveRange(disposeStackIndex, disposeStack.Count - disposeStackIndex);
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
        if (service.Lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped)
            if (isScopeProvider && service.Lifetime is ServiceLifetime.Singleton)
                builder.AppendInterpolation($"{indent}_{serviceProvider.Identifier.Name.AsFirstLower()}.{service.AsServiceField()} = ");
            else
                builder.AppendInterpolation($"{indent}{service.AsServiceField()} = ");
        // ServiceLifetime.Transient
        else
            if (transientNumber > 0)
                builder.AppendInterpolation($"{indent}global::{service.ImplementationType.AsClosedFullyQualified()} {service.Name.AsFirstLower()}_{transientNumber} = ");
            else
                builder.AppendInterpolation($"{indent}global::{service.ImplementationType.AsClosedFullyQualified()} {service.Name.AsFirstLower()} = ");


        // constructor
        if (service.Implementation.Type == MemberType.None) {
            builder.AppendInterpolation($"new global::{service.ImplementationType.AsClosedFullyQualified()}");
            AppendConstructorDependencyList(service, transientNumberConstructor);
            AppendPropertyDependencyList(service, transientNumberProperty);
        }
        // implementation field, property or method
        else {
            if (isScopeProvider && !service.Implementation.IsScoped && !service.Implementation.IsStatic)
                builder.AppendInterpolation($"_{serviceProvider.Identifier.Name.AsFirstLower()}.{service.AsImplementationName()}");
            else
                builder.AppendImplementationName(service);

            if (service.IsGeneric)
                builder.AppendClosedGenerics(service.ImplementationType);
            if (service.Implementation.Type == MemberType.Method)
                AppendConstructorDependencyList(service, transientNumberConstructor);
        }
        builder.Append(";\n");


        if (service.Lifetime is ServiceLifetime.Singleton or ServiceLifetime.Scoped) {
            if (service.IsValueType && service.CreationTimeTransitive is CreationTiming.Lazy)
                builder.AppendInterpolation($"{indent}_{service.Name.AsFirstLower()}_hasValue = true;\n");
            return 0;
        }
        else {
            if (generateDisposeMethods is not DisposeGeneration.NoDisposing)
                if (service.IsAsyncDisposable)
                    transientAsyncDisposeStack.Add((service, transientNumber));
                else if (service.IsDisposable)
                    transientDisposeStack.Add((service, transientNumber));

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

            builder.Length -= 2; // remove ", "
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

                if (!dependency.HasAttribute) {
                    builder.AppendFirstLower(dependency.Name);
                }
                else {
                    if (dependency.Service!.IsRefable && !keyword.HasFlag(TypeKeyword.Struct))
                        builder.Append(dependency.ByRef.AsString());
                    AppendDependencyIdentifier(dependency, numberList[i]);
                }

                builder.Append(", ");
            }

            builder.Length -= 2; // remove ", "
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
                        builder.AppendInterpolation($"\n{indent}{dependency.Name} = ");
                        if (dependency.Service!.CreationTimeTransitive is CreationTiming.Lazy)
                            AppendDependencyIdentifier(dependency, numberList[i]);
                        else
                            builder.Append("default!");
                        builder.Append(',');
                    }
                }
                else {
                    builder.AppendInterpolation($"\n{indent}{dependency.Name} = ");
                    AppendDependencyIdentifier(dependency, numberList[i]);
                    builder.Append(',');
                }
            }
            indent.DecreaseLevel(); // +0

            // at least one item got appended
            if (builderLength != builder.Length) {
                builder.Length--; // remove last ','
                builder.AppendInterpolation($"\n{indent}}}");
            }
            // nothing got appended
            else
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
                if (!dependency.HasAttribute)
                    builder.AppendInterpolation($"\n{indent}{dependency.Name} = {dependency.Name.AsFirstLower()},");
                else {
                    builder.AppendInterpolation($"\n{indent}{dependency.Name} = ");
                    AppendDependencyIdentifier(dependency, numberList[i]);
                    builder.Append(',');
                }
            }

            indent.DecreaseLevel(); // +0
            builder.Length--; // remove last ','
            builder.AppendInterpolation($"\n{indent}}}");
        }
    }


    /// <summary>
    /// if Singleton or Scoped -> ServiceField<br />
    /// if Transient -> localVariable<br />
    /// if Delegate -> ServiceGetter
    /// </summary>+
    /// <param name="dependency"></param>
    /// <param name="transientNumber"></param>
    private void AppendDependencyIdentifier(Dependency dependency, int transientNumber)
        => _ = dependency.Service!.Lifetime switch {
            ServiceLifetime.Singleton when isScopeProvider && dependency.Service.Implementation.Name == "this" => builder.AppendInterpolation($"_{serviceProvider.Identifier.Name.AsFirstLower()}"),
            ServiceLifetime.Singleton when isScopeProvider                                                     => builder.AppendInterpolation($"_{serviceProvider.Identifier.Name.AsFirstLower()}.{dependency.Service.AsServiceField()}"),
            ServiceLifetime.Singleton or ServiceLifetime.Scoped                                                => builder.AppendServiceField(dependency.Service),
            _ when dependency.Service.Lifetime.HasFlag(ServiceLifetime.Transient) && transientNumber > 0       => builder.AppendInterpolation($"{dependency.Service.Name.AsFirstLower()}_{transientNumber}"),
            _ when dependency.Service.Lifetime.HasFlag(ServiceLifetime.Transient)                              => builder.AppendFirstLower(dependency.Service.Name),
            _ /* when dependency.Service!.Lifetime.HasFlag(ServiceLifetime.Delegate) */                        => builder.AppendServiceGetter(dependency.Service)
        };
}
