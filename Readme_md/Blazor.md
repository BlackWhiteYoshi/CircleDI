# CircleDI.Blazor

CircleDI wired up with Blazor. The extra Blazor features are

- CircleDIComponentActivator
- Automatic component registration as transient services.
- Default services of *Microsoft.Extensions.DependencyInjection* provider get added.

This package includes all functionalities of CircleDI and including also the plain CircleDI package will cause errors.


<br></br>
## Get Started

1. Add PackageReference to your .csproj file.

```xml
<ItemGroup>
  <PackageReference Include="CircleDI.Blazor" Version="{latest version}" PrivateAssets="all" />
</ItemGroup>
```

2. Create a partial class with the [ServiceProviderAttribute](TypeTables.md#serviceproviderattribute).

```csharp
using CircleDIAttributes;

[ServiceProvider]
public partial class MyBlazorServiceProvider;
```

3. Register to the built-in provider the ServiceProvider as Singleton, the ScopeProvider as Scoped and the CircleDIComponentActivator with your ScopeProvider also as Scoped.

```csharp
using CircleDIAttributes;

...
builder.Services.AddSingleton<IMyBlazorServiceProvider, MyBlazorServiceProvider>();
builder.Services.AddScoped<IMyBlazorServiceProvider.IScope, MyBlazorServiceProvider.Scope>();
builder.Services.AddScoped<IComponentActivator, CircleDIComponentActivator<IMyBlazorServiceProvider.IScope>>();
```

4. Create a razor component with a code behind file and inject a dependency via constructor, primary constructor or required property.

```csharp
using Microsoft.AspNetCore.Components;

public partial class Home(NavigationManager navigationManager) : ComponentBase {
    protected override void OnInitialized() {
        Console.WriteLine(navigationManager.Uri);
    }
}
```


<br></br>
## Custom Constructor -> Custom Registration

You can also define a constructor inside your ServiceProvider/ScopeProvider that takes parameter.
In that case you register the provider with the method overload that takes the *implementationFactory* parameter.

```csharp
using CircleDIAttributes;

public partial interface IMyBlazorServiceProvider;

[ServiceProvider]
public sealed partial class MyBlazorServiceProvider {
    private readonly string _connectionString;

    public MyBlazorServiceProvider(string connectionString, IServiceProvider builtinProvider) {
        _connectionString = connectionString;
        InitServices(builtinProvider);
    }


    public sealed partial class Scope {
        private readonly string _connectionStringScoped;

        public Scope(string connectionStringScoped, [Dependency] IMyBlazorServiceProvider serviceProvider, IServiceProvider builtinProvider) {
            _connectionStringScoped = connectionStringScoped;
            InitServices(serviceProvider, builtinProvider);
        }
    }
}

...

string connectionString = GetConnectionString();
string connectionStringScoped = GetConnectionStringScoped();

builder.Services.AddSingleton<IMyBlazorServiceProvider>((IServiceProvider sp) => new MyBlazorServiceProvider(connectionString, sp));
builder.Services.AddScoped<IMyBlazorServiceProvider.IScope>((IServiceProvider sp) => sp.GetRequiredService<IMyBlazorServiceProvider>().CreateScope(connectionStringScoped, sp));
builder.Services.AddScoped<IComponentActivator, CircleDIComponentActivator<IMyBlazorServiceProvider.IScope>>();
```


<br></br>
## Add Razor Components

The [ServiceProviderAttribute](#serviceproviderattribute) has two additional properties: [DefaultServiceGeneration](#default-service-generation) and *AddRazorComponents*.
When *AddRazorComponents* is enabled, razor components (classes that inherit from [ComponentBase](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.componentbase)) are automatically registered as a service.
The default of *AddRazorComponents* is true (enabled).

All components are registered with lifetime Transient and disposing ignored, they get disposed by the framework.

When a component is registered manually, the component will not be added again.

The automatic detection of components is limited to the same project and only classes that explicitly inherit from [ComponentBase](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.componentbase) (or derived of ComponentBase) will be detected, *.razor*-files are ignored.
This is because at the point of generation the Blazor compiler has not generated the classes from the *.razor*-files yet.  
However, you can always register components by yourself.


<br></br>
## Default Service Generation

The [ServiceProviderAttribute](#serviceproviderattribute) has two additional properties: *DefaultServiceGeneration* and [AddRazorComponents](#add-razor-components).
The *DefaultServiceGeneration* can have multiple values depending in which environment your Blazor application is running you should use the appropriate one:
 
  - **Blazor Hybrid**: That environment has the least amount of default services. All these services are also available in the other environments.
  - **Blazor Webassembly**: All default services that are available in webassembly. Has some unique services (*LazyAssemblyLoader*, *WebAssemblyHostEnvironment*) that are only available in this environment.
  - **Blazor Server**: Has over one hundred of default services, most of them are omitted.
  - **Blazor Server and Webassembly**: All services that are compatible for server and webassembly environment are generated. This is the default.
  - **None**: No default services are generated.

  
Additionally, if enabled (other value than **None**), a *System.IServiceProvider* parameter gets added to the constructor to wire up the default services.

The singleton services have CreationTime constructor and the scoped services have CreationTime lazy.

When a service with the same lifetime and service type as a default service is registered, the corresponding default service will not be generated.

There is no guarantee that all default services are present, especially not in the server environment. Services that are unbound generic (e.g. ILogger<>) are not present yet because of missing support of [Open/Unbound Generic Types](ConfigurationAndCustomization.md#openunbound-generic-types).


<br></br>
## Remarks

### Framework Injection and Parameters

This package does not disable the built-in service provider or it's injection mechanism,
so the normal [Inject]/[Parameter]/[CascadingParameter] works as expected.
When using [Parameter]/[CascadingParameter] you must not use the *required* keyword,
otherwise CircleDI is forced to dependency inject that parameter only to get overridden by the framework afterwards.
To avoid nullable warnings, just assign a "null!" to the property.

<br></br>
### Manually Registering Components

There are some gotcha when working with components as service.

<br></br>
#### Interface Service Type
Using a component registered with an interface service type is possible, only the Blazor compiler does not like it, the framework supports everything that implements [IComponent](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.icomponent):

```razor
<IMyComponent> // does not work, gives error

// manually writing the above code just works
@{
    __builder.OpenComponent<IMyComponent>(0);
    __builder.CloseComponent();
}
```

<br></br>
#### Lifetime Singleton/Scoped

When registering a component with lifetime Singleton/Scoped, you get problems with the [RenderHandle](https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.components.renderhandle) of the component because of poorly implementation:

```csharp
// https://source.dot.net/#Microsoft.AspNetCore.Components/ComponentBase.cs,6cb4bd858fb66bb1

void IComponent.Attach(RenderHandle renderHandle)
{
    // This implicitly means a ComponentBase can only be associated with a single
    // renderer. That's the only use case we have right now. If there was ever a need,
    // a component could hold a collection of render handles.
    if (_renderHandle.IsInitialized)
    {
        throw new InvalidOperationException($"The render handle is already set. Cannot initialize a {nameof(ComponentBase)} more than once.");
    }
 
    _renderHandle = renderHandle;
}
```

You actually need a collection of RenderHandles in this case.
Because of the lifetime you can render the same component in different places.  
But if you intend to render the component only once at a time, you can use the following base class:

```csharp
using System.Runtime.CompilerServices;

/// <summary>
/// Same as <see cref="ComponentBase"/>, but<br />
/// - doesn't do the reassign check in <see cref="IComponent.Attach(RenderHandle)"/><br />
/// - has a Dispose() method that resets the <see cref="RenderHandle"/><br />
/// - provide Property <see cref="HasRenderHandle"/>
/// </summary>
public abstract class ServiceComponentBase : ComponentBase, IComponent, IDisposable {
    [UnsafeAccessor(UnsafeAccessorKind.Field, Name = "_renderHandle")]
    private extern static ref RenderHandle GetRenderHandle(ComponentBase @this);

    private RenderHandle RenderHandle {
        get => GetRenderHandle(this);
        set => GetRenderHandle(this) = value;
    }


    protected bool HasRenderHandle => RenderHandle.IsInitialized;

    void IComponent.Attach(RenderHandle renderHandle) => RenderHandle = renderHandle;

    public virtual void Dispose() => RenderHandle = default;
}
```

<br></br>
#### Dispose Singleton/Scoped

The lifecycle methods of the renderer include also *Dispose()*, that means each time a component gets no longer rendered, the Dispose()-method is executed.  
So be careful when implementing *IDisposable*/*IAsyncDisposable*, otherwise your component could be disposed way earlier than intended.
To differentiate between disposed from renderer or disposed from service provider you can check the RenderHandle:

```csharp
public class MyScopedComponent : ServiceComponentBase {
    public override void Dispose() {
        íf (HasRenderHandle) {
            base.Dispose();
            // logic for disposing when disposed by renderer (when no longer be rendered)
        }
        else {
            // logic for disposing when lifetime ends (when service provider gets disposed)
        }
    }
}
```


<br></br>
## Additional/Changed Types

<br></br>
### ServiceProviderAttribute / ServiceProviderAttribute&lt;TInterface&gt;

*Same as [ServiceProviderAttribute](TypeTables.md#serviceproviderattribute) + following properties*:

#### Properties

| **Name**                 | **Type**                                            | **Description**                                                                                                                                                                                                                                                                                                                                                                                                                                                                   |
| ------------------------ | --------------------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| DefaultServiceGeneration | [BlazorServiceGeneration](#blazorservicegeneration) | Toggles the generation of default services from the built-in service provider. It can be configured to have only services that are available in all environments, all services for a specific environment or disable generating any default services. If enabled, it also adds a [System.IServiceProvider](https://learn.microsoft.com/en-us/dotnet/api/system.iserviceprovider) parameter to the constructor parameters. Default is BlazorServiceGeneration.ServerAndWebassembly. |
| AddRazorComponents       | bool                                                | Decides whether classes derived from <see cref="Microsoft.AspNetCore.Components.ComponentBase"/> are automatically registered or not. Default is true.                                                                                                                                                                                                                                                                                                                            |


<br></br>
### CircleDIComponentActivator&lt;TScopeProvider&gt;

An implementation of the activator that can be used to instantiate components.

This activator will retrieve the component from the specified service provider if registered.  
If the type is not registered, the component will be instantiated with the parameterless constructor (or fails in an Exception).  
If the type is registered multiple times, it also fails in an Exception because of ambiguity.

After this instantiation the framework performs it's dependency injection *[Inject]* and parameter injection *[Parameter]*.

#### Type Parameters

| **Name**       | **TypeConstraints** |  **Description**                                                                                                                                                                                                                                                                                           |
| -------------- | ------------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| TScopeProvider | *none*              | Type of the service provider which is used to retrieve components. The object will be dependency injected from the built-in service provider. The object must implement [System.IServiceProvider](https://learn.microsoft.com/en-us/dotnet/api/system.iserviceprovider), otherwise an error will be thrown. |

#### Methods

| **Name**       | **Parameters**     | **ReturnType** | **Description**                                                                                                                                                                                                                                                                                                  |
| -------------- | ------------------ | -------------- | ---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| CreateInstance | Type componentType | IComponent     | It will retrieve the component from the specified service provider if registered. If the type is not registered, the component will be instantiated with the parameterless constructor (or fails in an Exception). If the type is registered multiple times, it also fails in an Exception because of ambiguity. |


<br></br>
### BlazorServiceGeneration (enum)

Configuration for generating getter methods for the default services from the built-in service provider.

The built-in service provider has some services registered only in specific environments.  
It can be configured to have only services that are available in all environments, all services for a specific environment or disable generating any default services.

#### Members

| **Name**             | **Description**                                                                                                       |
| -------------------- | --------------------------------------------------------------------------------------------------------------------- |
| None                 | No default services will be generated.                                                                                |
| Webassembly          | All Blazor Webassembly default services will be generated.                                                            |
| Server               | All Blazor Server-side default services will be generated.                                                            |
| Hybrid               | Blazor Hybrid has the least default services and all these services also works in server and webassembly environment. |
| ServerAndWebassembly | All default services that are available in server and webassembly environment.                                        |
