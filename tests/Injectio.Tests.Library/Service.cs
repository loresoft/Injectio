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

public interface IStaticObjectService
{
    string GetValue();
}

public interface IStaticObjectService2
{

}

public class StaticObjectService2 : IStaticObjectService2
{

}

public class StaticObjectService : IStaticObjectService
{
    private readonly string value;

    public StaticObjectService(string value)
    {
        this.value = value;
    }

    public string GetValue()
    {
        return value;
    }
}

public class StaticObjectExporter2
{
    [RegisterStaticObject(Registration = RegistrationStrategy.SelfWithInterfaces)]
    public static StaticObjectService2 myService1 = new StaticObjectService2();
}

public class StaticObjectExporter1
{
    [RegisterStaticObject]
    public static IStaticObjectService myService1 = new StaticObjectService("1");

    [RegisterStaticObject]
    public static StaticObjectService myService2 = new StaticObjectService("2");

    [RegisterStaticObject<IStaticObjectService>]
    public static StaticObjectService myService3 = new StaticObjectService("3");

    [RegisterStaticObject(Tags = "Tag1,Tag2,Tag3")]
    public static StaticObjectService myService4 = new StaticObjectService("4");

    [RegisterStaticObject(ServiceKey = "ServiceKey1")]
    public static StaticObjectService myService5 = new StaticObjectService("5");

    [RegisterStaticObject(Registration = RegistrationStrategy.SelfWithInterfaces)]
    public static StaticObjectService myService6 = new StaticObjectService("6");

    [RegisterStaticObject(Duplicate = DuplicateStrategy.Append)]
    public static StaticObjectService myService7 = new StaticObjectService("4");

    [RegisterStaticObject(Tags = "DONT_EXPORT_ME", ServiceKey = "StaticObjectFilter")]
    public static StaticObjectService myService8 = new StaticObjectService("8");

    [RegisterStaticObject(Tags = "EXPORT_ME,Alpha", ServiceKey = "StaticObjectFilter")]
    public static StaticObjectService myService9 = new StaticObjectService("9");
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
public class FactoryService1(IService1 service1) : IFactoryService1
{
    public IService1 Service { get; } = service1 ?? throw new ArgumentNullException(nameof(service1));

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

public interface IOpenGeneric2<T>
{
}

[RegisterSingleton]
public class OpenGeneric2<T> : IOpenGeneric2<T>
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

[RegisterSingleton(ServiceKey = "Alpha")]
public class ServiceAlphaKeyed : IServiceKeyed
{
}

[RegisterSingleton(ServiceKey = "Beta")]
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

[RegisterScoped]
public class Service : IService<Input, string?>;
public class Input : IInput<string?>;

public interface IService<TInput, TOutput> where TInput : IInput<TOutput>;
public interface IInput<TOutput>;
