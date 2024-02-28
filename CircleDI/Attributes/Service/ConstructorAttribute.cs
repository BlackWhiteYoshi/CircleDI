﻿namespace CircleDI;

public static partial class Attributes {
    public const string ConstructorAttribute = $$"""
        // <auto-generated/>
        #pragma warning disable
        #nullable enable annotations


        #if !CIRCLEDI_EXCLUDE_ATTRIBUTES
        
        using System;
    
        namespace CircleDIAttributes;

        /// <summary>
        /// <para>Explicitly specifies the constructor that is used to create the service.</para>
        /// <para>If multiple constructors are available, you must use this attribute on exactly one constructor, otherwise a compile error occurs.</para>
        /// </summary>
        /// <remarks>A struct has always the parameterless constructor, so by specifying one non-parameterless constructor you have actually two and therfore have to use this attribute.</remarks>
        [AttributeUsage(AttributeTargets.Constructor)]
        [System.CodeDom.Compiler.GeneratedCodeAttribute("{{NAME}}", "{{VERSION}}")]
        internal sealed class ConstructorAttribute : Attribute;

        #endif

        """;
}