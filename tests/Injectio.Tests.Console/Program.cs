using Injectio.Attributes;
using Injectio.Tests.Library;

using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();
services.AddInjectioTestsLibrary();
services.AddInjectioTestsConsole();

var provider = services.BuildServiceProvider();

var localService = provider.GetRequiredService<ILocalService>();
var service1 = provider.GetRequiredService<IService1>();
var multiple2 = provider.GetRequiredService<IEnumerable<IService2>>();
var factory1 = provider.GetRequiredService<IFactoryService1>();

var module = provider.GetRequiredService<IModuleService>();

Console.ReadLine();


public interface ILocalService { }


[RegisterSingleton(Registration = RegistrationStrategy.SelfWithInterfaces)]
public class LocalService : ILocalService { }

