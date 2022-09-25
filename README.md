# Injectio
Source generator that generates dependency injection registration during compile time

## Features

 - Transient, Singleton, Scoped service registration
 - Factory registration
 - Module registration
 - Duplicate Strategy - Skip,Replace,Append
 - Registration Strategy - Self, Implemented Interfaces, Self With Interfaces

### Usage

#### Add package

Add the package project to your projects.

`dotnet add package Injectio`


#### Add to container

Call the generated extension method to add to the service container.  The method will be called Add[AssemblyName].  The assemlby name will have the dots removed.

```c#
var services = new ServiceCollection();
services.AddInjectioTestsConsole();
```

### Attributes

#### Singleton services

Place registration attribute on class.  The class will be discovered and regirsted.

```c#
[RegisterSingleton]
public class SingletonService : IService { }
```

Explicit service type

```c#
[RegisterSingleton(ServiceType = typeof(IService))]
public class SingletonService : IService { }
```

#### Scoped Services

```c#
[RegisterScoped]
public class ScopedService : IService { }
```

#### Transient Services


```c#
[RegisterTransient]
public class TransientService : IService { }
```

#### Factories

```c#
[RegisterTransient(ServiceType = typeof(IService), Factory = nameof(ServiceFactory))]
public class FactoryService : IService
{
    public static object ServiceFactory(IServiceProvider serviceProvider)
    {
        return new FactoryService1();
    }
}
```

#### Modules

```c#
public class RegistrationModule
{
    [RegisterModule]
    public static void Register(IServiceCollection services)
    {
        services.TryAddTransient<IModuleService, ModuleService>();
    }
}
```
