using Injectio.Attributes;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Injectio.Tests.Library;

public interface IService1
{
}

public interface IService2
{
}

public interface IService3
{
}

[RegisterSingleton(Registration = RegistrationStrategy.SelfWithInterfaces)]
public class SingletonService3 : IService3
{
}

[RegisterScoped(Duplicate = DuplicateStrategy.Append)]
public class ScopedWithInterfacesService2 : IService2
{
}

[RegisterScoped(ServiceType = typeof(IService1))]
[RegisterScoped(ServiceType = typeof(IService2), Duplicate = DuplicateStrategy.Append)]
public class ServiceMultiple : IService1, IService2
{
}

public interface IFactoryService1
{
}

[RegisterTransient(ServiceType = typeof(IFactoryService1), Factory = nameof(ServiceFactory))]
public class FactoryService1 : IFactoryService1
{
    private readonly IService1 _service1;

    public FactoryService1(IService1 service1)
    {
        _service1 = service1;
    }

    public static IFactoryService1 ServiceFactory(IServiceProvider serviceProvider)
    {
        return new FactoryService1(serviceProvider.GetRequiredService<IService1>());
    }
}

public interface IModuleService
{
}

public class ModuleService : IModuleService
{
}

public class RegistrationModule
{
    [RegisterServices]
    public void Register(IServiceCollection services)
    {
        services.TryAddTransient<IModuleService, ModuleService>();
    }
}

public interface IOpenGeneric<T>
{
}

[RegisterSingleton(ImplementationType = typeof(OpenGeneric<>), ServiceType = typeof(IOpenGeneric<>))]
public class OpenGeneric<T> : IOpenGeneric<T>
{
}

public interface IServiceTag
{
}

[RegisterSingleton(Tags = "Client,FrontEnd")]
public class ServiceTag : IServiceTag
{
}

public interface IServiceKeyed
{

}

[RegisterSingleton<IServiceKeyed>(ServiceKey = "Alpha")]
public class ServiceAlphaKeyed : IServiceKeyed
{
}

[RegisterSingleton<IServiceKeyed>(ServiceKey = "Beta")]
public class ServiceBetaKeyed : IServiceKeyed
{
}

[RegisterSingleton<IServiceKeyed>(ServiceKey = ServiceType.Alpha)]
public class ServiceAlphaTypeKeyed : IServiceKeyed
{
}

[RegisterSingleton<IServiceKeyed>(ServiceKey = ServiceType.Beta)]
public class ServiceBetaTypeKeyed : IServiceKeyed
{
}

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

public enum ServiceType
{
    Alpha,
    Beta,
    Charlie,
    Delta
}
