using CircleDI.Defenitions;
using CircleDI.Extensions;

namespace CircleDI.Generation;

/// <summary>
/// Contains the method to build the constructor or InitServices method, including the parameter fields and summary.
/// </summary>
public partial struct CircleDIBuilderCore {
    /// <summary>
    /// Appends<br />
    /// - the parameter fields<br />
    /// - constructor/InitServices() summary<br />
    /// - constructor parameterList<br />
    /// - singleton/scoped services initialization
    /// </summary>
    public void AppendConstructor() {
        // parameter fields
        {
            int i = 1;
            if (isScopeProvider)
                builder.AppendInterpolation($"{indent}private {readonlyStr}global::{serviceProvider.Identifier.AsOpenFullyQualified()} _{serviceProvider.Identifier.Name.AsFirstLower()};\n");
            else
                i = 0;

            for (; i < constructorParameterList.Count; i++)
                // ConstructorParameterList items have always serviceType set
                builder.AppendInterpolation($"{indent}private {readonlyStr}global::{constructorParameterList[i].ServiceType!.AsClosedFullyQualified()} _{constructorParameterList[i].Name.AsFirstLower()};\n");

            if (i > 0)
                builder.Append('\n');
        }

        // lock object
        if (hasLock)
            // TODO just use Lock in both cases when there is support by PolySharp (https://github.com/Sergio0694/PolySharp)
            builder.AppendInterpolation($"""
                #if NET9_0_OR_GREATER
                {indent}private readonly global::System.Threading.Lock _lock = new();
                #else
                {indent}private readonly global::System.Object _lock = new();
                #endif


                """);

        // <summary> + method name
        AppendConstructionSummary();
        indent.IncreaseLevel(); // 2

        // constructor parameters
        builder.Append('(');
        if (constructorParameterList.Count > 0) {
            foreach (Dependency parameter in constructorParameterList)
                builder.AppendInterpolation($"global::{parameter.ServiceType!.AsClosedFullyQualified()} {parameter.Name.AsFirstLower()}, ");
            builder.Length -= 2;
        }
        builder.Append(") {\n");

        // parameter field = parameter
        {
            int i = 1;
            if (isScopeProvider)
                if (serviceProvider.HasInterface)
                    builder.AppendInterpolation($"{indent}_{serviceProvider.Identifier.Name.AsFirstLower()} = (global::{serviceProvider.Identifier.AsOpenFullyQualified()}){serviceProvider.Identifier.Name.AsFirstLower()};\n");
                else
                    builder.AppendInterpolation($"{indent}_{serviceProvider.Identifier.Name.AsFirstLower()} = {serviceProvider.Identifier.Name.AsFirstLower()};\n");
            else
                i = 0;

            for (; i < constructorParameterList.Count; i++)
                builder.AppendInterpolation($"{indent}_{constructorParameterList[i].Name.AsFirstLower()} = {constructorParameterList[i].Name.AsFirstLower()};\n");

            if (i > 0)
                builder.Append('\n');
        }

        AppendConstructorServices();

        if (builder[^2] == '\n')
            builder.Length--;

        indent.DecreaseLevel(); // 1
        builder.AppendInterpolation($"{indent}}}\n\n");
    }

    private void AppendConstructionSummary() {
        // constructor
        if (!hasConstructor) {
            // ServiceProvider
            if (!isScopeProvider)
                builder.AppendInterpolation($"""
                    {indent}/// <summary>
                    {indent}/// Creates an instance of a ServiceProvider together with all <see cref="global::CircleDIAttributes.CreationTiming.Constructor">non-lazy</see> singleton services.
                    {indent}/// </summary>
                    {indent}public {serviceProvider.Identifier.Name}
                    """);
            // ScopeProvider
            else {
                AppendCreateScopeSummary();
                if (serviceProvider.HasInterface)
                    builder.AppendInterpolation($"""
                        {indent}/// <param name="{serviceProvider.Identifier.Name.AsFirstLower()}">An instance of the service provider this provider is the scope of. It must be an instance of <see cref="{serviceProvider.Identifier.Name}"/>.</param>
                        {indent}public Scope
                        """);
                else
                    builder.AppendInterpolation($"""
                        {indent}/// <param name="{serviceProvider.Identifier.Name.AsFirstLower()}">An instance of the service provider this provider is the scope of.</param>
                        {indent}public Scope
                        """);
            }
        }
        // InitServices()
        else {
            // ServiceProvider
            if (!isScopeProvider)
                builder.AppendInterpolation($"""
                    {indent}/// <summary>
                    {indent}/// Constructs non-lazy singleton services. Should be called inside the constructor at the end.
                    {indent}/// </summary>

                    """);
            // ScopeProvider
            else {
                if (serviceProvider.HasInterface)
                    builder.AppendInterpolation($$"""
                        {{indent}}/// <summary>
                        {{indent}}/// Constructs non-lazy scoped services. Should be called inside the constructor at the end.
                        {{indent}}/// </summary>
                        {{indent}}/// <param name="{{serviceProvider.Identifier.Name.AsFirstLower()}}">
                        {{indent}}/// The ServiceProvider this ScopedProvider is created from. It must be an instance of <see cref="{{serviceProvider.Identifier.Name}}"/>. Usually it is the object you get injected to your constructor parameter:<br />
                        {{indent}}/// public Scope([Dependency] {{(serviceProvider.HasInterface ? serviceProvider.InterfaceIdentifier.Name : serviceProvider.Identifier.Name)}} {{serviceProvider.Identifier.Name.AsFirstLower()}}) { ...
                        {{indent}}/// </param>

                        """);
                else
                    builder.AppendInterpolation($$"""
                        {{indent}}/// <summary>
                        {{indent}}/// Constructs non-lazy scoped services. Should be called inside the constructor at the end.
                        {{indent}}/// </summary>
                        {{indent}}/// <param name="{{serviceProvider.Identifier.Name.AsFirstLower()}}">
                        {{indent}}/// The ServiceProvider this ScopedProvider is created from. Usually it is the object you get injected to your constructor parameter:<br />
                        {{indent}}/// public Scope([Dependency] {{(serviceProvider.HasInterface ? serviceProvider.InterfaceIdentifier.Name : serviceProvider.Identifier.Name)}} {{serviceProvider.Identifier.Name.AsFirstLower()}}) { ...
                        {{indent}}/// </param>

                        """);
            }

            // MemberNotNullAttribute
            {
                int rollbackPosition = builder.Length;

                builder.AppendInterpolation($"{indent}[System.Diagnostics.CodeAnalysis.MemberNotNull(");
                int startLength = builder.Length;

                foreach (Dependency parameter in constructorParameterList)
                    builder.AppendInterpolation($"nameof(_{parameter.Name.AsFirstLower()}), ");

                foreach (Service service in serviceList)
                    if (service.CreationTimeTransitive == CreationTiming.Constructor && service.Implementation.Type != MemberType.Field)
                        builder.AppendInterpolation($"nameof(_{service.Name.AsFirstLower()}), ");

                if (hasDisposeList)
                    builder.AppendInterpolation($"nameof({DISPOSE_LIST.AsFirstLower()}), ");

                if (hasAsyncDisposeList)
                    builder.AppendInterpolation($"nameof({ASYNC_DISPOSE_LIST.AsFirstLower()}), ");

                if (builder.Length > startLength) {
                    builder.Length -= 2; // remove ", "
                    builder.Append(")]\n");
                }
                else
                    builder.Length = rollbackPosition;
            }

            builder.AppendInterpolation($"{indent}private void InitServices");
        }
    }
}
