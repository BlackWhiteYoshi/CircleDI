﻿namespace CircleDI;

public static partial class Attributes {
    public const string DelegateAttribute = $$"""
        // <auto-generated/>
        #pragma warning disable
        #nullable enable annotations


        #if !CIRCLEDI_EXCLUDE_ATTRIBUTES
        
        using System;
    
        namespace CircleDIAttributes;

        /// <summary>
        /// <para>Specifies a delegate service. That means requesting this service will give you a method.</para>
        /// <para>If <see cref="ServiceProviderAttribute"/> is used at the same class, this service will be added to the provider.</para>
        /// </summary>
        /// <typeparam name="TService">Type of the service and implementation.</typeparam>
        /// <param name="methodName">Methodname that will be the implementation for the given service.</param>
        [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("{{NAME}}", "{{VERSION}}")]
        internal sealed class DelegateAttribute<TService>(string methodName) : Attribute {
            {{SERVICE_NAME_PROPERTY}}

            {{GET_ACCESSOR_PROPERTY}}
        }

        #endif

        """;
}