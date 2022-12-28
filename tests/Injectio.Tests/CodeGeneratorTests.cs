using System.Collections.Generic;
using System.Threading.Tasks;

using Injectio.Attributes;
using Injectio.Generators;

using VerifyXunit;

using Xunit;

namespace Injectio.Tests;


[UsesVerify]
public class CodeGeneratorTests
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
                factory: null,
                duplicate: DuplicateStrategy.Skip,
                registration: RegistrationStrategy.SelfWithInterfaces
            )
        };

        var result = ServiceRegistrationWriter.GenerateExtensionClass(modeulRegistrations, registrations, nameof(CodeGeneratorTests), nameof(CodeGeneratorTests), true);

        return Verifier.Verify(result).UseDirectory("Snapshots");
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
                factory: null,
                duplicate: DuplicateStrategy.Skip,
                registration: RegistrationStrategy.SelfWithInterfaces
            )
        };

        var result = ServiceRegistrationWriter.GenerateExtensionClass(modeulRegistrations, registrations, nameof(CodeGeneratorTests), nameof(CodeGeneratorTests), true);

        return Verifier.Verify(result).UseDirectory("Snapshots");
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
                factory: null,
                duplicate: DuplicateStrategy.Append,
                registration: RegistrationStrategy.SelfWithInterfaces
            )
        };

        var result = ServiceRegistrationWriter.GenerateExtensionClass(modeulRegistrations, registrations, nameof(CodeGeneratorTests), nameof(CodeGeneratorTests), true);

        return Verifier.Verify(result).UseDirectory("Snapshots");
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
                factory: null,
                duplicate: DuplicateStrategy.Replace,
                registration: RegistrationStrategy.SelfWithInterfaces
            )
        };

        var result = ServiceRegistrationWriter.GenerateExtensionClass(modeulRegistrations, registrations, nameof(CodeGeneratorTests), nameof(CodeGeneratorTests), true);

        return Verifier.Verify(result).UseDirectory("Snapshots");
    }

}

public interface IService1 { }

public interface IService2 { }

public class Service1 : IService1 { }

public class Service2 : IService2 { }

public class ServiceMultiple : IService1, IService2 { }
