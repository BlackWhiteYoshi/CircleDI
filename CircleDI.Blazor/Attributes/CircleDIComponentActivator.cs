﻿namespace CircleDI.Blazor;

public static partial class Attributes {
    public const string CircleDIComponentActivator = $$"""
        // <auto-generated/>
        #pragma warning disable
        #nullable enable annotations


        #if !CIRCLEDI_EXCLUDE_ATTRIBUTES
        
        using Microsoft.AspNetCore.Components;
        using System.CodeDom.Compiler;
        using System.Diagnostics.CodeAnalysis;

        namespace CircleDIAttributes;

        /// <summary>
        /// <para>An implementation of the activator that can be used to instantiate components.</para>
        /// <para>
        /// This activator will retrieve the component from the specified service provider if registered.<br />
        /// If the type is not registered, the component will be instantiated with the parameterless constructor (or fails in an Exception).<br />
        /// If the type is registered multiple times, it also fails in an Exception because of ambiguity.
        /// </para>
        /// <para>After this instantiation the framework performs it's dependency injection <see cref="InjectAttribute">[Inject]</see> and parameter injection <see cref="ParameterAttribute">[Parameter]</see>.</para>
        /// </summary>
        /// <typeparam name="TScopeProvider">Type of the service provider which is used to retrieve components. The object will be dependency injected from the built-in service provider. The object must implement <see href="https://learn.microsoft.com/en-us/dotnet/api/system.iserviceprovider">System.IServiceProvider</see>, otherwise an error will be thrown.</typeparam>
        /// <param name="scopeProvider">An ServiceProvider instance</param>
        [GeneratedCodeAttribute("{{NAME}}", "{{VERSION}}")]
        internal sealed class CircleDIComponentActivator<TScopeProvider>(TScopeProvider scopeProvider) : IComponentActivator {
            private readonly IServiceProvider _serviceProvider = scopeProvider as IServiceProvider ?? throw new ArgumentException($"The instance of registered type {typeof(TScopeProvider)} is of type {scopeProvider.GetType()}, which must implement System.IServiceProvider", nameof(TScopeProvider));

            /// <summary>
            /// It will retrieve the component from the specified service provider if registered.<br />
            /// If the type is not registered, the component will be instantiated with the parameterless constructor (or fails in an Exception).<br />
            /// If the type is registered multiple times, it also fails in an Exception because of ambiguity.
            /// </summary>
            /// <param name="componentType"></param>
            /// <returns>Returns an IComponent instance. Fails in an exception when the component is registered multiple times or is not registered and has no parameterless constructor.</returns>
            /// <exception cref="ArgumentException">Is thrown When the given type is registered multiple times.</exception>
            public IComponent CreateInstance([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)] Type componentType) {
                object? component = _serviceProvider.GetService(componentType);

                if (component is null)
                    return (IComponent)Activator.CreateInstance(componentType)!;

                if (component is Array components)
                    throw new ArgumentException($"Component of type '{componentType}' has multiple ({components.Length}) registrations", nameof(componentType));

                return (IComponent)component;
            }
        }

        #endif

        """;
}