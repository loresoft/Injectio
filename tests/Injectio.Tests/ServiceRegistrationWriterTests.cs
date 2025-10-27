using System;
using System.Collections.Generic;
using System.Threading.Tasks;

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
                Lifetime: "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton",
                ImplementationType: "Injectio.Tests.Service1",
                ServiceTypes: new[] { "Injectio.Tests.IService1" },
                ServiceKey: null,
                Factory: null,
                Duplicate: KnownTypes.DuplicateStrategySkipShortName,
                Registration: KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
                Tags: Array.Empty<string>()
            )
        };
        var staticObjectRegistrations = new List<StaticObjectRegistration>();

        var result = ServiceRegistrationWriter.GenerateExtensionClass(modeulRegistrations, registrations,staticObjectRegistrations, nameof(ServiceRegistrationWriterTests), nameof(ServiceRegistrationWriterTests), null);

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
                Lifetime: "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton",
                ImplementationType: "Injectio.Tests.ServiceMultiple",
                ServiceTypes: new[]                 {
                    "Injectio.Tests.ServiceMultiple",
                    "Injectio.Tests.IService1",
                    "Injectio.Tests.IService2",
                },
                ServiceKey: null,
                Factory: null,
                Duplicate: KnownTypes.DuplicateStrategySkipShortName,
                Registration: KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
                Tags : Array.Empty<string>()
            )
        };
        var staticObjectRegistrations = new List<StaticObjectRegistration>();

        var result = ServiceRegistrationWriter.GenerateExtensionClass(modeulRegistrations, registrations, staticObjectRegistrations, nameof(ServiceRegistrationWriterTests), nameof(ServiceRegistrationWriterTests), null);

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
                Lifetime: "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped",
                ImplementationType: "Injectio.Tests.Service1",
                ServiceTypes: new[] { "Injectio.Tests.IService1" },
                ServiceKey: null,
                Factory: null,
                Duplicate: KnownTypes.DuplicateStrategyAppendShortName,
                Registration: KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
                Tags : Array.Empty<string>()
            )
        };
        var staticObjectRegistrations = new List<StaticObjectRegistration>();


        var result = ServiceRegistrationWriter.GenerateExtensionClass(modeulRegistrations, registrations, staticObjectRegistrations, nameof(ServiceRegistrationWriterTests), nameof(ServiceRegistrationWriterTests), null);

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
                Lifetime: "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient",
                ImplementationType: "Injectio.Tests.Service1",
                ServiceTypes: new[] { "Injectio.Tests.IService1" },
                ServiceKey: null,
                Factory: null,
                Duplicate: KnownTypes.DuplicateStrategyReplaceTypeName,
                Registration: KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
                Tags : Array.Empty<string>()
            )
        };
        var staticObjectRegistrations = new List<StaticObjectRegistration>();

        var result = ServiceRegistrationWriter.GenerateExtensionClass(modeulRegistrations, registrations, staticObjectRegistrations, nameof(ServiceRegistrationWriterTests), nameof(ServiceRegistrationWriterTests), null);

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
                Lifetime: "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton",
                ImplementationType: "Injectio.Tests.Service1",
                ServiceTypes: new[] { "Injectio.Tests.IService1" },
                ServiceKey: null,
                Factory: null,
                Duplicate: KnownTypes.DuplicateStrategySkipShortName,
                Registration: KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
                Tags: new[] { "Test", "Big" }
            )
        };;
        var staticObjectRegistrations = new List<StaticObjectRegistration>();

        var result = ServiceRegistrationWriter.GenerateExtensionClass(modeulRegistrations, registrations, staticObjectRegistrations, nameof(ServiceRegistrationWriterTests), nameof(ServiceRegistrationWriterTests), null);

        return Verifier
            .Verify(result)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }
}
