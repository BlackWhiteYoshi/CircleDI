using System.Reflection;

namespace CircleDI.Tests;

public sealed class AssemblyNameAndVersionTest {
    [Fact]
    public void AssemblyNameAndVersionMatch() {
        string assemblyName = typeof(CircleDIGenerator).Assembly.GetName().Name!;
        string assemblyVersion = typeof(CircleDIGenerator).Assembly.GetName().Version!.ToString()[..^2];

        FieldInfo[] fields = typeof(Attributes).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
        string name = (string)fields[0].GetValue(null)!;
        string version = (string)fields[1].GetValue(null)!;

        Assert.Equal(assemblyName, name);
        Assert.Equal(assemblyVersion, version);
    }
}
