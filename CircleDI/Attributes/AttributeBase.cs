namespace CircleDI;

public static partial class Attributes {
    private const string NAME = "CircleDI";
    private const string VERSION = "0.2.0";

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

    private const string DEPENDENCY_NAME_PROPERTY = """
        /// <summary>
            /// <para>The name this dependency gets the service injected from.</para>
            /// <para>If omitted, it will match based on the type.</para>
            /// <para>If multiple services for this type exists, this property must be set, otherwise compile error.</para>
            /// <para>When the Name property of a service not set, the name defaults to the name of TImplementation.</para>
            /// </summary>
            public string Name { get; init; }
        """;
}
