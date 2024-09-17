using CircleDI.Defenitions;
using CircleDI.Extensions;
using CircleDI.Generation;
using CircleDI.MinimalAPI.Defenitions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace CircleDI.MinimalAPI.Generation;

/// <summary>
/// Contains all necessary information to source generate an endpoint registration.<br />
/// It includes the information about the attribute (route, HTTP method, routeBuilderMethod) and the static method (name, parameter).
/// </summary>
public sealed class Endpoint : IEquatable<Endpoint> {
    /// <summary>
    /// The fully qualified name of the method that is executed to handle this endpoint.
    /// </summary>
    public required MethodName MethodHandler { get; init; }


    /// <summary>
    /// the url/pattern of this endpoint.
    /// </summary>
    public string Route { get; init; } = string.Empty;

    /// <summary>
    /// The type of HTTP method: Get, Post, Put, Patch, Delete or anything
    /// </summary>
    public Http HttpMethod { get; init; } = Http.Any;

    /// <summary>
    /// The name of the extension method that is normally chained after the mapping call.
    /// </summary>
    public MethodName? MethodRouteBuilder { get; init; }


    /// <summary>
    /// <para>Primary for <see cref="Service.ConstructorDependencyList">parameter list</see> and to init dependency tree it.</para>
    /// <para>Similar to <see cref="ServiceProvider.CreateScope"/> this Service will mark dependencies with <see cref="Dependency.HasAttribute"/> flag.</para>
    /// <para>
    /// The 4 important information about a parameter are stored as followed:<br />
    /// - type -> if <see cref="Dependency.HasAttribute"/> ? Dependency.Service.ServiceType : <see cref="Dependency.ServiceType"/><br />
    /// - name -> <see cref="Dependency.Name"/><br />
    /// - isDependency -> <see cref="Dependency.HasAttribute"/><br />
    /// - attributes -> <see cref="ParameterAttributesList"/>
    /// </para>
    /// </summary>
    public required Service AsService { get; init; }

    /// <summary>
    /// <para>The corresponding attribute list for the parameters in <see cref="AsService">AsService</see>.<see cref="Service.ConstructorDependencyList">ConstructorDependencyList</see>.</para>
    /// <para>The first index corresponds to the parameter with the same index in <see cref="Service.ConstructorDependencyList">ConstructorDependencyList</see>.</para>
    /// <para>
    /// The second dimension lists all the attributes of that parameter.<br />
    /// An attribute does only contain it's content, missing surrounding '[', ']'
    /// </para>
    /// <para>If the parameter is a dependency, the second dimension is null.</para>
    /// </summary>
    public required ParameterAttribute[]?[] ParameterAttributesList { get; init; }


    public EndpointErrorManager ErrorManager { get; private set; }


    public Endpoint(AttributeData attribute) => ErrorManager = new EndpointErrorManager(attribute, []);

    [SetsRequiredMembers]
    public Endpoint(GeneratorAttributeSyntaxContext syntaxContext, List<Diagnostic> endpointErrorList) {
        Debug.Assert(syntaxContext.Attributes.Length > 0);
        AttributeData attribute = syntaxContext.Attributes[0];
        ErrorManager = new EndpointErrorManager(attribute, endpointErrorList);
        IMethodSymbol method = (IMethodSymbol)syntaxContext.TargetSymbol;

        MethodHandler = new MethodName(method);

        if (!method.IsStatic)
            ErrorManager.AddEndpointMethodNonStaticError(MethodHandler);

        if (method.TypeParameters.Length > 0)
            ErrorManager.AddEndpointMethodGenericError(MethodHandler);


        if (attribute.ConstructorArguments.Length >= 2) {
            if (attribute.ConstructorArguments[0] is TypedConstant { Value: string route })
                Route = route;
            if (attribute.ConstructorArguments[1] is TypedConstant { Value: int httpMethod })
                HttpMethod = (Http)httpMethod;
        }

        if (attribute.NamedArguments.GetArgument<string>("RouteBuilder") is string routeBuilderName) {
            if (method.ContainingType is not INamedTypeSymbol container)
                // invalid source code, methods can't be top level
                goto return_RouteBuilder;

            if (container.GetMembers(routeBuilderName) is not [IMethodSymbol routeBuilderMethod]) {
                ErrorManager.AddMissingRouteBuilderMethodError(container, routeBuilderName);
                goto return_RouteBuilder;
            }

            if (!routeBuilderMethod.IsStatic) {
                ErrorManager.AddRouteBuilderNonStaticError(routeBuilderName);
                goto return_RouteBuilder;
            }

            if (routeBuilderMethod.TypeParameters.Length > 0) {
                ErrorManager.AddRouteBuilderGenericError(routeBuilderName);
                goto return_RouteBuilder;
            }

            if (routeBuilderMethod.Parameters is not [
                IParameterSymbol {
                    Type: INamedTypeSymbol {
                        ContainingNamespace.ContainingNamespace.ContainingNamespace.ContainingNamespace.Name: "",
                        ContainingNamespace.ContainingNamespace.ContainingNamespace.Name: "Microsoft",
                        ContainingNamespace.ContainingNamespace.Name: "AspNetCore",
                        ContainingNamespace.Name: "Builder",
                        Name: "RouteHandlerBuilder",
                        TypeKind: TypeKind.Class,
                        ContainingType: null,
                        TypeParameters: []
                    }
                }
            ]) {
                ErrorManager.AddRouteBuilderParameterListError(routeBuilderName);
                goto return_RouteBuilder;
            }

            MethodRouteBuilder = new MethodName(routeBuilderMethod);

            return_RouteBuilder:;
        }


        List<ConstructorDependency> constructorDependencyList = method.CreateConstructorDependencyList();

        MethodDeclarationSyntax methodNode = (MethodDeclarationSyntax)syntaxContext.TargetNode;
        ParameterAttributesList = new ParameterAttribute[constructorDependencyList.Count][];
        for (int i = 0; i < method.Parameters.Length; i++) {
            if (constructorDependencyList[i].HasAttribute)
                ParameterAttributesList[i] = null;
            else {
                System.Collections.Immutable.ImmutableArray<AttributeData> attributeList = method.Parameters[i].GetAttributes();
                ParameterAttributesList[i] = new ParameterAttribute[attributeList.Length];
                for (int j = 0; j < attributeList.Length; j++)
                    ParameterAttributesList[i]![j] = new ParameterAttribute(attributeList[j]);
            }
        }

        AsService = new Service() {
            Name = $"Endooint: {MethodHandler.Name}",
            Lifetime = ServiceLifetime.Scoped,
            ServiceType = null!,
            ImplementationType = null!,

            ConstructorDependencyList = constructorDependencyList,
            PropertyDependencyList = [],
            Dependencies = constructorDependencyList.Where((Dependency dependency) => dependency.HasAttribute)
        };
    }


    #region Equals

    public static bool operator ==(Endpoint? left, Endpoint? right)
         => (left, right) switch {
             (null, null) => true,
             (null, not null) => false,
             (not null, _) => left.Equals(right)
         };

    public static bool operator !=(Endpoint? left, Endpoint? right) => !(left == right);

    public override bool Equals(object? obj) => Equals(obj as Endpoint);

    public bool Equals(Endpoint? other) {
        if (other is null)
            return false;
        if (ReferenceEquals(this, other))
            return true;

        if (MethodHandler != other.MethodHandler)
            return false;

        if (Route != other.Route)
            return false;
        if (HttpMethod != other.HttpMethod)
            return false;
        if (MethodRouteBuilder != other.MethodRouteBuilder)
            return false;

        if (AsService != other.AsService)
            return false;

        if (ParameterAttributesList.Length != other.ParameterAttributesList.Length)
            return false;
        for (int i = 0; i < ParameterAttributesList.Length; i++)
            switch ((ParameterAttributesList[i], other.ParameterAttributesList[i])) {
                case (null, not null) or (not null, null):
                    return false;
                case (null, null):
                    break;
                case (not null, not null):
                    if (ParameterAttributesList[i]!.Length != other.ParameterAttributesList[i]!.Length)
                        return false;
                    for (int j = 0; j < ParameterAttributesList[i]!.Length; j++)
                        if (ParameterAttributesList[i]![j] != other.ParameterAttributesList[i]![j])
                            return false;
                    break;
            }

        if (!ErrorManager.ErrorList.SequenceEqual(other.ErrorManager.ErrorList))
            return false;

        return true;
    }

    public override int GetHashCode() {
        int hashCode = MethodHandler.GetHashCode();

        hashCode = Combine(hashCode, Route.GetHashCode());
        hashCode = Combine(hashCode, HttpMethod.GetHashCode());
        hashCode = Combine(hashCode, MethodRouteBuilder?.GetHashCode() ?? 0);

        hashCode = Combine(hashCode, AsService.GetHashCode());

        foreach (ParameterAttribute[]? attributeList in ParameterAttributesList)
            if (attributeList != null)
                hashCode = CombineList(hashCode, attributeList);
            else
                hashCode = Combine(hashCode, 0);

        hashCode = CombineList(hashCode, ErrorManager.ErrorList);

        return hashCode;


        static int CombineList<T>(int hashCode, IEnumerable<T> list) where T : notnull {
            foreach (T item in list)
                hashCode = Combine(hashCode, item.GetHashCode());
            return hashCode;
        }

        static int Combine(int h1, int h2) {
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }

    #endregion
}
