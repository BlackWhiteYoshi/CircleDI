﻿using CircleDI.Defenitions;
using CircleDI.Generation;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CircleDI.Blazor;

[Generator(LanguageNames.CSharp)]
public sealed class CircleDIGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(static (IncrementalGeneratorPostInitializationContext context) => {
            // attributes
            // TODO ServiceProviderAttribute with 2 extra properties: AddComponents, AddDefaultServices
            context.AddSource("ServiceProviderAttribute.g.cs", Attributes.ServiceProviderAttribute);
            context.AddSource("ScopedProviderAttribute.g.cs", Attributes.ScopedProviderAttribute);
            context.AddSource("SingletonAttribute.g.cs", Attributes.SingletonAttribute);
            context.AddSource("ScopedAttribute.g.cs", Attributes.ScopedAttribute);
            context.AddSource("TransientAttribute.g.cs", Attributes.TransientAttribute);
            context.AddSource("DelegateAttribute.g.cs", Attributes.DelegateAttribute);
            context.AddSource("DependencyAttribute.g.cs", Attributes.DependencyAttribute);
            context.AddSource("ConstructorAttribute.g.cs", Attributes.ConstructorAttribute);

            // enums
            context.AddSource("CreationTiming.g.cs", Attributes.CreationTimingEnum);
            context.AddSource("GetAccess.g.cs", Attributes.GetAccessEnum);
            context.AddSource("DisposeGenerationEnum.g.cs", Attributes.DisposeGenerationEnum);

            // TODO CircleDIComponentActivator.cs
        });

        CircleDIBuilder circleDIBuilder = new();
        context.RegisterServiceProviderAttribute("CircleDIAttributes.ServiceProviderAttribute", circleDIBuilder);
        context.RegisterServiceProviderAttribute("CircleDIAttributes.ServiceProviderAttribute`1", circleDIBuilder);
    }
}

file static class CircleDIGeneratorRegisterExtension {
    public static void RegisterServiceProviderAttribute(this IncrementalGeneratorInitializationContext context, string serviceProviderAttributeName, CircleDIBuilder circleDIBuilder) {
        IncrementalValuesProvider<ServiceProvider> serviceProviderList = context.SyntaxProvider.ForAttributeWithMetadataName(
            serviceProviderAttributeName,
            static (SyntaxNode syntaxNode, CancellationToken _) => syntaxNode is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
            static (GeneratorAttributeSyntaxContext generatorAttributeSyntaxContext, CancellationToken _) => {
                ServiceProvider serviceProvider = new(generatorAttributeSyntaxContext);
                // TODO add all components and default services
                return serviceProvider;
            });

        context.RegisterSourceOutput(serviceProviderList, circleDIBuilder.GenerateClass);
        context.RegisterSourceOutput(serviceProviderList, circleDIBuilder.GenerateInterface);
    }
}