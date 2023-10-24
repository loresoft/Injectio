using Injectio.Attributes;
using Injectio.Tests.Library;

using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddInjectioTestsLibrary("Client", "Server");
services.AddConsoleLibrary();

var provider = services.BuildServiceProvider();

var localService = provider.GetRequiredService<ILocalService>();
var service1 = provider.GetRequiredService<IService1>();
var multiple2 = provider.GetRequiredService<IEnumerable<IService2>>();
var implementation = provider.GetRequiredService<ScopedWithInterfacesService2>();
var factory1 = provider.GetRequiredService<IFactoryService1>();

var module = provider.GetRequiredService<IModuleService>();

var generic = provider.GetRequiredService<IOpenGeneric<string>>();
var tagService = provider.GetService<IServiceTag>();

Console.WriteLine("Complete");

public interface ILocalService { }

[RegisterSingleton(Registration = RegistrationStrategy.SelfWithInterfaces, Duplicate = DuplicateStrategy.Replace)]
public class LocalService : ILocalService { }

public interface ILocalAttributeService { }

[RegisterSingleton<ILocalAttributeService>]
public class LocalAttributeService : ILocalService, IService1 { }


public interface ILocalAttributeNameService { }

[RegisterSingleton<ILocalAttributeNameService, LocalAttributeNameService>]
public class LocalAttributeNameService : ILocalAttributeNameService, ILocalAttributeService { }

public static class ServiceRegistration
{
    [RegisterServices]
    public static void RegisterWithTagsSet(IServiceCollection services, ISet<string> tags)
    {

    }

    [RegisterServices]
    public static void RegisterWithTagsReadOnly(IServiceCollection services, IReadOnlySet<string> tags)
    {

    }

    [RegisterServices]
    public static void RegisterWithTagsCollection(IServiceCollection services, ICollection<string> tags)
    {

    }

    [RegisterServices]
    public static void RegisterServices(IServiceCollection services)
    {

    }

}

public class ServiceRegistrationInstance
{
    [RegisterServices]
    public void RegisterWithTagsSet(IServiceCollection services, ISet<string> tags)
    {

    }

    [RegisterServices]
    public void RegisterWithTagsReadOnly(IServiceCollection services, IReadOnlySet<string> tags)
    {

    }

    [RegisterServices]
    public void RegisterWithTagsCollection(IServiceCollection services, ICollection<string> tags)
    {

    }

    [RegisterServices]
    public void RegisterServices(IServiceCollection services)
    {

    }
}
