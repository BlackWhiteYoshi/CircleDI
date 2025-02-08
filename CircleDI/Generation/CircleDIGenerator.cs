using CircleDI.Defenitions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.Extensions.ObjectPool;
using System.Text;

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
            context.AddSource("ImportAttribute.g.cs", Attributes.ImportAttribute);
            context.AddSource("DependencyAttribute.g.cs", Attributes.DependencyAttribute);
            context.AddSource("ConstructorAttribute.g.cs", Attributes.ConstructorAttribute);

            // enums
            context.AddSource("CreationTiming.g.cs", Attributes.CreationTimingEnum);
            context.AddSource("GetAccess.g.cs", Attributes.GetAccessEnum);
            context.AddSource("DisposeGeneration.g.cs", Attributes.DisposeGenerationEnum);
            context.AddSource("ImportMode.g.cs", Attributes.ImportModeEnum);

            // polyfill
            context.AddSource("Lock.g.cs", Attributes.LockPolyfill);
        });

        ObjectPool<StringBuilder> stringBuilderPool = CircleDIBuilder.CreateStringBuilderPool();
        context.RegisterServiceProviderAttribute("CircleDIAttributes.ServiceProviderAttribute", stringBuilderPool);
        context.RegisterServiceProviderAttribute("CircleDIAttributes.ServiceProviderAttribute`1", stringBuilderPool);
    }
}

file static class RegisterServiceProviderAttributeExtension {
    public static void RegisterServiceProviderAttribute(this IncrementalGeneratorInitializationContext context, string serviceProviderAttributeName, ObjectPool<StringBuilder> stringBuilderPool) {
        IncrementalValuesProvider<ServiceProvider> serviceProviderList = context.SyntaxProvider.ForAttributeWithMetadataName(
            serviceProviderAttributeName,
            static (SyntaxNode syntaxNode, CancellationToken _) => syntaxNode is ClassDeclarationSyntax or StructDeclarationSyntax or RecordDeclarationSyntax,
            static (GeneratorAttributeSyntaxContext generatorAttributeSyntaxContext, CancellationToken _) => new ServiceProvider(generatorAttributeSyntaxContext)
        ).Select((ServiceProvider serviceProvider, CancellationToken _) => serviceProvider.CreateDependencyTree()).WithComparer(NoComparison<ServiceProvider>.Instance);

        context.RegisterSourceOutput(serviceProviderList, stringBuilderPool.GenerateClass);
        context.RegisterSourceOutput(serviceProviderList, stringBuilderPool.GenerateInterface);
    }
}
