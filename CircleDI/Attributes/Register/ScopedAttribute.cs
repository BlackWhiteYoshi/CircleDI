﻿namespace CircleDI;

public static partial class Attributes {
    public const string ScopedAttribute = $$"""
        // <auto-generated/>
        #pragma warning disable
        #nullable enable annotations


        #if !CIRCLEDI_EXCLUDE_ATTRIBUTES
        
        using System;
    
        namespace CircleDIAttributes;

        /// <summary>
        /// <para>Specifies a scoped service. That means this service will only be available in the ScopedProvider and there will be a single instance of that service in every ScopedProvider instance.</para>
        /// <para>If <see cref="ServiceProviderAttribute"/> is used at the same class, this service will be added to the provider.</para>
        /// </summary>
        /// <typeparam name="TService">Type of the service.</typeparam>
        /// <typeparam name="TImplementation">Type of the implementation.</typeparam>
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("{{NAME}}", "{{VERSION}}")]
        internal class ScopedAttribute<TService, TImplementation> : Attribute where TImplementation : TService {
            /// <summary>
            /// <para>Fieldname, propertyname or methodname that will be the implementation supplier for the given service.</para>
            /// <para>The parameters of the method will be dependency injected.</para>
            /// </summary>
            public string Implementation { get; init; }

            {{SERVICE_NAME_PROPERTY}}

            {{CREATION_TIME_PROPERTY}}

            {{GET_ACCESSOR_PROPERTY}}
        }

        /// <summary>
        /// Shorthand for <see cref="ScopedAttribute{TService, TImplementation}"/> where type of service and implementation is the same.
        /// </summary>
        /// <typeparam name="TService">Type of the service and implementation.</typeparam>
        [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = true)]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("{{NAME}}", "{{VERSION}}")]
        internal sealed class ScopedAttribute<TService> : ScopedAttribute<TService, TService>;

        #endif

        """;
}
