using CircleDI.Defenitions;
using CircleDI.Extensions;

namespace CircleDI.Generation;

/// <summary>
/// <para>Contains the method to build the CreateScope() method.</para>
/// <para>
/// The CreateScope method has some special properties:<br />
/// - it is a predefined service, but it is not in included in the <see cref="ServiceProvider.SortedServiceList">service list</see>.<br />
/// - it is the only service with <see cref="ServiceLifetime.TransientSingleton"/>, that means it is a transient, but do not have access to a ScopeProivder (no Scoped or TransientScoped services).
/// - only [Dependency] flagged parameters (in Scope constructor method) are injected from the provider, others are parameters of this method.
/// </para>
/// </summary>
public partial struct CircleDIBuilderCore {
    /// <summary>
    /// "special" method CreateScope()
    /// </summary>
    public void AppendCreateScope() {
        if (!serviceProvider.GenerateScope)
            return;

        AppendCreateScopeSummary();

        // method head
        {
            TypeName identifier = serviceProvider.HasInterface ? serviceProvider.InterfaceIdentifierScope : serviceProvider.IdentifierScope;
            builder.AppendInterpolation($"{indent}public global::{identifier.AsOpenFullyQualified()} CreateScope{serviceProvider.IdentifierScope.AsOpenGenerics()}(");

            foreach (Dependency dependency in serviceProvider.CreateScope.ConstructorDependencyList.Concat<Dependency>(serviceProvider.CreateScope.PropertyDependencyList))
                if (!dependency.HasAttribute)
                    builder.AppendInterpolation($"global::{dependency.ServiceType.AsClosedFullyQualified()} {dependency.Name.AsFirstLower()}, ");
            if (builder[^1] == ' ')
                builder.Length -= 2;

            builder.Append(") {\n");
            indent.IncreaseLevel(); // 2
        }

        // method body
        AppendCreateScopeServiceTree();

        indent.DecreaseLevel(); // 1
        builder.AppendInterpolation($"{indent}}}\n\n\n");
    }
}
