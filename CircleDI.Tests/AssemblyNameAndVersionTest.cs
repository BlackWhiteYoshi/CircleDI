namespace CircleDI.Tests;

public sealed class AssemblyNameAndVersionTest {
    [Fact]
    public void AssemblyNameAndVersionMatch() {
        const string NAME = Defenitions.Attributes.NAME;
        const string VERSION = Defenitions.Attributes.VERSION;

        string assemblyName = typeof(Generation.CircleDIGenerator).Assembly.GetName().Name!;
        Assert.Equal(NAME, assemblyName);
        
        string assemblyVersion = typeof(Generation.CircleDIGenerator).Assembly.GetName().Version!.ToString()[..^2];
        Assert.Equal(VERSION, assemblyVersion);
        
        string assemblyVersionBlazor = typeof(Blazor.Generation.CircleDIGenerator).Assembly.GetName().Version!.ToString()[..^2];
        Assert.Equal(VERSION, assemblyVersionBlazor);

        string assemblyVersionMinimalAPI = typeof(MinimalAPI.Generation.CircleDIGenerator).Assembly.GetName().Version!.ToString()[..^2];
        Assert.Equal(VERSION, assemblyVersionMinimalAPI);
    }
}
