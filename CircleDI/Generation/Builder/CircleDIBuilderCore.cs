using CircleDI.Defenitions;
using CircleDI.Extensions;
using System.Runtime.InteropServices;
using System.Text;

namespace CircleDI.Generation;

/// <summary>
/// <para>
/// This struct contains the sub-methods for <see cref="CircleDIBuilder.GenerateClass">GenerateClass</see> and <see cref="CircleDIBuilder.GenerateInterface">GenerateInterface</see>.<br />
/// The fields of this struct are basically local variables shared across all these methods.
/// </para>
/// <para>Primarily for performance reasons (reusing memory to reduce memory allocations), the code is structured in this partial struct instead of splitting it in multiple types.</para>
/// </summary>
[StructLayout(LayoutKind.Auto)]
public partial struct CircleDIBuilderCore {
    private readonly StringBuilder builder;
    private readonly ServiceProvider serviceProvider;

    private List<Service> serviceList;
    private List<ConstructorDependency> constructorParameterList;
    private TypeKeyword keyword;
    private DisposeGeneration generateDisposeMethods;
    private bool hasConstructor;
    private bool hasDisposeMethod;
    private bool hasDisposeAsyncMethod;
    private bool threadSafe;

    private string readonlyStr;
    private bool hasDisposeList;
    private bool hasAsyncDisposeList;

    private bool isScopeProvider = false;
    public Indent indent = new();


    /// <summary>
    /// Initializes the struct with fields set to ServiceProvider/MainProvider and indentation level 0.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="serviceProvider"></param>
    public CircleDIBuilderCore(StringBuilder builder, ServiceProvider serviceProvider) {
        this.builder = builder;
        this.serviceProvider = serviceProvider;

        serviceList = serviceProvider.SingletonList;
        constructorParameterList = serviceProvider.ConstructorParameterList;
        keyword = serviceProvider.Keyword;
        generateDisposeMethods = serviceProvider.GenerateDisposeMethods;
        hasConstructor = serviceProvider.HasConstructor;
        hasDisposeMethod = serviceProvider.HasDisposeMethod;
        hasDisposeAsyncMethod = serviceProvider.HasDisposeAsyncMethod;
        threadSafe = serviceProvider.ThreadSafe;

        readonlyStr = hasConstructor switch {
            true => "",
            false => "readonly "
        };

        if (generateDisposeMethods == DisposeGeneration.NoDisposing) {
            hasDisposeList = false;
            hasAsyncDisposeList = false;
        }
        else
            foreach (Service service in serviceProvider.TransientList) {
                if (service.Lifetime is ServiceLifetime.TransientScoped)
                    continue;

                if (service.IsAsyncDisposable) {
                    hasAsyncDisposeList = true;
                    if (hasDisposeList)
                        break;
                }
                else if (service.IsDisposable) {
                    hasDisposeList = true;
                    if (hasAsyncDisposeList)
                        break;
                }
            }
    }

    /// <summary>
    /// Sets all related ServiceProvider fields to ScopeProvider equivalents.
    /// </summary>
    public void SetToScope() {
        isScopeProvider = true;

        serviceList = serviceProvider.ScopedList;
        constructorParameterList = serviceProvider.ConstructorParameterListScope;
        keyword = serviceProvider.KeywordScope;
        generateDisposeMethods = serviceProvider.GenerateDisposeMethodsScope;
        hasConstructor = serviceProvider.HasConstructorScope;
        hasDisposeMethod = serviceProvider.HasDisposeMethodScope;
        hasDisposeAsyncMethod = serviceProvider.HasDisposeAsyncMethodScope;
        threadSafe = serviceProvider.ThreadSafeScope;

        readonlyStr = hasConstructor switch {
            true => "",
            false => "readonly "
        };

        if (generateDisposeMethods == DisposeGeneration.NoDisposing) {
            hasDisposeList = false;
            hasAsyncDisposeList = false;
        }
        else
            foreach (Service service in serviceProvider.TransientList)
                if (service.IsAsyncDisposable) {
                    hasAsyncDisposeList = true;
                    if (hasDisposeList)
                        break;
                }
                else if (service.IsDisposable) {
                    hasDisposeList = true;
                    if (hasAsyncDisposeList)
                        break;
                }
    }


    /// <summary>
    /// <para>Appends ServiceProvider summary:</para>
    /// <para>
    /// &lt;summary&gt;<br />
    /// Number of services registered: {}<br />
    /// - Singleton: {}<br />
    /// - Scoped: {}<br />
    /// - Transient: {}<br />
    /// - Delegate: {}
    /// </para>
    /// <para>
    /// This provider [can create a scope | has no scope],<br />
    /// implements [no Dispose methods | only synchronous Dispose() method | only asynchronous DisposeAsync() method | both Dispose() and DisposeAsync() methods]<br />
    /// and is {not} thread safe.<br />
    /// &lt;/summary&gt;
    /// </para>
    /// </summary>
    public void AppendClassSummary() {
        builder.AppendIndent(indent)
            .Append("/// <summary>\n");


        builder.AppendIndent(indent)
            .Append("/// <para>\n");
        
        builder.AppendIndent(indent)
            .AppendInterpolation($"/// Number of services registered: {serviceProvider.SortedServiceList.Count}<br />\n");
        
        builder.AppendIndent(indent)
            .AppendInterpolation($"/// - Singleton: {serviceProvider.SingletonList.Count}<br />\n");
        
        builder.AppendIndent(indent)
            .AppendInterpolation($"/// - Scoped: {serviceProvider.ScopedList.Count}<br />\n");
        
        builder.AppendIndent(indent)
            .AppendInterpolation($"/// - Transient: {serviceProvider.TransientList.Count}<br />\n");
        
        builder.AppendIndent(indent)
            .AppendInterpolation($"/// - Delegate: {serviceProvider.DelegateList.Count}\n");
        
        builder.AppendIndent(indent)
            .Append("/// </para>\n");


        builder.AppendIndent(indent)
            .Append("/// <para>\n");
        
        builder.AppendIndent(indent)
            .Append(serviceProvider.GenerateScope switch {
                true => "/// This provider can create a scope,",
                false => "/// This provider has no scope,"
            })
            .Append("<br />\n");
        
        builder.AppendIndent(indent)
            .Append(generateDisposeMethods switch {
                DisposeGeneration.NoDisposing => "/// implements no Dispose methods",
                DisposeGeneration.Dispose => "/// implements only synchronous Dispose() method",
                DisposeGeneration.DisposeAsync => "/// implements only asynchronous DisposeAsync() method",
                DisposeGeneration.GenerateBoth => "/// implements both Dispose() and DisposeAsync() methods",
                _ => throw new Exception($"Invalid enum DisposeGeneration: {generateDisposeMethods}")
            })
            .Append("<br />\n");
        
        builder.AppendIndent(indent)
            .Append(threadSafe switch {
                true => "/// and is thread safe.",
                false => "/// and is not thread safe."
            })
            .Append('\n');
        
        builder.AppendIndent(indent)
            .Append("/// </para>\n");


        builder.AppendIndent(indent)
            .Append("/// </summary>\n");
    }

    /// <summary>
    /// <para>Appends service information:</para>
    /// <para>
    /// &lt;summary&gt;<br />
    /// Lifetime: {lifetime}<br />
    /// Service type: {serviceType}<br />
    /// Implementation type: {implementationType}<br />
    /// &lt;/summary&gt;
    /// </para>
    /// </summary>
    /// <param name="service"></param>
    public void AppendServiceSummary(Service service) {
        builder.AppendIndent(indent)
            .Append("/// <summary>\n");

        builder.AppendIndent(indent)
            .AppendInterpolation($"/// Lifetime: <see cref=\"global::CircleDIAttributes.{service.Lifetime.AsString()}Attribute{{TService}}\">{service.Lifetime.AsString()}</see><br />\n");
        {
            int startIndex = builder.Length + indent.Level + 37; // "/// Service type: <see cref=\global::"
            builder.AppendIndent(indent)
                .Append("/// Service type: <see cref=\"global::")
                .AppendClosedFullyQualified(service.ServiceType)
                .Replace('<', '{', startIndex, builder.Length - startIndex)
                .Replace('>', '}', startIndex, builder.Length - startIndex)
                .Append("\"/><br />\n");
        }
        {
            int startIndex = builder.Length + indent.Level + 44; // "/// Implementation type: <see cref=\"global::"
            builder.AppendIndent(indent)
                .Append("/// Implementation type: <see cref=\"global::")
                .AppendClosedFullyQualified(service.ImplementationType)
                .Replace('<', '{', startIndex, builder.Length - startIndex)
                .Replace('>', '}', startIndex, builder.Length - startIndex)
                .Append("\"/>\n");
        }
        builder.AppendIndent(indent)
            .Append("/// </summary>\n");
    }

    /// <summary>
    /// <para>Summary for CreateScope()-method and Scope-constructor:</para>
    /// <para>
    /// &lt;summary&gt;<br />
    /// Creates an instance of {classname}.Scope together with all non-lazy scoped services.<br />
    /// &lt;/summary&gt;
    /// </para>
    /// </summary>
    public void AppendCreateScopeSummary() {
        builder.AppendIndent(indent)
            .Append("/// <summary>\n");

        builder.AppendIndent(indent)
            .Append("/// Creates an instance of a ScopeProvider together with all <see cref=\"global::CircleDIAttributes.CreationTiming.Constructor\">non-lazy</see> scoped services.\n");

        builder.AppendIndent(indent)
            .Append("/// </summary>\n");
    }
}
