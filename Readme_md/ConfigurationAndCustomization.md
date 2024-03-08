# Configuration and Customization

- [Attributes and Attribute Properties](#attributes-and-attribute-properties)
  - [Attributes and their Properties on Service Provider](#attributes-and-their-properties-on-service-provider)
  - [DependencyAttribute](#dependencyattribute)
  - [ConstructorAttribute](#constructorattribute)
  - [ImportAttribute](#importattribute)
- [Specific Attributes in Depth](#specific-attributes-in-depth)
  - [Implementation Property](#implementation-property)
  - [Named Services](#named-services)
- [Implicit Configurations](#implicit-configurations)
  - [Name, Namespace and Modifier](#name-namespace-and-modifier)
  - [Overwriting default services](#overwriting-default-services)
  - [Custom Constructor](#custom-constructor)
  - [Custom Dispose](#custom-dispose)
  - [Struct](#struct)
- [Workarounds for not supported Features](#workarounds-for-not-supported-features)
  - [Async Constructor](#async-constructor)
  - [Decoration](#decoration)
  - [Open/Unbound Generic Types](#openunbound-generic-types)
- [Remarks](#remarks)
  - [IServiceProvider Interface](#iserviceprovider-interface)
  - [Error Handling](#error-handling)


<br></br>
## Attributes and Attribute Properties

There are 6 attributes that are used at your ServiceProvider class and 2 other attributes that are used at service classes:

- [ServiceProviderAttribute](#attributes-and-their-properties-on-service-provider)
- [ScopedProviderAttribute](#attributes-and-their-properties-on-service-provider)
  - [SingletonAttribute](#attributes-and-their-properties-on-service-provider)
  - [ScopedAttribute](#attributes-and-their-properties-on-service-provider)
  - [TransientAttribute](#attributes-and-their-properties-on-service-provider)
  - [DelegateAttribute](#attributes-and-their-properties-on-service-provider)
<br></br>
- [DependencyAttribute](#dependencyattribute)
- [ConstructorAttribute](#constructorattribute)


<br></br>
### Attributes and their Properties on Service Provider

The main usage of the [ServiceProviderAttribute](Readme_md/TypeTables.md#serviceproviderattribute) is to indicate that the class is a ServiceProvider, so the generator will generate code for that class.  
The [ScopedProviderAttribute](Readme_md/TypeTables.md#scopedproviderattribute) without setting any properties does nothing.  
The other 4 attributes are for registering services.  
There are several properties that can be configured to change the generated code:

- *InterfaceName*:
Together with the ServiceProvider an interface based on the provider will be generated and the ServiceProvider implements it.
This sets the name/identifier of the generated interface explicitly. The default name is "I{classname}".

- *CreationTime*:
Sets when the instantiation of a service happens: at ServiceProvider instantiation (inside the constructor) or lazy (first time used).
This option is available at the [ServiceProviderAttribute](Readme_md/TypeTables.md#serviceproviderattribute) to set all services, at the [ScopedProviderAttribute](Readme_md/TypeTables.md#scopedproviderattribute) to set all scoped services or at a [registration attribute](Readme_md/TypeTables.md#singletonattribute) to set this option for that specific service.
It is structured hierarchically: Specific service settings take priority over ScopedProvider settings and ScopedProvider settings take priority over ServiceProvider settings.  
Note that [TransientAttribute](Readme_md/TypeTables.md#transientattribute) and [DelegateAttribute](Readme_md/TypeTables.md#delegateattribute) do not have a CreationTiming, this applies only for [SingletonAttribute](Readme_md/TypeTables.md#singletonattribute) and [ScopedAttribute](Readme_md/TypeTables.md#scopedattribute) services.

- *GetAccessor*:
The type of the member to access the service: tt can be either a get property or a parameterless method.
This option is available at the [ServiceProviderAttribute](Readme_md/TypeTables.md#serviceproviderattribute) to set all services, at the [ScopedProviderAttribute](Readme_md/TypeTables.md#scopedproviderattribute) to set all scoped services or at a [registration attribute](Readme_md/TypeTables.md#singletonattribute) to set this option for that specific service.
It is structured hierarchically: Specific service settings take priority over ScopedProvider settings and ScopedProvider settings take priority over ServiceProvider settings. 

- *Implementation*:
If nothing is specified the constructor is used to create the service, but you can also use a field to provide an instance or a property/method to use a factory method instead.
For in depth explanation see [Implementation Property](#implementation-property).

- *Name*:
Sets the name/identifier of the service getter property/method explicitly. This can be used to avoid naming conflicts, configure named services or just for better naming.
For in depth explanation see [Named Services](#named-services).

- *GenerateDisposeMethods*:
If nothing is specified both Dispose() and DisposeAsync() methods are generated to implement IDisposable and IAsyncDisposable.
You can configure this property to only generate one method or omit both.

- *Generate* (ScopeProvider):
Toggles the generation off the Scope Class inside the ServiceProvider.
If turned off, no Scope is generated and therefore <see cref="ScopedAttribute"/> has no effect.

- *ThreadSafe*:
Toggle for generating lock()-statements to make the ServiceProvider thread safe or omitting those for better performance.
This option should be set to false, if the ServiceProvider is used in a single threaded scenario or only accessed by one thread at a time.
Default is generating lock()-statements.


<br></br>
### DependencyAttribute

This attribute can be used on parameters and properties.
You can mark a non-required property as dependency by decorating it with this attribute.

```csharp
using CircleDIAttributes;

namespace MySpace;

public sealed class MyServiceImplementation {
    [Dependency]
    public OtherService otherService { get; set; } = OtherService.Default;
}
```

Another usage is for Named Services.
When at the [DependencyAttribute](Readme_md/TypeTables.md#dependencyattribute) the *Name*-property is specified, the service will be injected by name instead by service type.
For in depth explanation see [Named Services](#named-services).


<br></br>
### ConstructorAttribute

This attribute is only needed if a service implementation has 2 or more constructors and the service is not constructed via [Implementation](#implementation-property).
In this case you have to specify one constructor which should be used to create the service.
This is done by marking the constructor with this attribute.


```csharp
using CircleDIAttributes;

namespace MySpace;

// marks the primary constructor for creating this service
[method: Constructor]
public sealed class MyServiceImplementation(string name, int age) {
    public MyServiceImplementation(string name) : this(name, -1) { }
}
```

```csharp
using CircleDIAttributes;

namespace MySpace;

public sealed class MyServiceImplementation(string name, int age) {
    // marks this constructor for creating this service
    [Constructor]
    public MyServiceImplementation(string name) : this(name, -1) { }
}
```


<br></br>
### ImportAttribute

Not implemented yet


<br></br>
## Specific Attributes in Depth


<br></br>
### Implementation Property

The *Implementation* property can contain the name/identifier of a field, property or method, typically supplied with *nameof()*.
If this property is not used, the constructor is used to create an implementation.

If a field is provided, that instance of the field is used. (for singleton/scoped services only)

```csharp
[ServiceProvider]
[Singleton<IService, Service>(Implementation = nameof(_service))]
public sealed partial class MyProvider {
    private readonly Service _service;

    public MyProvider(Service service) {
        _service = service;
        InitServices();
    }
}

public interface IService;
public sealed class Service : IService;
```

If a property or method is provided, that factory method is executed to create an implementation.

```csharp
[ServiceProvider]
[Singleton<IService, Service>(Implementation = nameof(CreateService))]
public sealed partial class MyProvider {
    private Service CreateService => new Service();
}

public interface IService;
public sealed class Service : IService;
```

If a method is provided and the method has parameters, these parameters will be dependency injected.  

```csharp
[ServiceProvider]
[Singleton<IService, Service>(Implementation = nameof(CreateService))]
[Singleton<INameProvider, NameProvider>]
public sealed partial class MyProvider {
    private Service CreateService(INameProvider nameProvider) => new Service(nameProvider.Name);
}

public interface IService;
public sealed class Service(string name) : IService {
    public string Name => name;
}

public interface INameProvider {
    string Name { get; }
}
public sealed class NameProvider : INameProvider {
    public string Name => "BlackWhiteYoshi";
}
```

Note:  
Do not use the ServiceProvider itself as dependency to resolve your dependencies with that parameter,
otherwise there will be no guarantee of order correctness, so the services might not be initialized at that point.

The special keyword "this" is also supported and is treated like a field, see [Overwriting default services](#overwriting-default-services) for an example.


<br></br>
### Named Services

The default name/identifier of a service is the name of the implementation class.

```csharp
[ServiceProvider]
[Singleton<IService, Service1>]     // has name "Service1"
[Singleton<IService, Service2>]     // has name "Service2"
internal sealed partial class MyProvider;

public interface IService;
public sealed class Service1 : IService;
public sealed class Service2 : IService;
```

The name can be set explicitly by using the *Name* property.
This can be used to avoid naming conflicts.

```csharp
[ServiceProvider]
[Singleton<IService, Service>(Name = "Service1")]
[Singleton<IService2, Service>(Name = "Service2")]
internal sealed partial class MyProvider;

public interface IService;
public interface IService2;
public sealed class Service : IService, IService2;
```

When you have multiple services with the same service type, the normal approach injecting the service by service type is ambiguous and will fail in a compile error.
In that case you must inject the service by name using the [DependencyAttribute](Readme_md/TypeTables.md#dependencyattribute).

```csharp
[ServiceProvider]
[Singleton<IService, Service>]                      // has default name "Service"
[Singleton<IService, Service>(Name = "Service2")]   // explicitly named "Service2"
[Transient<MyService>]
internal sealed partial class MyProvider;

public interface IService;
public sealed class Service : IService;

public sealed class MyService([Dependency(Name = "Service")] IService firstService) {
    [Dependency(Name = "Service2")]
    public required IService SecondService { private get; init; }
}
```


<br></br>
## Implicit Configurations


<br></br>
### Name, Namespace and Modifier

The class name and the namespace are given with partial class under the [ServiceProviderAttribute](Readme_md/TypeTables.md#serviceproviderattribute).
The modifiers are also inferred from the class definition.

```csharp
namespace MySpace;

[ServiceProvider]
internal sealed partial class MyProvider;

// generated ServiceProvider will have:
//   name: "MyProvider"
//   namespace: "MySpace"
//   modifier: "internal", "sealed", "partial"
```


<br></br>
### Overwriting default services

There are always 2 services registered by default:
A singleton service returning the ServiceProvider itself and scoped service returning the ScopedProvider itself.
If a service is registered that matches the service type and lifetime of a default service,
the default service is not registered and the manual registered service is used instead.

Note:  
The service types of the ServiceProvider and ScopedProvider are the auto-generated interfaces.
You can use them without getting a syntax error, however, at the point of generation the interfaces do not exist yet
and therefore the generator will accept these types, but puts them in the global namespace.
To solve this issue, you can correct the namespace by just declaring the interface type in the same namespace.

```csharp
namespace MySpace;

[ServiceProvider]
// overrides default singleton service => default is identifier "Self", now it is identifier "Me"
[Singleton<IMyProvider, MyProvider>(Name = "Me", Implementation = "this")]
// overrides default scoped service => default is identifier "SelfScope", now it is identifier "MeScope"
[Scoped<IMyProvider.IScope, MyProvider.Scope>(Name = "MeScope", Implementation = "this")]
public sealed partial class MyProvider;

// declaring the interface type in the same namespace
public partial interface IMyProvider;
```


<br></br>
### Custom Constructor

The generator implements a constructor to initialize Singleton/Scoped services.
When you declare a constructor in the ServiceProvider/ScopedProvider, the generator will generate a private *InitServices()* method instead.
This method should be called at the end of your constructor to initialize the services.
In the ScopeProvider the generated constructor takes as parameter a reference of the ServiceProvider, so the *InitServices()* method will also take this parameter.

For constructing the Scope you can make use of the *CreateScope()* method.
Every dependency of the ScopeProvider will be a parameter in *CreateScope()*
or if the dependency has a [Dependency]-attribute, the dependency will be injected from the ServiceProvider instead

Note:  
The service types of the ServiceProvider and ScopedProvider are the auto-generated interfaces.
You can use them without getting a syntax error, however, at the point of generation the interfaces do not exist yet
and therefore the generator will accept these types, but puts them in the global namespace.
To solve this issue, you can correct the namespace by just declaring the interface type in the same namespace.

```csharp
[ServiceProvider]
public sealed partial class MyProvider {
    private readonly string _dbConnection;

    public MyProvider(string dbConnection) {
        _dbConnection = dbConnection;
        InitServices();
    }

    public sealed partial class Scope {
        private readonly string _dbConnectionScoped;

        public Scope([Dependency] IMyProvider myProvider, string dbConnectionScoped) {
            _dbConnectionScoped = dbConnectionScoped;
            InitServices(myProvider);
        }
    }
}

// declaring the interface type in the same namespace
public partial interface IMyProvider;

/**
 * generated CreateScope() method:
 *
 * public global::IMyProvider.IScope CreateScope(global::System.String dbConnectionScoped) => new global::MyProvider.Scope(Self, dbConnectionScoped);
 **/
```

If you have nullable reference types enabled and forgot to call *InitServices()*, you get "Non-nullable field '\{name\}' must contain a non-null value when exiting constructor" warning for each reference type service.
These warnings vanish when you call *InitServices()*.  
You can define multiple constructors in your ScopedProvider, but then you must mark one of them with the [ConstructorAttribute](TypeTables.md#constructorattribute).  
Primary constructors are not supported and will not be detected as constructor.
Since you must call *InitServices()* at the end of your constructor logic, primary constructors are not suited anyway.


<br></br>
### Custom Dispose

The generator implements *Dispose()* and *DisposeAsync()* methods when not disabled.
When you declare your own *Dispose()* or *DisposeAsync()* method in the ServiceProvider/ScopedProvider, the generator will generate a private *Dispose()*/*DisposeAsync()* method instead.
This method should be called in your *Dispose()*/*DisposeAsync()* method to dispose the services.

```csharp
[ServiceProvider]
public sealed partial class MyProvider {
    // Does the same as not providing this method
    public void Dispose() {
        DisposeServices();
    }

    // Does the same as not providing this method
    public async ValueTask DisposeAsync() {
        await DisposeServicesAsync();
    }

    public sealed partial class Scope {
        // Does the same as not providing this method
        public void Dispose() {
            DisposeServices();
        }

        // Does the same as not providing this method
        public async ValueTask DisposeAsync() {
            await DisposeServicesAsync();
        }
    }
}
```


<br></br>
### Struct Types and Native/Built-in Types

It is also possible to register struct types or native/built-in types as service.
However, structs are value types and therefore copied when retrieving the service.
So make sure the registered struct is trivially copyable.

Getting a value type service by reference is not supported yet.


<br></br>
## Workarounds for not supported Features


<br></br>
### Async Constructor

A special feature for constructing services asynchronous or the ServiceProvider itself asynchronous does not exist.
However, you can just use common practices to deal with asynchronous operations.

#### ServiceProvider

If constructing of the ServiceProvider needs a asynchronous operation, just set the constructor private and expose a static async method for constructing the ServiceProvider

```csharp
[ServiceProvider]
public sealed partial class MyProvider {
    private readonly string _fileContent;

    private MyProvider(string fileContent) {
        _fileContent = fileContent;
        InitServices();
    }

    public static async Task<MyProvider> Create() {
        string fileContent = await File.ReadAllTextAsync("secret.json");
        return new MyProvider(fileContent);
    }
}
```

#### Service

If the construction of a service needs asynchronous operations, you can either move the problem to the outside by registering *Task&lt;Service&gt;* instead of *Service*:

```csharp
[ServiceProvider]
[Singleton<Task<MyService>>(Implementation = nameof(CreateMyService))]
public sealed partial class MyProvider {
    public static Task<MyService> CreateMyService => MyService.Create();
}

public class MyService {
    private readonly string _fileContent;

    private MyService(string fileContent) {
        _fileContent = fileContent;
    }

    public static async Task<MyService> Create() {
        string fileContent = await File.ReadAllTextAsync("secret.json");
        return new MyService(fileContent);
    }
}
```

Or you move the problem to the inside and all methods using the resource become asynchronous operations:

```csharp
[ServiceProvider]
[Singleton<MyService>]
public sealed partial class MyProvider;

public class MyService {
    private readonly Task<string> _fileContent;

    public MyService() {
        _fileContent = File.ReadAllTextAsync("secret.json");
    }

    public async ValueTask<string> GetSecret() {
        return await _fileContent;
    }
}
```


<br></br>
### Decoration

Sometimes you have a base class and want to extend that class via Composition.
The extensions have a dependency to the base class, so you have to register the base class in order to be able to construct the extensions.
But then nothing stops other services to (accidentally) get this service as well.
To solve this problem you could not register the base class and instead construct the base and each extension by yourself:

```csharp
[ServiceProvider]
[Singleton<Service1>(Implementation = nameof(CreateService1))]
[Singleton<Service2>(Implementation = nameof(CreateService2))]
public sealed partial class MyProvider {
    private readonly ServiceBase _serviceBase;

    private Service1 CreateService1 => new Service1(_serviceBase);
    private Service2 CreateService2 => new Service2(_serviceBase);

    public MyProvider() {
        _serviceBase = new();
        InitServices();
    }
}

public class ServiceBase;
public class Service1(ServiceBase serviceBase);
public class Service2(ServiceBase serviceBase);
```


<br></br>
### Open/Unbound Generic Types

Generic services, something like Logger&lt;T&gt;, are not supported yet.
However, you can easily build your own service factory:

```csharp
[ServiceProvider]
[Singleton<LoggerFactory>]
[Transient<MyService>]
public sealed partial class MyProvider;

public struct LoggerFactory {
    public Logger<T> CreateLogger<T>() => new Logger<T>();
}


public class MyService {
    private readonly Logger<MyService> _logger;

    public MyService(LoggerFactory loggerFactory) {
        _logger = loggerFactory.CreateLogger<MyService>();
    }
}
```

If your produced loggers should be singletons, you have to track them in a list:

```csharp
[ServiceProvider]
[Singleton<LoggerFactory>]
[Transient<MyService>]
public sealed partial class MyProvider;

public struct LoggerFactory {
    private readonly Dictionary<Type, object> _loggerList = [];

    [Constructor]
    public LoggerFactory() { }

    public Logger<T> GetLogger<T>() {
        if (_loggerList.TryGetValue(typeof(T), out object? logger))
            return (Logger<T>)logger;
        else
            return new Logger<T>();
    }
}


public class MyService {
    private readonly Logger<MyService> _logger;

    public MyService(LoggerFactory loggerFactory) {
        _logger = loggerFactory.GetLogger<MyService>();
    }
}
```


<br></br>
## Remarks


<br></br>
### IServiceProvider Interface

The generated ServiceProvider also implements [System.IServiceProvider](https://learn.microsoft.com/en-us/dotnet/api/system.iserviceprovider?view=net-8.0), but it is not recommended to use it.
The interface is implemented explicitly, so you need a *IServiceProvider* reference to call the *GetService(Type)* method.
The method can return *null*, an *object* of the given type or an *Array* of the given type, depending if the given type is registered zero, one or many times.


<br></br>
### Error Handling

The source generator will emit descriptive errors when something is wrong.
The location of the error is always at the corresponding attribute to make clear the error originate from a ServiceProvider, even if the error itself is in the interface or implementation.
So reading the error message will certainly help to figure out the problem.

However, not all errors are emitted by the source generator, some are just natural syntax errors.
e.g. when there are 2 services with the same name/identifier, the generator will generate the source code without issues, but the generated code is just not legal C# code, because the class contains 2 properties/methods with the same name/identifier.
It is intended that the generated code can contain errors and do not be afraid of reading and understanding the generated code, it is intended to be read.

Note:  
When the generator emits an error, it does not generate the ServiceProvider class.
In most cases this results in additional errors and these errors are listed first and the source generator errors are usually at the bottom.
So take care of the errors starting with id 'CDI' first and then look at the remaining ones (if any).
