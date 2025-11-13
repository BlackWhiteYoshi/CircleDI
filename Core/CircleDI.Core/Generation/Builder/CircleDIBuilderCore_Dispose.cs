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
        if (hasDisposeList) {
            builder.AppendInterpolation($"{indent}private {readonlyStr}global::System.Collections.Generic.List<IDisposable> {DISPOSE_LIST};\n");
            if (threadSafe)
                builder.AppendInterpolation($"{indent}private readonly global::System.Threading.Lock {DISPOSE_LIST}_lock = new();\n");
            builder.Append('\n');
        }

        // asyncDisposeList
        if (hasAsyncDisposeList) {
            builder.AppendInterpolation($"{indent}private {readonlyStr}global::System.Collections.Generic.List<IAsyncDisposable> {ASYNC_DISPOSE_LIST};\n");
            if (threadSafe)
                builder.AppendInterpolation($"{indent}private readonly global::System.Threading.Lock {ASYNC_DISPOSE_LIST}_lock = new();\n");
            builder.Append('\n');
        }


        uint singeltonDisposablesCount = 0;
        uint singeltonAsyncDisposablesCount = 0;

        // Dispose()
        if (generateDisposeMethods.HasFlag(DisposeGeneration.Dispose)) {
            if (!hasDisposeMethod)
                builder.AppendInterpolation($$"""
                    {{indent}}/// <summary>
                    {{indent}}/// Disposes all disposable services instantiated by this provider.
                    {{indent}}/// </summary>
                    {{indent}}public void Dispose() {

                    """);
            else
                builder.AppendInterpolation($$"""
                    {{indent}}/// <summary>
                    {{indent}}/// Disposes all disposable services instantiated by this provider. Should be called inside the Dispose() method.
                    {{indent}}/// </summary>
                    {{indent}}private void DisposeServices() {

                    """);
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

                    if (service.CreationTimeTransitive == CreationTiming.Constructor)
                        builder.AppendInterpolation($"{indent}_ = ((IAsyncDisposable){service.AsServiceField}).DisposeAsync().Preserve();\n");
                    else // CreationTiming.Lazy
                        if (!service.IsValueType)
                            builder.AppendInterpolation($"{indent}_ = ({service.AsServiceField} as IAsyncDisposable)?.DisposeAsync().Preserve();\n");
                        else {
                            builder.AppendInterpolation($"{indent}if ({service.AsServiceField}_hasValue)\n");
                            indent.IncreaseLevel(); // 3
                            builder.AppendInterpolation($"{indent}_ = ((IAsyncDisposable){service.AsServiceField}).DisposeAsync().Preserve();\n");
                            indent.DecreaseLevel(); // 2
                        }
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
            builder.AppendInterpolation($"{indent}}}\n\n");
        }

        // DisposeAsync()
        if (generateDisposeMethods.HasFlag(DisposeGeneration.DisposeAsync)) {
            if (!hasDisposeAsyncMethod)
                builder.AppendInterpolation($$"""
                    {{indent}}/// <summary>
                    {{indent}}/// Disposes all disposable services instantiated by this provider asynchronously.
                    {{indent}}/// </summary>
                    {{indent}}public ValueTask DisposeAsync() {

                    """);
            else
                builder.AppendInterpolation($$"""
                    {{indent}}/// <summary>
                    {{indent}}/// Disposes all disposable services instantiated by this provider asynchronously. Should be called inside the DisposeAsync() method.
                    {{indent}}/// </summary>
                    {{indent}}private ValueTask DisposeServicesAsync() {

                    """);
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

                    builder.AppendInterpolation($"{indent}return default;\n");
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

                    if (asyncDisposableService.CreationTimeTransitive == CreationTiming.Constructor)
                        builder.AppendInterpolation($"{indent}return ((IAsyncDisposable){asyncDisposableService.AsServiceField}).DisposeAsync();\n");
                    else
                        if (!asyncDisposableService.IsValueType)
                            builder.AppendInterpolation($"{indent}return ({asyncDisposableService.AsServiceField} as IAsyncDisposable)?.DisposeAsync() ?? default;\n");
                        else
                            builder.AppendInterpolation($"{indent}return _tT_hasValue ? ((IAsyncDisposable){asyncDisposableService.AsServiceField}).DisposeAsync() : default;\n");
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

                    builder.AppendInterpolation($"""
                        {indent}Task[] disposeTasks = new Task[{ASYNC_DISPOSE_LIST}.Count];

                        {indent}int index = 0;

                        """);
                    AppendDisposingAsyncDisposeListArray();

                    builder.AppendInterpolation($"{indent}return new ValueTask(Task.WhenAll(disposeTasks));\n");
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

                    builder.AppendInterpolation($"{indent}Task[] disposeTasks = new Task[{singeltonAsyncDisposablesCount}];\n\n");

                    int index = 0;
                    foreach (Service service in serviceList)
                        if (service.IsAsyncDisposable)
                            AppendDisposeAsyncArray(service, index++);
                    builder.Append('\n');

                    builder.AppendInterpolation($"{indent}return new ValueTask(Task.WhenAll(disposeTasks));\n");
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

                    builder.AppendInterpolation($"{indent}Task[] disposeTasks = new Task[{singeltonAsyncDisposablesCount} + {ASYNC_DISPOSE_LIST}.Count];\n\n");

                    int index = 0;
                    foreach (Service service in serviceList)
                        if (service.IsAsyncDisposable)
                            AppendDisposeAsyncArray(service, index++);
                    builder.Append('\n');

                    builder.AppendInterpolation($"{indent}int index = {singeltonAsyncDisposablesCount};\n");
                    AppendDisposingAsyncDisposeListArray();

                    builder.AppendInterpolation($"{indent}return new ValueTask(Task.WhenAll(disposeTasks));\n");
                    break;
                }
            }

            indent.DecreaseLevel(); // 1
            builder.AppendInterpolation($"{indent}}}\n\n");
        }

        builder.Append('\n');
    }


    private void AppendDispose(Service service) {
        if (service.CreationTimeTransitive == CreationTiming.Constructor)
            builder.AppendInterpolation($"{indent}((IDisposable){service.AsServiceField}).Dispose();\n");
        else
            if (!service.IsValueType)
                builder.AppendInterpolation($"{indent}({service.AsServiceField} as IDisposable)?.Dispose();\n");
            else {
                builder.AppendInterpolation($"{indent}if ({service.AsServiceField}_hasValue)\n");
                indent.IncreaseLevel(); // +1
                builder.AppendInterpolation($"{indent}((IDisposable){service.AsServiceField}).Dispose();\n");
                indent.DecreaseLevel(); // +0
            }
    }

    private void AppendDisposeAsyncArray(Service service, int index) {
        if (service.CreationTimeTransitive == CreationTiming.Constructor)
            builder.AppendInterpolation($"{indent}disposeTasks[{index}] = ((IAsyncDisposable){service.AsServiceField}).DisposeAsync().AsTask();\n");
        else
            if (!service.IsValueType)
                builder.AppendInterpolation($"{indent}disposeTasks[{index}] = ({service.AsServiceField} as IAsyncDisposable)?.DisposeAsync().AsTask() ?? Task.CompletedTask;\n");
            else
                builder.AppendInterpolation($"{indent}disposeTasks[{index}] = {service.AsServiceField}_hasValue ? ((IAsyncDisposable){service.AsServiceField}).DisposeAsync().AsTask() : Task.CompletedTask;\n");
    }


    private void AppendDisposingDisposeList() {
        if (threadSafe) {
            builder.AppendInterpolation($"{indent}lock ({DISPOSE_LIST}_lock)\n");
            indent.IncreaseLevel(); // 3
        }

        builder.AppendInterpolation($"{indent}foreach (IDisposable disposable in {DISPOSE_LIST})\n");
        indent.IncreaseLevel(); // 3 or 4

        builder.AppendInterpolation($"{indent}disposable.Dispose();\n\n");
        indent.DecreaseLevel(); // 2 or 3

        if (threadSafe)
            indent.DecreaseLevel(); // 2
    }

    private void AppendDisposingAsyncDisposeListDiscard() {
        if (threadSafe) {
            builder.AppendInterpolation($"{indent}lock ({ASYNC_DISPOSE_LIST}_lock)\n");
            indent.IncreaseLevel(); // 3
        }

        builder.AppendInterpolation($"{indent}foreach (IAsyncDisposable asyncDisposable in {ASYNC_DISPOSE_LIST})\n");
        indent.IncreaseLevel(); // 3 or 4

        builder.AppendInterpolation($"{indent}if (asyncDisposable is IDisposable disposable)\n");
        indent.IncreaseLevel(); // 4 or 5

        builder.AppendInterpolation($"{indent}disposable.Dispose();\n");
        indent.DecreaseLevel(); // 3 or 4

        builder.AppendInterpolation($"{indent}else\n");
        indent.IncreaseLevel(); // 4 or 5

        builder.AppendInterpolation($"{indent}_ = asyncDisposable.DisposeAsync().Preserve();\n\n");
        indent.DecreaseLevel(); // 3 or 4
        indent.DecreaseLevel(); // 2 or 3

        if (threadSafe)
            indent.DecreaseLevel(); // 2
    }

    private void AppendDisposingAsyncDisposeListArray() {
        if (threadSafe) {
            builder.AppendInterpolation($"{indent}lock ({ASYNC_DISPOSE_LIST}_lock)\n");
            indent.IncreaseLevel(); // 3
        }

        builder.AppendInterpolation($"{indent}foreach (IAsyncDisposable asyncDisposable in {ASYNC_DISPOSE_LIST})\n");
        indent.IncreaseLevel(); // 3 or 4

        builder.AppendInterpolation($"{indent}disposeTasks[index++] = asyncDisposable.DisposeAsync().AsTask();\n\n");
        indent.DecreaseLevel(); // 2 or 3

        if (threadSafe)
            indent.DecreaseLevel(); // 2
    }
}
