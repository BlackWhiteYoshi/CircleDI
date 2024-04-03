﻿namespace CircleDI.MinimalAPI.Defenitions;

public static partial class Attributes {
    public const string ServiceProviderAttribute = $$"""
        // <auto-generated/>
        #pragma warning disable
        #nullable enable annotations


        #if !CIRCLEDI_EXCLUDE_ATTRIBUTES
        
        using System;
    
        namespace CircleDIAttributes;
        
        {{CircleDI.Defenitions.Attributes.ServiceProviderAttributeSummary}}
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("{{NAME}}", "{{VERSION}}")]
        internal sealed class ServiceProviderAttribute : Attribute {
            /// <summary>
            /// <para>Name/Identifier of the generated Interface.</para>
            /// <para>If omitted, the name will be "I{ClassName}".</para>
            /// </summary>
            public string InterfaceName { get; init; }

            {{CircleDI.Defenitions.Attributes.ServiceProviderAttributeContentWithoutInterfaceName}}

            {{ServiceProviderAttributePropertyEndpointProvider}}
        }

        {{CircleDI.Defenitions.Attributes.ServiceProviderAttributeSummary}}
        /// <typeparam name="TInterface">
        /// An explicit declared interface the generated interface will be based on: The name, access modifier, namespace and containing types will be inferred.<br />
        /// That interface must be partial.<br />
        /// If the generated interface is used without declaring the interface yourself, it will have no effect.
        /// </typeparam>
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("{{NAME}}", "{{VERSION}}")]
        internal sealed class ServiceProviderAttribute<TInterface> : Attribute {
            {{CircleDI.Defenitions.Attributes.ServiceProviderAttributeContentWithoutInterfaceName}}

            {{ServiceProviderAttributePropertyEndpointProvider}}
        }

        #endif

        """;

    private const string ServiceProviderAttributePropertyEndpointProvider = """
        /// <summary>
            /// <para>
            /// Indicates that this provider is taken for resolving [Dependency]-parameters in [Endpoint]-methods.
            /// </para>
            /// <para>Default is true.</para>
            /// </summary>
            public bool EndpointProvider { get; init; }
        """;
}
