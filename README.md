# CircleDI

The world only full-power circular Service Provider.

- [Resolves Circular Dependencies](#resolves-circular-dependencies)
- [Optimal Performance](#optimal-performance)
- [Compile Time Safety](#compile-time-safety)
- [No Runtime Dependencies](#no-runtime-dependencies)
- [AOT friendly](#aot-friendly)
- [Beautiful Code Generation](#beautiful-code-generation)
- [Non-Generic](#non-generic)
- [Non-Lazy Instantiation](#non-lazy-instantiation)
- [Object Oriented](#object-oriented)
- [Customizable](#customizable)
- [Easy to Use](#easy-to-use)

```csharp
/**
 * MyService1 has dependency on MyService2
 * MyService2 has dependency on MyService1
 *
 *  --------------         --------------
 *  |            | ------> |            |
 *  | MyService1 |         | MyService2 |
 *  |            | <------ |            |
 *  --------------         --------------
 *
 **/

using CircleDIAttributes;


[ServiceProvider]
[Singleton<IMyService1, MyService1>]
[Singleton<IMyService2, MyService2>]
public partial class CircleExampleProvider;


public interface IMyService1;
public class MyService1(IMyService2 myService2) : IMyService1;

public interface IMyService2;
public class MyService2 : IMyService2 {
    public required IMyService1 MyService1 { private get; init; }
}



/**
 * MyService has dependency on MyService (not meaningful, but it resolves)
 *
 *    ---------
 *    |       |
 *    |       v
 *  -------------
 *  |           |
 *  | MyService |
 *  |           |
 *  -------------
 *
 **/

[ServiceProvider]
[Singleton<MyService>]
public partial class SelfCircleExampleProvider;

public class MyService {
    public required MyService MyServiceCircle { private get; init; }
}

```


<br></br>
## Requirements

- Language Version C#12 (default of .NET 8)
  - if you are using an older TargetFramework:
    - **.NET**
    => just set the language version to at least C#12.
    - **.NET Standard 2.1**
    => together with the LangVersion requirement you also need some polyfills, I recommend using [PolySharp](https://github.com/Sergio0694/PolySharp).
    - **.NET Framework, UWP, .NET Standard 2.0**
    => LangVersion C#12 or newer, polyfills ([PolySharp](https://github.com/Sergio0694/PolySharp)) and [disable DisposeAsync generation](Readme_md/ConfigurationAndCustomization.md#attributes-and-their-properties-on-service-provider).


<br></br>
## Get Started

1. Add PackageReference to your .csproj file.

```xml
<ItemGroup>
  <PackageReference Include="CircleDI" Version="{latest version}" PrivateAssets="all" />
</ItemGroup>
```

2. Create a partial class with the [ServiceProviderAttribute](Readme_md/TypeTables.md#serviceproviderattribute) and register a singleton service.

```csharp
using CircleDIAttributes;

[ServiceProvider]
[Singleton<IMyService, MyService>]
public partial class MyFirstServiceProvider;


public interface IMyService;

public class MyService : IMyService;
```

3. Create an Instance and use it.

```csharp
public static class Program {
    public static void Main() {
        // instantiate ServiceProvider
        MyFirstServiceProvider serviceProvider = new();
        // get Service
        MyService myService = serviceProvider.MyService;
    }
}
```

### Dependency Injection

- Constructor Parameter

```csharp
public class MyService : IMyService {
    private readonly IService1 _service1;
    private readonly IService2 _service2;
    
    public MyService(IService1 service1, IService2 service2) {
        _service1 = service1;
        _service2 = service2;
    }
}
```

-  Primary Constructor

```csharp
public class MyService(IService1 service1, IService2 service2) : IMyService;
```

- Property Member

```csharp
public class MyService : IMyService {
    public required IService1 Service1 { private get; init; }
    public required IService2 Service2 { private get; init; }
}
```

<br></br>
## [Get Started - Blazor](Readme_md/Blazor.md#GetStarted)

## [Get Started - MinimalAPI](Readme_md/MinimalAPI.md#GetStarted)


<br></br>
## Examples

[Comparison to Microsoft.Extensions.DependencyInjection.ServiceCollection](Readme_md/ComparisonToServiceCollection.md)

[Source Code Generation Output](Readme_md/SourceCodeGenerationOutput.md)


<br></br>
## Configuration and Customization

- [Attributes and Attribute Properties](Readme_md/ConfigurationAndCustomization.md#attributes-and-attribute-properties)
  - [Attributes and their Properties on Service Provider](Readme_md/ConfigurationAndCustomization.md#attributes-and-their-properties-on-service-provider)
  - [DependencyAttribute](Readme_md/ConfigurationAndCustomization.md#dependencyattribute)
  - [ConstructorAttribute](Readme_md/ConfigurationAndCustomization.md#constructorattribute)
  - [ImportAttribute](Readme_md/ConfigurationAndCustomization.md#importattribute)
- [Specific Attributes in Depth](Readme_md/ConfigurationAndCustomization.md#specific-attributes-in-depth)
  - [Implementation Property](Readme_md/ConfigurationAndCustomization.md#implementation-property)
  - [Named Services](Readme_md/ConfigurationAndCustomization.md#named-services)
  - [Generic Services](Readme_md/ConfigurationAndCustomization.md#generic-services)
- [Implicit Configurations](Readme_md/ConfigurationAndCustomization.md#implicit-configurations)
  - [Name, Namespace, Modifiers, Containing Types and Generic](Readme_md/ConfigurationAndCustomization.md#name-namespace-modifiers-containing-types-and-generic)
  - [Interface name, namespace, access modifier, containing types and generic](Readme_md/ConfigurationAndCustomization.md#interface-name-namespace-access-modifier-containing-types-and-generic)
  - [Overwriting default services](Readme_md/ConfigurationAndCustomization.md#overwriting-default-services)
  - [Custom Constructor](Readme_md/ConfigurationAndCustomization.md#custom-constructor)
  - [Custom Dispose](Readme_md/ConfigurationAndCustomization.md#custom-dispose)
  - [Struct Types and Native/Built-in Types](Readme_md/ConfigurationAndCustomization.md#struct-types-and-nativebuilt-in-types)
- [Workarounds for not supported Features](Readme_md/ConfigurationAndCustomization.md#workarounds-for-not-supported-features)
  - [Async Constructor](Readme_md/ConfigurationAndCustomization.md#async-constructor)
  - [Decoration](Readme_md/ConfigurationAndCustomization.md#decoration)
  - [Nullable Service](Readme_md/ConfigurationAndCustomization.md#nullable-service)
- [Remarks](Readme_md/ConfigurationAndCustomization.md#remarks)
  - [IServiceProvider Interface](Readme_md/ConfigurationAndCustomization.md#iserviceprovider-interface)
  - [Error Handling](Readme_md/ConfigurationAndCustomization.md#error-handling)


<br></br>
## Attributes and Enums

- [ServiceProviderAttribute](Readme_md/TypeTables.md#serviceproviderattribute)
- [ServiceProviderAttribute&lt;TInterface&gt;](Readme_md/TypeTables.md#serviceproviderattributetinterface)
- [ScopedProviderAttribute](Readme_md/TypeTables.md#scopedproviderattribute)
- [SingletonAttribute](Readme_md/TypeTables.md#singletonattribute)
- [ScopedAttribute](Readme_md/TypeTables.md#scopedattribute)
- [TransientAttribute](Readme_md/TypeTables.md#transientattribute)
- [DelegateAttribute](Readme_md/TypeTables.md#delegateattribute)
- [DependencyAttribute](Readme_md/TypeTables.md#dependencyattribute)
- [ConstructorAttribute](Readme_md/TypeTables.md#constructorattribute)
- [CreationTiming (enum)](Readme_md/TypeTables.md#creationtiming-enum)
- [GetAccess (enum)](Readme_md/TypeTables.md#getaccess-enum)
- [DisposeGeneration (enum)](Readme_md/TypeTables.md#disposegeneration-enum)


<br></br>
## Features

### Resolves Circular Dependencies

Singleton and Scoped Dependencies can be injected everywhere and it will not count as dependency circles.
The only requirement is to inject these dependencies as properties.

### Optimal Performance

No boilerplate, no overhead, no unnecessary if-checks, only just the necessary operations to provide the service.
CircleDI aims for optimal performance.

### Compile Time Safety

The dependency tree is resolved at compile time and emits compile errors, when something is missing.
Also because of the nature of [non-generic](#non-generic) it is not possible to retrieve a service that is not registered.

### No Runtime Dependencies

This NuGet package is only a source generator, so it is a development dependency.
Only the generated code gets in your binary and that is not much.

### AOT friendly

It has [no runtime dependencies](#no-runtime-dependencies) and the generated code uses no reflection or other AOT-critical statements.
Actually the generated code is quite simple.
However, to resolve circular init-only dependency injection, [UnsafeAccessor](https://learn.microsoft.com/en-us/dotnet/api/system.runtime.compilerservices.unsafeaccessorattribute) are used.

### Beautiful Code Generation

Every indentation, every line break, every space is intended.
Additionally the code is quite easy to read, it is mostly just getters and constructor calls.
You can also set a breakpoint and just step into to see step by step what is happening.

### Non-Generic

Instead of the generic *GetService&lt;T&gt;()* method, for every service a dedicated getter-method is generated.
This is not only more performant and has better IntelliSense guidance, it also prevents accessing a service that is not registered.

### Non-Lazy Instantiation

Singleton/Scoped services are instantiated within the constructor of the Service Provider.
You can also configure each service to instantiate when it is first time requested (lazy).
Both approaches have advantages and disadvantages, you choose.


### Object Oriented

No global definition or state.
You can define as many different Service Provider classes you want and instantiate each defined Provider as often as you need.
Additional, it automatically creates for itself and the containing *Scope* class an interface respectively to support loose coupling and polymorphism.

### Customizable

You can configure a lot if you want:
You can decide the [class name](Readme_md/ConfigurationAndCustomization.md#name-namespace-and-modifier) of the Service Provider,
set the name of the [interface](Readme_md/ConfigurationAndCustomization.md#attributes-and-their-properties-on-service-provider),
set the [namespace and modifiers](Readme_md/ConfigurationAndCustomization.md#name-namespace-and-modifier) of the class,
provide your own [constructor](Readme_md/ConfigurationAndCustomization.md#custom-constructor) and [dispose-methods](Readme_md/ConfigurationAndCustomization.md#custom-dispose),
change the name of [default services](Readme_md/ConfigurationAndCustomization.md#overwriting-default-services),
decide for each service the [creation timing and get accessor](Readme_md/ConfigurationAndCustomization.md#attributes-and-their-properties-on-service-provider),
[toggle the generation](Readme_md/ConfigurationAndCustomization.md#attributes-and-their-properties-on-service-provider) of the *Scope* class or *Dispose()*-methods,
or make the provider [not thread safe](Readme_md/ConfigurationAndCustomization.md#attributes-and-their-properties-on-service-provider) for better performance.

### Easy to Use

CircleDI has many default configurations, so you can just [get started](#get-started) with minimal effort.
But if you want, you can change the behavior of all services by changing a single property at the [ServiceProviderAttribute](Readme_md/TypeTables.md#serviceproviderattribute) or overwrite the behavior of a specific service by changing a property at that [ServiceProviderAttribute](Readme_md/TypeTables.md#serviceproviderattribute).


<br></br>
## Comparison to [Jab](https://github.com/pakrym/jab)

CircleDI is heavily inspired by Jab and has many similarities, but there are a handful of improvements:

 - CircleDI allows also property dependency injection and based on that also [circular dependency injection](#resolves-circular-dependencies).
 - [Non-Generic](#non-generic): Instead of *GetService&lt;T&gt;()*, CicleDI generates dedicated getter-methods.
 - [Non-Lazy Instantiation](#non-lazy-instantiation): Jab only creates services lazy, CircleDI is configurable to create a service within the constructor or lazy.
 - CircleDI has [less boilerplate](#optimal-performance) (unnecessary classes/methods and if-checks) than Jab.
 - CircleDI is more [customizable](#customizable)

However, the idea of circular dependency resolving was inspired by razor components.
The mechanism for injecting dependencies at razor components works differently.
A component is first instantiated with the parameterless constructor and then the dependencies are injected to the marked properties.
So razor components are also capable of resolving circular dependencies, however, everything is done via reflection.

The performance difference is negligible.
In benchmark testing the difference shows only a few nano-seconds and about 20 to 40 bytes memory-allocations difference.
In most simple tests CircleDI is a few nanoseconds faster, but has more memory-allocations.
The increased memory allocation is because of constructing some objects at instantiation (singleton/scoped-services, dispose-list) instead lazily and the increased execution speed is probably because of [non-generic](#non-generic) and less if-checks.


<br></br>
## Disable Attribute Generation

You can disable the generation of the attributes by defining a constant for your compilation:

```xml
  <PropertyGroup>
    <DefineConstants>CIRCLEDI_EXCLUDE_ATTRIBUTES</DefineConstants>
  </PropertyGroup>
```

This functionality is specific for the use case when you have a project referencing another project, both projects using this generator and you have <i>InternalsVisibleTo</i> enabled.
In that case you have the attributes defined twice in your referencing project and you get a warning about that.
By defining this constant in your referencing project, you prevent one generation, so the attributes are only defined once in the referenced project.


<br></br>
## Versions

### CircleDI

- 0.1.0  
  - first version, includes all basic functionalities for generating a Service Provider
- 0.2.0  
  - breaking change: *CreateScope()* lists dependencies without [Dependency]-attribute as parameters and dependencies with [Dependency]-attribute are supplied by the ServiceProvider  
  - native/built-in types are supported
- 0.3.0  
  - added support for passing value type services by reference  
  - ServiceProvider can now also be struct, record or record struct
- 0.4.0  
  - added "NoDispose"-property to disable disposing for each distinct service  
  - added functionality for omitting interface generation when *InterfaceName* is empty  
  - added Attribute *ServiceProvider&lt;TInterface&gt;* to generate the interface into an existing one
- 0.5.0  
  - added CicleDI.Blazor  
  - removed Error *CDI029* "Dependency CreationTiming: Constructor on Lazy" and instead the lazy instantiated service will become constructor instantiated
- 0.6.0  
  - added Minimal.API  
  - added support for ServiceProvider being generic  
  - improved IServiceProvder.GetService(Type) method
- 0.7.0  
  - added ImportAttribute
- 0.8.0  
  - added ComponentModuleAttribute for cross project razor components importing  
  - breaking change: ServiceProvider does not longer generate TransientAttributes, add ComponentModuleAttribute to the ServiceProvider to get the same behavior as before  
  - improved service tree generation
- 0.9.0  
  - support for registering services with *typeof()*  
  - registering open/unbound generic services
- 0.9.1  
  - small breaking change: The error-id of most error messages got changed  
  - small breaking change: Modules implementing IDisposable/IAsyncDisposable imported as Service get disposed  
  - dedicated lock-objects instead of locking provider or lists itself  
  - some fixes for specific cases
<br></br>
- 1.0  
  - changed Blazor ComponentActivator to support the default constructor dependency injection  
  - changed lock type to System.Threading.Lock when available
- 1.1  
  - added polyfill for System.Threading.Lock for .NET8 backwards compatibility
