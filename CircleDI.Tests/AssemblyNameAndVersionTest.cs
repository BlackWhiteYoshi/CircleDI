namespace CircleDI.Tests;

public sealed class AssemblyNameAndVersionTest {
    [Test]
    public async ValueTask AssemblyNameAndVersionMatch() {
        const string NAME = Defenitions.Attributes.NAME;
        const string VERSION = Defenitions.Attributes.VERSION;

        string assemblyName = typeof(Generation.CircleDIGenerator).Assembly.GetName().Name!;
        await Assert.That(assemblyName).IsEqualTo(NAME);

        string assemblyVersion = typeof(Generation.CircleDIGenerator).Assembly.GetName().Version!.ToString(3);
        await Assert.That(assemblyVersion).IsEqualTo(VERSION);

        string assemblyVersionBlazor = typeof(Blazor.Generation.CircleDIGenerator).Assembly.GetName().Version!.ToString(3);
        await Assert.That(assemblyVersionBlazor).IsEqualTo(VERSION);

        string assemblyVersionMinimalAPI = typeof(MinimalAPI.Generation.CircleDIGenerator).Assembly.GetName().Version!.ToString(3);
        await Assert.That(assemblyVersionMinimalAPI).IsEqualTo(VERSION);
    }
}
