using CircleDI.Generation;
using System.Reflection;

namespace CircleDI.Tests;

public sealed class AssemblyNameAndVersionTest {
    [Fact]
    public void AssemblyNameAndVersionMatch() {
        #region CircleDI

        string assemblyName = typeof(CircleDIGenerator).Assembly.GetName().Name!;
        string assemblyVersion = typeof(CircleDIGenerator).Assembly.GetName().Version!.ToString()[..^2];

        FieldInfo[] fields = typeof(Defenitions.Attributes).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
        string name = (string)fields[0].GetValue(null)!;
        string version = (string)fields[1].GetValue(null)!;

        Assert.Equal(name, assemblyName);

        #endregion


        #region CircleDI.Blazor

        string assemblyNameBlazor = typeof(Blazor.Generation.CircleDIGenerator).Assembly.GetName().Name!;
        string assemblyVersionBlazor = typeof(Blazor.Generation.CircleDIGenerator).Assembly.GetName().Version!.ToString()[..^2];

        FieldInfo[] fieldsBlazor = typeof(Blazor.Defenitions.Attributes).GetFields(BindingFlags.NonPublic | BindingFlags.Static);
        string nameBlazor = (string)fieldsBlazor[0].GetValue(null)!;
        string versionBlazor = (string)fieldsBlazor[1].GetValue(null)!;

        Assert.Equal(nameBlazor, assemblyNameBlazor);

        #endregion


        Assert.Equal(version, assemblyVersion);
        Assert.Equal(version, versionBlazor);
        Assert.Equal(version, assemblyVersionBlazor);
    }
}
