# Comparison to ServiceCollection

[Microsoft.Extensions.DependencyInjection.ServiceCollection](https://learn.microsoft.com/en-us/dotnet/api/microsoft.extensions.dependencyinjection.servicecollection?view=dotnet-plat-ext-8.0)



<br></br>
## Create ServiceProvider and register Services

```csharp
using Microsoft.Extensions.DependencyInjection;

public interface IService1;
public class Service1(IService3 service3) : IService1;
public interface IService2;
public class Service2(IService1 service1, IService3 service3) : IService2;
public interface IService3;
public class Service3 : IService3;

public static class Program {
    public static async Task Main() {
        #region Definition

        ServiceCollection serviceCollection = new();

        serviceCollection.AddSingleton<IService1, Service1>();
        serviceCollection.AddScoped<IService2, Service2>();
        serviceCollection.AddTransient<IService3, Service3>();

        #endregion

        #region Instance

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IService1? service1 = serviceProvider.GetService<IService1>();
        IService2? service2 = serviceProvider.GetService<IService2>();
        IService3? service3 = serviceProvider.GetService<IService3>();

        #endregion
    }
}
```

```csharp
using CircleDIAttributes;

public interface IService1;
public class Service1(IService3 service3) : IService1;
public interface IService2;
public class Service2(IService1 service1, IService3 service3) : IService2;
public interface IService3;
public class Service3 : IService3;

#region Definition

[ServiceProvider]
[Singleton<IService1, Service1>]
[Scoped<IService2, Service2>]
[Transient<IService3, Service3>]
public sealed partial class MyProvider;

#endregion

public static class Program {
    public static async Task Main() {
        #region Instance

        MyProvider serviceProvider = new();

        IService1 service1 = serviceProvider.Service1;
        IService2 service2 = serviceProvider.Service2;
        IService3 service3 = serviceProvider.Service3;

        #endregion
    }
}
```



<br></br>
## Register Delegate Service

```csharp
using Microsoft.Extensions.DependencyInjection;

public delegate string MyDelegate(int number);

public static class Program {
    public static void Main() {
        #region Definition

        ServiceCollection serviceCollection = new();

        serviceCollection.AddSingleton<MyDelegate>(_ => static (int number) => number.ToString());

        #endregion

        #region Instance

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        MyDelegate? delegateService = serviceProvider.GetService<MyDelegate>();

        if (delegateService != null) {
            string result = delegateService(5);
            Console.WriteLine(result);
        }

        #endregion
    }
}
```

```csharp
using CircleDIAttributes;

public delegate string MyDelegate(int number);

#region Definition

[ServiceProvider]
[Delegate<MyDelegate>(nameof(DelegateImpl))]
public sealed partial class MyProvider {
    private static string DelegateImpl(int number) => number.ToString();
}

#endregion

public static class Program {
    public static void Main() {
        #region Instance

        MyProvider serviceProvider = new();

        MyDelegate delegateService = serviceProvider.MyDelegate;

        string result = delegateService(5);
        Console.WriteLine(result);

        #endregion
    }
}
```



<br></br>
## Register Singleton Instance

```csharp
using Microsoft.Extensions.DependencyInjection;

public interface IService;
public class Service : IService;

public static class Program {
    public static void Main() {
        Service instance = new();

        #region Definition

        ServiceCollection serviceCollection = new();

        serviceCollection.AddSingleton<IService>(instance);

        #endregion

        #region Instance

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IService? service = serviceProvider.GetService<IService>();

        #endregion
    }
}
```

```csharp
using CircleDIAttributes;

public interface IService;
public class Service : IService;

#region Definition

[ServiceProvider]
[Singleton<IService, Service>(Implementation = nameof(instance))]
public sealed partial class MyProvider {
    private readonly Service instance;

    public MyProvider(Service service) {
        instance = service;
        InitServices();
    }
}

#endregion

public static class Program {
    public static void Main() {
        Service instance = new();

        #region Instance

        MyProvider serviceProvider = new(instance);

        IService service = serviceProvider.Service;

        #endregion
    }
}
```



<br></br>
## Register Factory Method

```csharp
using Microsoft.Extensions.DependencyInjection;

public interface IService;
public class Service(Guid guid) : IService;

public static class Program {
    public static void Main() {
        #region Definition

        ServiceCollection serviceCollection = new();

        serviceCollection.AddTransient<IService>(_ => new Service(Guid.NewGuid()));

        #endregion

        #region Instance

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IService? service = serviceProvider.GetService<IService>();

        #endregion
    }
}
```

```csharp
using CircleDIAttributes;

public interface IService;
public class Service(Guid guid) : IService;

#region Definition

[ServiceProvider]
[Transient<IService, Service>(Implementation = nameof(ServiceFactory))]
public sealed partial class MyProvider {
    // can also be a property
    private static Service ServiceFactory( /* Dependency Injection works here as well */ ) => new Service(Guid.NewGuid());
}

#endregion

public static class Program {
    public static void Main() {
        #region Instance

        MyProvider serviceProvider = new();

        IService service = serviceProvider.Service;

        #endregion
    }
}
```




<br></br>
## Register Named Service / Keyed Service

```csharp
using Microsoft.Extensions.DependencyInjection;

public interface IService;
public class Service : IService;

public interface IServiceT;
public class ServiceT([FromKeyedServices("Service1")] IService service1, [FromKeyedServices("Service2")] IService service2) : IServiceT;

public static class Program {
    public static void Main() {
        #region Definition

        ServiceCollection serviceCollection = new();

        serviceCollection.AddKeyedSingleton<IService, Service>("Service1");
        serviceCollection.AddKeyedSingleton<IService, Service>("Service2");
        serviceCollection.AddTransient<IServiceT, ServiceT>();

        #endregion

        #region Instance

        ServiceProvider serviceProvider = serviceCollection.BuildServiceProvider();

        IServiceT? serviceT = serviceProvider.GetService<IServiceT>();

        #endregion
    }
}
```

```csharp
using CircleDIAttributes;

public interface IService;
public class Service : IService;

public interface IServiceT;
public class ServiceT([Dependency(Name = "Service1")] IService service1, [Dependency(Name = "Service2")] IService service2) : IServiceT;

#region Definition

[ServiceProvider]
[Singleton<IService, Service>(Name = "Service1")]
[Singleton<IService, Service>(Name = "Service2")]
[Transient<IServiceT, ServiceT>]
public sealed partial class MyProvider;

#endregion

public static class Program {
    public static void Main() {
        #region Instance

        MyProvider serviceProvider = new();

        IServiceT serviceT = serviceProvider.ServiceT;

        #endregion
    }
}
```
