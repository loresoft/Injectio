using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Injectio.Attributes;
using Injectio.Generators;

using VerifyXunit;

using Xunit;

namespace Injectio.Tests;


public class ServiceRegistrationWriterTests
{
    [Fact]
    public Task GenerateExtensionClassSingleton()
    {
        var modeulRegistrations = new List<ModuleRegistration>();
        var registrations = new List<ServiceRegistration>
        {
            new (
                lifetime: "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton",
                implementationType: "Injectio.Tests.Service1",
                serviceTypes: new[] { "Injectio.Tests.IService1" },
                serviceKey: null,
                factory: null,
                duplicate: KnownTypes.DuplicateStrategySkipShortName,
                registration: KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
                tags: Enumerable.Empty<string>()
            )
        };

        var result = ServiceRegistrationWriter.GenerateExtensionClass(modeulRegistrations, registrations, nameof(ServiceRegistrationWriterTests), nameof(ServiceRegistrationWriterTests));

        return Verifier
            .Verify(result)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateExtensionClassSingletonMultiple()
    {
        var modeulRegistrations = new List<ModuleRegistration>();
        var registrations = new List<ServiceRegistration>
        {
            new (
                lifetime: "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton",
                implementationType: "Injectio.Tests.ServiceMultiple",
                serviceTypes: new[]                 {
                    "Injectio.Tests.ServiceMultiple",
                    "Injectio.Tests.IService1",
                    "Injectio.Tests.IService2",
                },
                serviceKey: null,
                factory: null,
                duplicate: KnownTypes.DuplicateStrategySkipShortName,
                registration: KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
                tags: Enumerable.Empty<string>()
            )
        };

        var result = ServiceRegistrationWriter.GenerateExtensionClass(modeulRegistrations, registrations, nameof(ServiceRegistrationWriterTests), nameof(ServiceRegistrationWriterTests));

        return Verifier
            .Verify(result)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateExtensionClassScopedAppend()
    {
        var modeulRegistrations = new List<ModuleRegistration>();
        var registrations = new List<ServiceRegistration>
        {
            new (
                lifetime: "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped",
                implementationType: "Injectio.Tests.Service1",
                serviceTypes: new[] { "Injectio.Tests.IService1" },
                serviceKey: null,
                factory: null,
                duplicate: KnownTypes.DuplicateStrategyAppendShortName,
                registration: KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
                tags: Enumerable.Empty<string>()
            )
        };

        var result = ServiceRegistrationWriter.GenerateExtensionClass(modeulRegistrations, registrations, nameof(ServiceRegistrationWriterTests), nameof(ServiceRegistrationWriterTests));

        return Verifier
            .Verify(result)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateExtensionClassTransientReplace()
    {
        var modeulRegistrations = new List<ModuleRegistration>();
        var registrations = new List<ServiceRegistration>
        {
            new (
                lifetime: "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient",
                implementationType: "Injectio.Tests.Service1",
                serviceTypes: new[] { "Injectio.Tests.IService1" },
                serviceKey: null,
                factory: null,
                duplicate: KnownTypes.DuplicateStrategyReplaceTypeName,
                registration: KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
                tags: Enumerable.Empty<string>()
            )
        };

        var result = ServiceRegistrationWriter.GenerateExtensionClass(modeulRegistrations, registrations, nameof(ServiceRegistrationWriterTests), nameof(ServiceRegistrationWriterTests));

        return Verifier
            .Verify(result)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateExtensionClassSingletonTags()
    {
        var modeulRegistrations = new List<ModuleRegistration>();
        var registrations = new List<ServiceRegistration>
        {
            new (
                lifetime: "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton",
                implementationType: "Injectio.Tests.Service1",
                serviceTypes: new[] { "Injectio.Tests.IService1" },
                serviceKey: null,
                factory: null,
                duplicate: KnownTypes.DuplicateStrategySkipShortName,
                registration: KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
                tags: new[] { "Test", "Big" }
            )
        };

        var result = ServiceRegistrationWriter.GenerateExtensionClass(modeulRegistrations, registrations, nameof(ServiceRegistrationWriterTests), nameof(ServiceRegistrationWriterTests));

        return Verifier
            .Verify(result)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }
}

public interface IService1 { }

public interface IService2 { }

public class Service1 : IService1 { }

public class Service2 : IService2 { }

public class ServiceMultiple : IService1, IService2 { }

