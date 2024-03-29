﻿namespace CircleDI.Blazor.Defenitions;

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

            {{ServiceProviderAttributePropertyDefaultServiceGeneration}}

            {{ServiceProviderAttributePropertyAddRazorComponents}}
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

            {{ServiceProviderAttributePropertyDefaultServiceGeneration}}

            {{ServiceProviderAttributePropertyAddRazorComponents}}
        }

        #endif

        """;


    private const string ServiceProviderAttributePropertyDefaultServiceGeneration = """
        /// <summary>
            /// <para>
            /// Toggles the generation of default services from the built-in service provider.<br />
            /// It can be configured to have only services that are available in all environments, all services for a specific environment or disable generating any default services.<br />
            /// If enabled, it also adds a <see cref="System.IServiceProvider"/> parameter to the constructor parameters.
            /// </para>
            /// <para>Default is <see cref="BlazorServiceGeneration.ServerAndWebassembly"/>.</para>
            /// </summary>
            public BlazorServiceGeneration DefaultServiceGeneration { get; init; }
        """;

    private const string ServiceProviderAttributePropertyAddRazorComponents = """
        /// <summary>
            /// <para>
            /// Decides whether classes derived from <see cref="Microsoft.AspNetCore.Components.ComponentBase"/> are automatically registered or not.
            /// </para>
            /// <para>Default is true.</para>
            /// </summary>
            public bool AddRazorComponents { get; init; }
        """;
}