# Injectio

Source generator that automatically registers discovered services in the dependency injection container

## Features

 - Transient, Singleton, Scoped service registration
 - Factory registration
 - Module registration
 - Duplicate Strategy - Skip,Replace,Append
 - Registration Strategy - Self, Implemented Interfaces, Self With Interfaces

### Usage

#### Add package

Add the nuget package project to your projects.

`dotnet add package Injectio`

Prevent dependances from including Injectio

```xml
<PackageReference Include="Injectio" PrivateAssets="all" />
```


### Attributes

Place registration attribute on class.  The class will be discovered and regirsted.

#### Singleton services

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

#### Register Method

When the service registration is complex, use the `RegisterServices` attribute on a method that has a parameter of `IServiceCollection` or `ServiceCollection`

```c#
public class RegistrationModule
{
    [RegisterServices]
    public static void Register(IServiceCollection services)
    {
        services.TryAddTransient<IModuleService, ModuleService>();
    }
}
```

#### Add to container

The source generator creates an extension method with all the discovered services registered.  Call the generated extension method to add the services to the container.  The method will be called `Add[AssemblyName]`.  The assemlby name will have the dots removed.

```c#
var services = new ServiceCollection();
services.AddInjectioTestsConsole();
```

Override the extension method name by using the `InjectioName` MSBuild property.

```xml
<PropertyGroup>
  <InjectioName>Library</InjectioName>
</PropertyGroup>
```

```c#
var services = new ServiceCollection();
services.AddLibrary();
```
