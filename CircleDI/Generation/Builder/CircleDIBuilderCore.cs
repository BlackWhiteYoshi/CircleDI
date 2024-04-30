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
        builder.AppendIndent(indent);
        builder.Append("/// <summary>\n");


        builder.AppendIndent(indent);
        builder.Append("/// <para>\n");

        builder.AppendIndent(indent);
        builder.Append("/// Number of services registered: ");
        builder.Append(serviceProvider.SortedServiceList.Count);
        builder.Append("<br />\n");

        builder.AppendIndent(indent);
        builder.Append("/// - Singleton: ");
        builder.Append(serviceProvider.SingletonList.Count);
        builder.Append("<br />\n");

        builder.AppendIndent(indent);
        builder.Append("/// - Scoped: ");
        builder.Append(serviceProvider.ScopedList.Count);
        builder.Append("<br />\n");

        builder.AppendIndent(indent);
        builder.Append("/// - Transient: ");
        builder.Append(serviceProvider.TransientList.Count);
        builder.Append("<br />\n");

        builder.AppendIndent(indent);
        builder.Append("/// - Delegate: ");
        builder.Append(serviceProvider.DelegateList.Count);
        builder.Append('\n');

        builder.AppendIndent(indent);
        builder.Append("/// </para>\n");


        builder.AppendIndent(indent);
        builder.Append("/// <para>\n");

        builder.AppendIndent(indent);
        builder.Append(serviceProvider.GenerateScope switch {
            true => "/// This provider can create a scope,",
            false => "/// This provider has no scope,"
        });
        builder.Append("<br />\n");

        builder.AppendIndent(indent);
        builder.Append(generateDisposeMethods switch {
            DisposeGeneration.NoDisposing => "/// implements no Dispose methods",
            DisposeGeneration.Dispose => "/// implements only synchronous Dispose() method",
            DisposeGeneration.DisposeAsync => "/// implements only asynchronous DisposeAsync() method",
            DisposeGeneration.GenerateBoth => "/// implements both Dispose() and DisposeAsync() methods",
            _ => throw new Exception($"Invalid enum DisposeGeneration: {generateDisposeMethods}")
        });
        builder.Append("<br />\n");

        builder.AppendIndent(indent);
        builder.Append(threadSafe switch {
            true => "/// and is thread safe.",
            false => "/// and is not thread safe."
        });
        builder.Append('\n');

        builder.AppendIndent(indent);
        builder.Append("/// </para>\n");


        builder.AppendIndent(indent);
        builder.Append("/// </summary>\n");
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
        builder.AppendIndent(indent);
        builder.Append("/// <summary>\n");

        builder.AppendIndent(indent);
        builder.Append("/// Lifetime: <see cref=\"global::CircleDIAttributes.");
        builder.Append(service.Lifetime.AsString());
        builder.Append("Attribute{TService}\">");
        builder.Append(service.Lifetime.AsString());
        builder.Append("</see><br />\n");

        builder.AppendIndent(indent);
        builder.Append("/// Service type: <see cref=\"global::");
        {
            int startIndex = builder.Length;
            builder.AppendClosedFullyQualified(service.ServiceType);
            builder.Replace('<', '{', startIndex, builder.Length - startIndex);
            builder.Replace('>', '}', startIndex, builder.Length - startIndex);
        }
        builder.Append("\"/><br />\n");

        builder.AppendIndent(indent);
        builder.Append("/// Implementation type: <see cref=\"global::");
        {
            int startIndex = builder.Length;
            builder.AppendClosedFullyQualified(service.ImplementationType);
            builder.Replace('<', '{', startIndex, builder.Length - startIndex);
            builder.Replace('>', '}', startIndex, builder.Length - startIndex);
        }
        builder.Append("\"/>\n");

        builder.AppendIndent(indent);
        builder.Append("/// </summary>\n");
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
        builder.AppendIndent(indent);
        builder.Append("/// <summary>\n");

        builder.AppendIndent(indent);
        builder.Append("/// Creates an instance of a ScopeProvider together with all <see cref=\"global::CircleDIAttributes.CreationTiming.Constructor\">non-lazy</see> scoped services.\n");

        builder.AppendIndent(indent);
        builder.Append("/// </summary>\n");
    }
}
