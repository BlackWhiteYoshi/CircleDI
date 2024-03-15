using CircleDI.Defenitions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CircleDI.Generation;

[Generator(LanguageNames.CSharp)]
public sealed class CircleDIGenerator : IIncrementalGenerator {
    public void Initialize(IncrementalGeneratorInitializationContext context) {
        context.RegisterPostInitializationOutput(static (IncrementalGeneratorPostInitializationContext context) => {
            // attributes
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
        });

        // all classes with ServiceProviderAttribute
        IncrementalValuesProvider<ServiceProvider> serviceProviderList = context.SyntaxProvider.ForAttributeWithMetadataName(
            "CircleDIAttributes.ServiceProviderAttribute",
            static (SyntaxNode syntaxNode, CancellationToken _) => syntaxNode is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
            static (GeneratorAttributeSyntaxContext generatorAttributeSyntaxContext, CancellationToken _) => new ServiceProvider(generatorAttributeSyntaxContext));

        CircleDIBuilder circleDIBuilder = new();
        context.RegisterSourceOutput(serviceProviderList, circleDIBuilder.GenerateClass);
        context.RegisterSourceOutput(serviceProviderList, circleDIBuilder.GenerateInterface);
    }
}
