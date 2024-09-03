﻿namespace CircleDI.MinimalAPI.Defenitions;

public static partial class Attributes {
    public const string HttpEnum = $$"""
        // <auto-generated/>
        #pragma warning disable
        #nullable enable annotations


        #if !CIRCLEDI_EXCLUDE_ATTRIBUTES

        namespace CircleDIAttributes;

        /// <summary>
        /// HTTP method of the endpoint.
        /// </summary>
        [System.CodeDom.Compiler.GeneratedCodeAttribute("{{NAME}}", "{{VERSION}}")]
        internal enum Http {
            /// <summary>
            /// Matches any HTTP requests for the specified pattern.
            /// </summary>
            Any,

            /// <summary>
            /// Matches HTTP GET requests for the specified pattern.
            /// </summary>
            Get,

            /// <summary>
            /// Matches HTTP POST requests for the specified pattern.
            /// </summary>
            Post,

            /// <summary>
            /// Matches HTTP PUT requests for the specified pattern.
            /// </summary>
            Put,

            /// <summary>
            /// Matches HTTP PATCH requests for the specified pattern.
            /// </summary>
            Patch,

            /// <summary>
            /// Matches HTTP DELETE requests for the specified pattern.
            /// </summary>
            Delete,
        }

        #endif

        """;
}
