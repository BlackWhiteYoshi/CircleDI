namespace CircleDI.Defenitions;

public static partial class Attributes {
    public const string NAME = "CircleDI";
    public const string VERSION = "0.9.0";

    private const string CREATION_TIME_PROPERTY = """
        /// <summary>
            /// <para>Decides whether this service will be lazy constructed or instantiated inside the constructor.</para>
            /// <para>Defaults to <see cref="ServiceProviderAttribute.CreationTime"/> or <see cref="ScopedProviderAttribute.CreationTime"/>.</para>
            /// </summary>
            public CreationTiming CreationTime { get; init; }
        """;

    private const string GET_ACCESSOR_PROPERTY = """
        /// <summary>
            /// <para>Decides whether the type of the member to access this service will be a property or method.</para>
            /// <para>Defaults to <see cref="ServiceProviderAttribute.GetAccessor"/> or <see cref="ScopedProviderAttribute.GetAccessor"/>.</para>
            /// </summary>
            public GetAccess GetAccessor { get; init; }
        """;

    private const string SERVICE_NAME_PROPERTY = """
        /// <summary>
            /// <para>The name of this service.</para>
            /// <para>If omitted, it will be the name of TImplementation.</para>
            /// <para>If the getter is a method, for the identifier a "Get" prefix will be used, but this does not affect the name.</para>
            /// </summary>
            public string Name { get; init; }
        """;

    private const string SERVICE_NO_DISPOSE_PROPERTY = """
        /// <summary>
            /// <para>When true, the ServiceProvider does not dispose this service on <see cref="System.IDisposable.Dispose">Dispose()</see> or <see cref="System.IAsyncDisposable.DisposeAsync">DisposeAsync()</see>,
            /// regardless the service implements <see cref="System.IDisposable">IDisposable</see> or <see cref="System.IAsyncDisposable">IAsyncDisposable</see>.</para>
            /// <para>If the service does not implement <see cref="System.IDisposable">IDisposable</see>/<see cref="System.IAsyncDisposable">IAsyncDisposable</see>, this will have no effect.</para>
            /// <para>Default is false.</para>
            /// </summary>
            public bool NoDispose { get; init; }
        """;
}
