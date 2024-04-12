# ServiceProviderAttribute

A class decorated with this attribute becomes a service provider. That class must be partial.

To add services to that class, add additional attributes to that class:  
[SingletonAttribute](#singletonattribute), [ScopedAttribute](#scopedattribute), [TransientAttribute](#transientattribute), [DelegateAttribute](#delegateattribute).

The source generator will generate a complete service provider, but you can add additional custom implementation to it.

## Properties

| **Name**               | **Type**                                     | **Description**                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| ---------------------- | -------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| InterfaceName          | string                                       | Name/Identifier of the generated Interface. If omitted, the name will be "I\{ClassName\}".                                                                                                                                                                                                                                                                                                                                                                                                               |
| CreationTime           | [CreationTiming](#creationtiming-enum)       | Decides whether services defaulting to lazy construction or instantiation inside the constructor. This option applies to all services where the "CreationTime"-attribute is not set. Default is [CreationTiming.Constructor](#creationtiming-enum).                                                                                                                                                                                                                                                      |
| GetAccessor            | [GetAccess](#getaccess-enum)                 | Decides whether the members to access the services in the ServiceProvider defaulting to properties or methods. This option applies to all services where the "GetAccessor"-attribute is not set. Default is [GetAccess.Property](#getaccess-enum).                                                                                                                                                                                                                                                       |
| GenerateDisposeMethods | [DisposeGeneration](#disposegeneration-enum) | Toggles the generation of the Dispose methods: public void Dispose(); public ValueTask DisposeAsync(); It can be toggled that both are generated, only one of them or the generation is skipped entirely. Default is [DisposeGeneration.GenerateBoth](#disposegeneration-enum)                                                                                                                                                                                                                           |
| ThreadSafe             | bool                                         | Indicates if the generated code will be thread safe or a little bit more efficient. Affects performance for lazy constructed singleton services and disposables transient services: Singletons acquire a lock for construction; Disposable transient services acquire a lock when constructed and disposed to synchronize access on a dispose-list. This option should be set to false, if the provider is used in a single threaded scenario or only accessed by one thread at a time. Default is true. |



<br></br>
# ServiceProviderAttribute&lt;TInterface&gt;

A class decorated with this attribute becomes a service provider. That class must be partial.

To add services to that class, add additional attributes to that class:  
[SingletonAttribute](#singletonattribute), [ScopedAttribute](#scopedattribute), [TransientAttribute](#transientattribute), [DelegateAttribute](#delegateattribute).

The source generator will generate a complete service provider, but you can add additional custom implementation to it.

## Type Parameters

| **Name**   | **TypeConstraints** |  **Description**                                                                                                                                                                                                                                                                          |
| ---------- | ------------------- | ----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| TInterface | interface           | An explicit declared interface the generated interface will be based on: The name, access modifier, namespace and containing types will be inferred. That interface must be partial. If the generated interface is used without declaring the interface yourself, it will have no effect. |

## Properties

| **Name**               | **Type**                                     | **Description**                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| ---------------------- | -------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| CreationTime           | [CreationTiming](#creationtiming-enum)       | Decides whether services defaulting to lazy construction or instantiation inside the constructor. This option applies to all services where the "CreationTime"-attribute is not set. Default is [CreationTiming.Constructor](#creationtiming-enum).                                                                                                                                                                                                                                                      |
| GetAccessor            | [GetAccess](#getaccess-enum)                 | Decides whether the members to access the services in the ServiceProvider defaulting to properties or methods. This option applies to all services where the "GetAccessor"-attribute is not set. Default is [GetAccess.Property](#getaccess-enum).                                                                                                                                                                                                                                                       |
| GenerateDisposeMethods | [DisposeGeneration](#disposegeneration-enum) | Toggles the generation of the Dispose methods: public void Dispose(); public ValueTask DisposeAsync(); It can be toggled that both are generated, only one of them or the generation is skipped entirely. Default is [DisposeGeneration.GenerateBoth](#disposegeneration-enum)                                                                                                                                                                                                                           |
| ThreadSafe             | bool                                         | Indicates if the generated code will be thread safe or a little bit more efficient. Affects performance for lazy constructed singleton services and disposables transient services: Singletons acquire a lock for construction; Disposable transient services acquire a lock when constructed and disposed to synchronize access on a dispose-list. This option should be set to false, if the provider is used in a single threaded scenario or only accessed by one thread at a time. Default is true. |



<br></br>
# ScopedProviderAttribute

This attribute can be applied to the ServiceProvider itself, so right next to the [ServiceProviderAttribute](#serviceproviderattribute) or to a class named "Scope" inside the ServiceProvider, but not both.

This attribute itself does nothing, but it provides additional configurations for the ScopedProvider.

## Properties

| **Name**               | **Type**                                     | **Description**                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      |
| ---------------------- | -------------------------------------------- | -------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Generate               | bool                                         | If turned off, no Scope Class is generated and therefore [ScopedAttribute](#scopedattribute) has no effect.                                                                                                                                                                                                                                                                                                                                                                                                                                                          |
| CreationTime           | [CreationTiming](#creationtiming-enum)       | Decides whether services in the ScopedProvider defaulting to lazy construction or instantiation inside the constructor. This option applies to all services where the "CreationTime"-attribute is not set. Default is [ServiceProviderAttribute.CreationTime](#serviceproviderattribute).                                                                                                                                                                                                                                                                            |
| GetAccessor            | [GetAccess](#getaccess-enum)                 | Decides whether the members to access the services in the ScopedProvider defaulting to properties or methods. This option applies to all services where the "GetAccessor"-attribute is not set. Default is [ServiceProviderAttribute.GetAccessor](#serviceproviderattribute).                                                                                                                                                                                                                                                                                        |
| GenerateDisposeMethods | [DisposeGeneration](#disposegeneration-enum) | Toggles the generation of the Dispose methods: public void Dispose(); public ValueTask DisposeAsync(); It can be toggled that both are generated, only one of them or the generation is skipped entirely. Default is [ServiceProviderAttribute.GenerateDisposeMethods](#serviceproviderattribute)                                                                                                                                                                                                                                                                    |
| ThreadSafe             | bool                                         | Indicates if the generated code will be thread safe or a little bit more efficient. Affects performance for lazy constructed singleton services and disposables transient services: Singletons acquire a lock for construction; Disposable transient services acquire a lock when constructed and disposed to synchronize access on a dispose-list. This option should be set to false, if the provider is used in a single threaded scenario or only accessed by one thread at a time. Default is [ServiceProviderAttribute.ThreadSafe](#serviceproviderattribute). |



<br></br>
# SingletonAttribute

Specifies a singleton service.
That means there will be a single instance of that service in every ServiceProvider instance.

If [ServiceProviderAttribute](#serviceproviderattribute) is used at the same class, this service will be added to the provider.

## Type Parameters

| **Name**                   | **Description**                                                                           |
| -------------------------- | ----------------------------------------------------------------------------------------- |
| TService                   | Type of the service.                                                                      |
| TImplementation (Optional) | Type of the implementation. If omitted, implementation will be the same type as TService. |

## Properties

| **Name**       | **Type**                               | **Description**                                                                                                                                                                                                                                                                         |
| -------------- | -------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Implementation | string                                 | Fieldname, propertyname or methodname that will be the implementation supplier for the given service. The parameters of the method will be dependency injected.                                                                                                                         |
| Name           | string                                 | The name of this service. If omitted, it will be the name of TImplementation. If the getter is a method, for the identifier a "Get" prefix will be used, but this does not affect the name.                                                                                             |
| CreationTime   | [CreationTiming](#creationtiming-enum) | Decides whether this service will be lazy constructed or instantiated inside the constructor. Defaults to [ServiceProviderAttribute.CreationTime](#serviceproviderattribute) or [ScopedProviderAttribute.CreationTime](#scopedproviderattribute).                                       |
| GetAccessor    | [GetAccess](#getaccess-enum)           | Decides whether the type of the member to access this service will be a property or method. Defaults to [ServiceProviderAttribute.GetAccessor](#serviceproviderattribute) or [ScopedProviderAttribute.GetAccessor](#scopedproviderattribute).                                           |
| NoDispose      | bool                                   | When true, the ServiceProvider does not dispose this service on *Dispose()* or *DisposeAsync()*, regardless the service implements *IDisposable* or *IAsyncDisposable*. If the service does not implement *IDisposable*/*IAsyncDisposable*, this will have no effect. Default is false. |



<br></br>
# ScopedAttribute

Specifies a scoped service.
That means this service will only be available in the ScopedProvider and there will be a single instance of that service in every ScopedProvider instance.

If [ServiceProviderAttribute](#serviceproviderattribute) is used at the same class, this service will be added to the provider.

## Type Parameters

| **Name**                   | **Description**                                                                           |
| -------------------------- | ----------------------------------------------------------------------------------------- |
| TService                   | Type of the service.                                                                      |
| TImplementation (Optional) | Type of the implementation. If omitted, implementation will be the same type as TService. |

## Properties

| **Name**       | **Type**                               | **Description**                                                                                                                                                                                                                                                                         |
| -------------- | -------------------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Implementation | string                                 | Fieldname, propertyname or methodname that will be the implementation supplier for the given service. The parameters of the method will be dependency injected.                                                                                                                         |
| Name           | string                                 | The name of this service. If omitted, it will be the name of TImplementation. If the getter is a method, for the identifier a "Get" prefix will be used, but this does not affect the name.                                                                                             |
| CreationTime   | [CreationTiming](#creationtiming-enum) | Decides whether this service will be lazy constructed or instantiated inside the constructor. Defaults to [ServiceProviderAttribute.CreationTime](#serviceproviderattribute) or [ScopedProviderAttribute.CreationTime](#scopedproviderattribute).                                       |
| GetAccessor    | [GetAccess](#getaccess-enum)           | Decides whether the type of the member to access this service will be a property or method. Defaults to [ServiceProviderAttribute.GetAccessor](#serviceproviderattribute) or [ScopedProviderAttribute.GetAccessor](#scopedproviderattribute).                                           |
| NoDispose      | bool                                   | When true, the ServiceProvider does not dispose this service on *Dispose()* or *DisposeAsync()*, regardless the service implements *IDisposable* or *IAsyncDisposable*. If the service does not implement *IDisposable*/*IAsyncDisposable*, this will have no effect. Default is false. |



<br></br>
# TransientAttribute

Specifies a transient service.
That means this service will be instantiated each time requested.

If [ServiceProviderAttribute](#serviceproviderattribute) is used at the same class, this service will be added to the provider.

## Type Parameters

| **Name**                   | **Description**                                                                           |
| -------------------------- | ----------------------------------------------------------------------------------------- |
| TService                   | Type of the service.                                                                      |
| TImplementation (Optional) | Type of the implementation. If omitted, implementation will be the same type as TService. |

## Properties

| **Name**       | **Type**                     | **Description**                                                                                                                                                                                                                                                                         |
| -------------- | ---------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Implementation | string                       | The name of a method or property that construct a implementation for the given service. The parameters of the method will be dependency injected.                                                                                                                                       |
| Name           | string                       | The name of this service. If omitted, it will be the name of TImplementation. If the getter is a method, for the identifier a "Get" prefix will be used, but this does not affect the name.                                                                                             |
| GetAccessor    | [GetAccess](#getaccess-enum) | Decides whether the type of the member to access this service will be a property or method. Defaults to [ServiceProviderAttribute.GetAccessor](#serviceproviderattribute) or [ScopedProviderAttribute.GetAccessor](#scopedproviderattribute).                                           |
| NoDispose      | bool                         | When true, the ServiceProvider does not dispose this service on *Dispose()* or *DisposeAsync()*, regardless the service implements *IDisposable* or *IAsyncDisposable*. If the service does not implement *IDisposable*/*IAsyncDisposable*, this will have no effect. Default is false. |



<br></br>
# DelegateAttribute

Specifies a delegate service.
That means requesting this service will give you a method.

If [ServiceProviderAttribute](#serviceproviderattribute) is used at the same class, this service will be added to the provider.

## Type Parameters

| **Name** | **Description**                         |
| -------- | --------------------------------------- |
| TService | Type of the service and implementation. |

## Parameters

| **Name**   | **Type** | **Description**                                                   |
| ---------- | -------- | ----------------------------------------------------------------- |
| methodName | string   | Methodname that will be the implementation for the given service. |

## Properties

| **Name**       | **Type**                     | **Description**                                                                                                                                                                                                                               |
| -------------- | ---------------------------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Name           | string                       | The name of this service. If omitted, it will be the name of TImplementation. If the getter is a method, for the identifier a "Get" prefix will be used, but this does not affect the name.                                                   |
| GetAccessor    | [GetAccess](#getaccess-enum) | Decides whether the type of the member to access this service will be a property or method. Defaults to [ServiceProviderAttribute.GetAccessor](#serviceproviderattribute) or [ScopedProviderAttribute.GetAccessor](#scopedproviderattribute). |



<br></br>
# ImportAttribute

Registers all attributes of the specified class/struct/interface in *TModule*.

There are several options to handle the instantiation of the module:
- static (no instantiation)  
- injected as service  
- given as parameter

## Type Parameters

| **Name** | **Description**                                                      |
| -------- | -------------------------------------------------------------------- |
| TModule  | The class, struct or interface containing the attributes to include. |

## Parameters

| **Name** | **Type**                                         | **Description**                                                   |
| -------- | ------------------------------------------------ | ----------------------------------------------------------------- |
| mode     | [ImportMode = ImportMode.Auto](#importmode-enum) | Option for handling the instantiation of the module. It can be static (no instantiation), injected as service or given as parameter. Default is *ImportMode.Auto*. |



<br></br>
# DependencyAttribute

This attribute is used to set a non-required property as dependency (required properties are always dependencies).

It is also used to specify specific/named services, see *Name* property.

## Properties

| **Name** | **Type** | **Description**                                                                                                                                                                                                                                                                                     |
| -------- | -------- | --------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------- |
| Name     | string   | The name this dependency gets the service injected from. If omitted, it will match based on the type. If multiple services for this type exists, this property must be set, otherwise compile error. When the Name property of a service not set, the name defaults to the name of TImplementation. |



<br></br>
# ConstructorAttribute

Explicitly specifies the constructor that is used to create the service.

If multiple constructors are available, you must use this attribute on exactly one constructor, otherwise a compile error occurs.  
Only constructors with accessibility public or internal are considered, others are ignored.

A struct has always the parameterless constructor, so by specifying one non-parameterless constructor you have actually two and therefore have to use this attribute.



<br></br>
# CreationTiming (enum)

Configuration when the instantiation of the service happens.  
At ServiceProvider instantiation (inside the constructor) or lazy (first time used).

## Members

| **Name**    | **Description**                                                                         |
| ----------- | --------------------------------------------------------------------------------------- |
| Constructor | The instantiation of the service happens inside the constructor of the ServiceProvider. |
| Lazy        | The instantiation of the service happens the first time it is requested.                |



<br></br>
# GetAccess (enum)

The type of the member to access the service.  
It can be either property or method.

## Members

| **Name** | **Description**                                                                                                                                   |
| -------- | ------------------------------------------------------------------------------------------------------------------------------------------------- |
| Property | The member will be a property with a get-accessor. The name of the property will be the same as the service name.                                 |
| Method   | The member will be a method with no parameters. The name will be prefixed with "Get" e.g. name "MyService" will generate member "GetMyService()". |



<br></br>
# DisposeGeneration (enum)

Configuration for generating the Dispose methods:

public void Dispose();  
public ValueTask DisposeAsync();

It can be toggled that both are generated, only one of them or the generation is skipped entirely.

## Members

| **Name**         | **Description**                                                                                                   |
| ---------------- | ----------------------------------------------------------------------------------------------------------------- |
| NoDisposing      | The generation of both dispose methods will be skipped.                                                           |
| DisposeOnly      | Only the sync version of Dispose will be generated: public void Dispose();                                        |
| DisposeAsyncOnly | Only the async version DisposeAsync will be generated: public ValueTask DisposeAsync();                           |
| GenerateBoth     | Both versions Dispose and DisposeAsync will be generated. public void Dispose(); public ValueTask DisposeAsync(); |



<br></br>
# ImportMode (enum)

Option for handling the instantiation of the module.

## Members

| **Name**  | **Description**                                                                                                          |
| --------- | ------------------------------------------------------------------------------------------------------------------------ |
| Auto      | Chooses *Static* when type is interface, *Service* if constructed with parameterless constructor, *Parameter* otherwise. |
| Static    | No instantiation needed, all members are static.                                                                         |
| Service   | The module is registered as service.                                                                                     |
| Parameter | An instance of the module is given as parameter.                                                                         |
