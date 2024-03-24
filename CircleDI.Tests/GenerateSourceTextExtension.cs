using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Reflection;

namespace CircleDI.Tests.GenerateSourceText;

public static class GenerateSourceTextExtension {
    /// <summary>
    /// <para>Takes source code as input and outputs the generated source code based on the given input with <see cref="Generation.CircleDIGenerator"/>.</para>
    /// <para>The generated source code contains post-initialization-output code as well as source output code.</para>
    /// </summary>
    /// <param name="input"></param>
    /// <param name="outputCompilation"></param>
    /// <param name="diagnostics"></param>
    /// <returns></returns>
    public static string[] GenerateSourceText(this string input, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics)
        => GenerateSourceText<Generation.CircleDIGenerator>(input, out outputCompilation, out diagnostics);

    /// <summary>
    /// <para>Takes source code as input and outputs the generated source code based on the given input with <see cref="Blazor.CircleDIGenerator"/>.</para>
    /// <para>The generated source code contains post-initialization-output code as well as source output code.</para>
    /// </summary>
    /// <param name="input"></param>
    /// <param name="outputCompilation"></param>
    /// <param name="diagnostics"></param>
    /// <returns></returns>
    public static string[] GenerateSourceTextBlazor(this string input, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics)
        => GenerateSourceText<Blazor.CircleDIGenerator>(input, out outputCompilation, out diagnostics);


    /// <summary>
    /// <para>Takes source code as input and outputs the generated source code based on the given input.</para>
    /// <para>The generated source code contains post-initialization-output code as well as source output code.</para>
    /// </summary>
    /// <param name="input"></param>
    /// <param name="outputCompilation"></param>
    /// <param name="diagnostics"></param>
    /// <returns></returns>
    private static string[] GenerateSourceText<TIncrementalGenerator>(this string input, out Compilation outputCompilation, out ImmutableArray<Diagnostic> diagnostics) where TIncrementalGenerator : IIncrementalGenerator, new() {
        TIncrementalGenerator generator = new();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGeneratorsAndUpdateCompilation(CreateCompilation(input), out outputCompilation, out diagnostics);

        GeneratorDriverRunResult runResult = driver.GetRunResult();
        GeneratorRunResult generatorResult = runResult.Results[0];

        string[] sourceTexts = new string[generatorResult.GeneratedSources.Length];
        for (int i = 0; i < sourceTexts.Length; i++)
            sourceTexts[i] = generatorResult.GeneratedSources[i].SourceText.ToString();
        return sourceTexts;


        static CSharpCompilation CreateCompilation(string source) {
            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(source);
            PortableExecutableReference metadataReference = MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location);
            CSharpCompilationOptions compilationOptions = new(OutputKind.ConsoleApplication);

            return CSharpCompilation.Create("compilation", [syntaxTree], [metadataReference], compilationOptions);
        }
    }
}
