using AwesomeAssertions;

using Injectio.Tests.Library;

using Microsoft.Extensions.DependencyInjection;

using Xunit.Abstractions;

namespace Injectio.Acceptance.Tests;

[Collection(DependencyInjectionCollection.CollectionName)]
public class LibraryServiceTests(ITestOutputHelper output, DependencyInjectionFixture fixture) : DependencyInjectionBase(output, fixture)
{
    [Fact]
    public void ShouldResolveService()
    {
        var service1 = Services.GetService<IService1>();
        service1.Should().NotBeNull();
        service1.Should().BeOfType<ServiceMultiple>();
    }

    [Fact]
    public void ShouldResolveDuplicateStrategyAppend()
    {
        var services = Services.GetServices<IService2>();
        services.Should().NotBeNull();
        services.Count().Should().Be(2);

        services.Should().ContainSingle(s => s.GetType() == typeof(ServiceMultiple));
        services.Should().ContainSingle(s => s.GetType() == typeof(ScopedWithInterfacesService2));
    }

    [Fact]
    public void ShouldResolveServiceWithFactory()
    {
        var factoryService1 = Services.GetService<IFactoryService1>();
        factoryService1.Should().NotBeNull();
        factoryService1.Should().BeOfType<FactoryService1>();

        var factoryInstance = factoryService1 as FactoryService1;
        factoryInstance.Should().NotBeNull();

        var service1 = factoryInstance.Service;
        service1.Should().NotBeNull();
        service1.Should().BeOfType<ServiceMultiple>();
    }

    [Fact]
    public void ShouldResolveServiceWithModule()
    {
        var moduleService = Services.GetService<IModuleService>();
        moduleService.Should().NotBeNull();
        moduleService.Should().BeOfType<ModuleService>();
    }

    [Fact]
    public void ShouldResolveOpenGeneric()
    {
        var generic = Services.GetService<IOpenGeneric<string>>();
        generic.Should().NotBeNull();
        generic.Should().BeOfType<OpenGeneric<string>>();

        var generic2 = Services.GetService<IOpenGeneric2<string>>();
        generic2.Should().NotBeNull();
        generic2.Should().BeOfType<OpenGeneric2<string>>();
    }

    [Fact]
    public void ShouldNotResolveTaggedService()
    {
        var tagService = Services.GetService<IServiceTag>();
        tagService.Should().BeNull();
    }

    [Fact]
    public void ShouldResolveKeyedService()
    {
        var alpaService = Services.GetKeyedService<IServiceKeyed>("Alpha");
        alpaService.Should().NotBeNull();
        alpaService.Should().BeOfType<ServiceAlphaKeyed>();

        var betaService = Services.GetKeyedService<IServiceKeyed>("Beta");
        betaService.Should().NotBeNull();
        betaService.Should().BeOfType<ServiceBetaKeyed>();

        var alpaTypeService = Services.GetKeyedService<IServiceKeyed>(ServiceType.Alpha);
        alpaTypeService.Should().NotBeNull();
        alpaTypeService.Should().BeOfType<ServiceAlphaTypeKeyed>();

        var betaTypeService = Services.GetKeyedService<IServiceKeyed>(ServiceType.Beta);
        betaTypeService.Should().NotBeNull();
        betaTypeService.Should().BeOfType<ServiceBetaTypeKeyed>();

        var charlieService = Services.GetKeyedService<IServiceKeyed>("Charlie");
        charlieService.Should().NotBeNull();
        charlieService.Should().BeOfType<ServiceFactoryKeyed>();

        var charlieInstance = charlieService as ServiceFactoryKeyed;
        charlieInstance.Should().NotBeNull();
        charlieInstance.ServiceKey.Should().Be("Charlie");

        var deltaService = Services.GetKeyedService<IServiceKeyed>("Delta");
        deltaService.Should().NotBeNull();
        deltaService.Should().BeOfType<ServiceFactoryKeyed>();

        var deltaInstance = deltaService as ServiceFactoryKeyed;
        deltaInstance.Should().NotBeNull();
        deltaInstance.ServiceKey.Should().Be("Delta");

        var blahService = Services.GetKeyedService<IServiceKeyed>("Blah");
        blahService.Should().BeNull();
    }

    [Fact]
    public void ShouldResolveServiceGeneric()
    {
        var serviceInterface = Services.GetService<IService<Input, string?>>();
        serviceInterface.Should().NotBeNull();
        serviceInterface.Should().BeOfType<Service>();

        var serviceInstance = Services.GetService<Service>();
        serviceInstance.Should().NotBeNull();

        serviceInstance.Should().BeSameAs(serviceInterface);
    }
}
