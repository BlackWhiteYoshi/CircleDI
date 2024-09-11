using CircleDI.Defenitions;
using CircleDI.Extensions;

namespace CircleDI.Generation;

/// <summary>
/// Contains the method to build the the Dispose and DisposeAsync methods, inclusive the fields holding the transient dispose list.
/// </summary>
public partial struct CircleDIBuilderCore {
    private const string DISPOSE_LIST = "_disposeList";
    private const string ASYNC_DISPOSE_LIST = "_asyncDisposeList";

    /// <summary>
    /// Generates the Dispose/AsyncDispose methods and the corresponding disposeLists for the transient services.
    /// </summary>
    public void AppendDisposeMethods() {
        if (generateDisposeMethods == DisposeGeneration.NoDisposing)
            return;


        // disposeList
        if (hasDisposeList)
            builder.AppendIndent(indent)
                .AppendInterpolation($"private {readonlyStr}global::System.Collections.Generic.List<IDisposable> {DISPOSE_LIST};\n\n");

        // asyncDisposeList
        if (hasAsyncDisposeList)
            builder.AppendIndent(indent)
                .AppendInterpolation($"private {readonlyStr}global::System.Collections.Generic.List<IAsyncDisposable> {ASYNC_DISPOSE_LIST};\n\n");


        uint singeltonDisposablesCount = 0;
        uint singeltonAsyncDisposablesCount = 0;

        // Dispose()
        if (generateDisposeMethods.HasFlag(DisposeGeneration.Dispose)) {
            if (!hasDisposeMethod) {
                builder.AppendIndent(indent)
                    .Append("/// <summary>\n");
                builder.AppendIndent(indent)
                    .Append("/// Disposes all disposable services instantiated by this provider.\n");
                builder.AppendIndent(indent)
                    .Append("/// </summary>\n");
                builder.AppendIndent(indent)
                    .Append("public void Dispose() {\n");
            }
            else {
                builder.AppendIndent(indent)
                    .Append("/// <summary>\n");
                builder.AppendIndent(indent)
                    .Append("/// Disposes all disposable services instantiated by this provider. Should be called inside the Dispose() method.\n");
                builder.AppendIndent(indent)
                    .Append("/// </summary>\n");
                builder.AppendIndent(indent)
                    .Append("private void DisposeServices() {\n");
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
                    builder.AppendIndent(indent)
                        .Append("_ = (");
                    if (service.CreationTimeTransitive == CreationTiming.Constructor)
                        builder.Append("(IAsyncDisposable)")
                            .AppendServiceField(service)
                            .Append(')');
                    else
                        builder.AppendServiceField(service)
                            .Append(" as IAsyncDisposable)?");
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
            builder.AppendIndent(indent)
                .Append("}\n\n");
        }

        // DisposeAsync()
        if (generateDisposeMethods.HasFlag(DisposeGeneration.DisposeAsync)) {
            if (!hasDisposeAsyncMethod) {
                builder.AppendIndent(indent)
                    .Append("/// <summary>\n");
                builder.AppendIndent(indent)
                    .Append("/// Disposes all disposable services instantiated by this provider asynchronously.\n");
                builder.AppendIndent(indent)
                    .Append("/// </summary>\n");
                builder.AppendIndent(indent)
                    .Append("public ValueTask DisposeAsync() {\n");
            }
            else {
                builder.AppendIndent(indent)
                    .Append("/// <summary>\n");
                builder.AppendIndent(indent)
                    .Append("/// Disposes all disposable services instantiated by this provider asynchronously. Should be called inside the DisposeAsync() method.\n");
                builder.AppendIndent(indent)
                    .Append("/// </summary>\n");
                builder.AppendIndent(indent)
                    .Append("private ValueTask DisposeServicesAsync() {\n");
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

                    builder.AppendIndent(indent)
                        .Append("return default;\n");
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

                    builder.AppendIndent(indent)
                        .Append("return (");
                    if (asyncDisposableService.CreationTimeTransitive == CreationTiming.Constructor)
                        builder.Append("(IAsyncDisposable)")
                            .AppendServiceField(asyncDisposableService)
                            .Append(").DisposeAsync();\n");
                    else
                        builder.AppendServiceField(asyncDisposableService)
                            .Append(" as IAsyncDisposable)?.DisposeAsync() ?? default;\n");
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

                    builder.AppendIndent(indent)
                        .Append($"Task[] disposeTasks = new Task[{ASYNC_DISPOSE_LIST}.Count];\n\n");

                    builder.AppendIndent(indent)
                        .Append("int index = 0;\n");
                    AppendDisposingAsyncDisposeListArray();

                    builder.AppendIndent(indent)
                        .Append("return new ValueTask(Task.WhenAll(disposeTasks));\n");
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

                    builder.AppendIndent(indent)
                        .AppendInterpolation($"Task[] disposeTasks = new Task[{singeltonAsyncDisposablesCount}];\n\n");

                    int index = 0;
                    foreach (Service service in serviceList)
                        if (service.IsAsyncDisposable)
                            AppendDisposeAsyncArray(service, index++);
                    builder.Append('\n');

                    builder.AppendIndent(indent)
                        .Append("return new ValueTask(Task.WhenAll(disposeTasks));\n");
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

                    builder.AppendIndent(indent)
                        .AppendInterpolation($"Task[] disposeTasks = new Task[{singeltonAsyncDisposablesCount} + {ASYNC_DISPOSE_LIST}.Count];\n\n");

                    int index = 0;
                    foreach (Service service in serviceList)
                        if (service.IsAsyncDisposable)
                            AppendDisposeAsyncArray(service, index++);
                    builder.Append('\n');

                    builder.AppendIndent(indent)
                        .AppendInterpolation($"int index = {singeltonAsyncDisposablesCount};\n");
                    AppendDisposingAsyncDisposeListArray();

                    builder.AppendIndent(indent)
                        .Append("return new ValueTask(Task.WhenAll(disposeTasks));\n");
                    break;
                }
            }

            indent.DecreaseLevel(); // 1
            builder.AppendIndent(indent)
                .Append("}\n\n");
        }

        builder.Append('\n');
    }


    private void AppendDispose(Service service) {
        builder.AppendIndent(indent)
            .Append('(');
        if (service.CreationTimeTransitive == CreationTiming.Constructor)
            builder.Append("(IDisposable)")
                .AppendServiceField(service)
                .Append(')');
        else
            builder.AppendServiceField(service)
                .Append(" as IDisposable)?");
        builder.Append(".Dispose();\n");
    }

    private void AppendDisposeAsyncArray(Service service, int index) {
        builder.AppendIndent(indent)
            .AppendInterpolation($"disposeTasks[{index}] = (");
        if (service.CreationTimeTransitive == CreationTiming.Constructor)
            builder.Append("(IAsyncDisposable)")
                .AppendServiceField(service)
                .Append(").DisposeAsync().AsTask();\n");
        else
            builder.AppendServiceField(service)
                .Append(" as IAsyncDisposable)?.DisposeAsync().AsTask() ?? Task.CompletedTask;\n");
    }


    private void AppendDisposingDisposeList() {
        if (threadSafe) {
            builder.AppendIndent(indent)
                .Append($"lock ({DISPOSE_LIST})\n");
            indent.IncreaseLevel(); // 3
        }

        builder.AppendIndent(indent)
            .Append($"foreach (IDisposable disposable in {DISPOSE_LIST})\n");
        indent.IncreaseLevel(); // 3 or 4

        builder.AppendIndent(indent)
            .Append("disposable.Dispose();\n\n");
        indent.DecreaseLevel(); // 2 or 3

        if (threadSafe)
            indent.DecreaseLevel(); // 2
    }

    private void AppendDisposingAsyncDisposeListDiscard() {
        if (threadSafe) {
            builder.AppendIndent(indent)
                .Append($"lock ({ASYNC_DISPOSE_LIST})\n");
            indent.IncreaseLevel(); // 3
        }

        builder.AppendIndent(indent)
            .Append($"foreach (IAsyncDisposable asyncDisposable in {ASYNC_DISPOSE_LIST})\n");
        indent.IncreaseLevel(); // 3 or 4

        builder.AppendIndent(indent)
            .Append("if (asyncDisposable is IDisposable disposable)\n");
        indent.IncreaseLevel(); // 4 or 5

        builder.AppendIndent(indent)
            .Append("disposable.Dispose();\n");
        indent.DecreaseLevel(); // 3 or 4

        builder.AppendIndent(indent)
            .Append("else\n");
        indent.IncreaseLevel(); // 4 or 5

        builder.AppendIndent(indent)
            .Append("_ = asyncDisposable.DisposeAsync().Preserve();\n\n");
        indent.DecreaseLevel(); // 3 or 4
        indent.DecreaseLevel(); // 2 or 3

        if (threadSafe)
            indent.DecreaseLevel(); // 2
    }

    private void AppendDisposingAsyncDisposeListArray() {
        if (threadSafe) {
            builder.AppendIndent(indent)
                .Append($"lock ({ASYNC_DISPOSE_LIST})\n");
            indent.IncreaseLevel(); // 3
        }

        builder.AppendIndent(indent)
            .Append($"foreach (IAsyncDisposable asyncDisposable in {ASYNC_DISPOSE_LIST})\n");
        indent.IncreaseLevel(); // 3 or 4

        builder.AppendIndent(indent)
            .Append("disposeTasks[index++] = asyncDisposable.DisposeAsync().AsTask();\n\n");
        indent.DecreaseLevel(); // 2 or 3

        if (threadSafe)
            indent.DecreaseLevel(); // 2
    }
}
