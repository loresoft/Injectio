# Injectio

Source generator that helps register attribute marked services in the dependency injection ServiceCollection

[![Source generator](https://raw.githubusercontent.com/loresoft/Injectio/main/media/Injectio.Genertors.png)](https://github.com/loresoft/Injectio)

## Features

- Transient, Singleton, Scoped service registration
- Factory registration
- Module method registration
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

### Registration Attributes

Place registration attribute on class.  The class will be discovered and registered.

- `[RegisterSingleton]` Marks the class as a singleton service
- `[RegisterScoped]` Marks the class as a scoped service
- `[RegisterTransient]` Marks the class as a transient service
- `[RegisterServices]` Marks the method to be called to register services

#### Attribute Properties

| Property           | Description                                                                                         |
|--------------------|-----------------------------------------------------------------------------------------------------|
| ImplementationType | The type that implements the service.  If not set, the class the attribute is on will be used.      |
| ServiceType        | The type of the service. If not set, the Registration property used to determine what is registered.|
| Factory            | Name of a factory method to create new instances of the service implementation.                     |
| Duplicate          | How the generator handles duplicate registrations. See Duplicate Strategy                           |
| Registration       | How the generator determines what to register. See Registration Strategy                            |

#### Duplicate Strategy

| Value   | Description                                          |
|---------|------------------------------------------------------|
| Skip    | Skips registrations for services that already exists |
| Replace | Replaces existing service registrations              |
| Append  | Appends a new registration for existing services     |

#### Registration Strategy

| Value                 | Description                                                                           |
|-----------------------|---------------------------------------------------------------------------------------|
| Self                  | Registers each matching concrete type as itself                                       |
| ImplementedInterfaces | Registers each matching concrete type as all of its implemented interfaces            |
| SelfWithInterfaces    | Registers each matching concrete type as all of its implemented interfaces and itself |

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

Support resolving multiple services with `IEnumerable<T>`

```c#
[RegisterSingleton(Duplicate = DuplicateStrategy.Append)]
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
[RegisterTransient(Factory = nameof(ServiceFactory))]
public class FactoryService : IFactoryService
{
    private readonly IService _service;

    public FactoryService(IService service)
    { 
        _service = service;
    }

    public static IFactoryService ServiceFactory(IServiceProvider serviceProvider)
    {
        return new FactoryService(serviceProvider.GetService<IService>());
    }
}
```

#### Open Generic

```c#
[RegisterSingleton(ImplementationType = typeof(OpenGeneric<>), ServiceType = typeof(IOpenGeneric<>))]
public class OpenGeneric<T> : IOpenGeneric<T> { }
```

#### Generic Attributes

You can use generic attributes to register services if your project targets .NET 7.0+

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFrameworks>net7.0</TargetFrameworks>
  </PropertyGroup>
</Project>
```

Generic attributes allow declaration to be more compact by avoiding the typeof calls

```c#
[RegisterSingleton<IService>]
public class ServiceImplementation : IService { }
```

#### Keyed Services

You can register keyed services with version 8.0+ of `Microsoft.Extensions.DependencyInjection`

Register a keyed service

```c#
[RegisterSingleton<IServiceKeyed>(ServiceKey = "Alpha")]
public class ServiceAlphaKeyed : IServiceKeyed
{
}

[RegisterSingleton<IServiceKeyed>(ServiceKey = "Beta")]
public class ServiceBetaKeyed : IServiceKeyed
{
}
```

Register using an enum

```c#
public enum ServiceType
{
    Alpha,
    Beta
}

[RegisterSingleton<IServiceKeyed>(ServiceKey = ServiceType.Alpha)]
public class ServiceAlphaTypeKeyed : IServiceKeyed
{
}

[RegisterSingleton<IServiceKeyed>(ServiceKey = ServiceType.Beta)]
public class ServiceBetaTypeKeyed : IServiceKeyed
{
}
```

Register using an factory method

```c#
[RegisterSingleton<IServiceKeyed>(ServiceKey = "Charlie", Factory = nameof(ServiceFactory))]
[RegisterSingleton<IServiceKeyed>(ServiceKey = "Delta", Factory = nameof(ServiceFactory))]
public class ServiceFactoryKeyed : IServiceKeyed
{
    public ServiceFactoryKeyed(object? serviceKey)
    {
        ServiceKey = serviceKey;
    }

    public object? ServiceKey { get; }

    public static IServiceKeyed ServiceFactory(IServiceProvider serviceProvider, object? serviceKey)
    {
        return new ServiceFactoryKeyed(serviceKey);
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
        // add configuration options
        services
            .AddOptions<PollingOption>()
            .Configure<IConfiguration>((settings, configuration) => configuration
                .GetSection(PollingOption.SectionName)
                .Bind(settings)
            );
    }
}
```

#### Add to container

The source generator creates an extension method with all the discovered services registered.  Call the generated extension method to add the services to the container.  The extension method will be called `Add[AssemblyName]`.  The assembly name will have the dots removed.

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

#### Registration Tags

Control what is registered when calling the generated extension method using Tags

Tag the service

```c#
public interface IServiceTag
{
}

[RegisterSingleton(Tags = "Client,FrontEnd")]
public class ServiceTag : IServiceTag
{
}
```

Tags can be passed to register methods

```c#
public static class ServiceRegistration
{
    [RegisterServices]
    public static void Register(IServiceCollection services, ISet<string> tags)
    {

    }
}
```

Specify tags when adding to service collection.  Note, if no tags specified, all services are registered

```c#
var services = new ServiceCollection();
services.AddInjectioTestsLibrary("Client");
```
