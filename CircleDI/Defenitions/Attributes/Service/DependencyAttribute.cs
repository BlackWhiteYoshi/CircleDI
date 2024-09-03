﻿namespace CircleDI.Defenitions;

public static partial class Attributes {
    public const string DependencyAttribute = $$"""
        // <auto-generated/>
        #pragma warning disable
        #nullable enable annotations


        #if !CIRCLEDI_EXCLUDE_ATTRIBUTES

        using System;

        namespace CircleDIAttributes;

        /// <summary>
        /// <para>This attribute is used to set a non-required property as dependency (required properties are always dependencies).</para>
        /// <para>It is also used to specify specific/named services, see <see cref="Name"/>.</para>
        /// </summary>
        [AttributeUsage(AttributeTargets.Property | AttributeTargets.Parameter)]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("{{NAME}}", "{{VERSION}}")]
        internal sealed class DependencyAttribute : Attribute {
            /// <summary>
            /// <para>The name this dependency gets the service injected from.</para>
            /// <para>If omitted, it will match based on the type.</para>
            /// <para>If multiple services for this type exists, this property must be set, otherwise compile error.</para>
            /// <para>When the Name property of a service not set, the name defaults to the name of TImplementation.</para>
            /// </summary>
            public string Name { get; init; }
        }

        #endif

        """;
}
