using System.Collections.Generic;
using System.Threading.Tasks;

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
            new ServiceRegistration
            {
                ImplementationType = "Injectio.Tests.Service1",
                Lifetime = "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton",
                ServiceTypes = new HashSet<string>
                {
                    "Injectio.Tests.IService1"
                }
            }
        };

        var result = CodeGenerator.GenerateExtensionClass(modeulRegistrations, registrations, nameof(CodeGeneratorTests), nameof(CodeGeneratorTests), true);

        return Verifier.Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task GenerateExtensionClassSingletonMultiple()
    {
        var modeulRegistrations = new List<ModuleRegistration>();
        var registrations = new List<ServiceRegistration>
        {
            new ServiceRegistration
            {
                ImplementationType = "Injectio.Tests.ServiceMultiple",
                Lifetime = "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton",
                Registration = "RegistrationStrategy.SelfWithInterfaces",
                ServiceTypes = new HashSet<string>
                {
                    "Injectio.Tests.ServiceMultiple",
                    "Injectio.Tests.IService1",
                    "Injectio.Tests.IService2",
                }
            }
        };

        var result = CodeGenerator.GenerateExtensionClass(modeulRegistrations, registrations, nameof(CodeGeneratorTests), nameof(CodeGeneratorTests), true);

        return Verifier.Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task GenerateExtensionClassScopedAppend()
    {
        var modeulRegistrations = new List<ModuleRegistration>();
        var registrations = new List<ServiceRegistration>
        {
            new ServiceRegistration
            {
                ImplementationType = "Injectio.Tests.Service1",
                Lifetime = "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped",
                Duplicate = "DuplicateStrategy.Append",
                ServiceTypes = new HashSet<string>
                {
                    "Injectio.Tests.IService1"
                }
            }
        };

        var result = CodeGenerator.GenerateExtensionClass(modeulRegistrations, registrations, nameof(CodeGeneratorTests), nameof(CodeGeneratorTests), true);

        return Verifier.Verify(result).UseDirectory("Snapshots");
    }

    [Fact]
    public Task GenerateExtensionClassTransientReplace()
    {
        var modeulRegistrations = new List<ModuleRegistration>();
        var registrations = new List<ServiceRegistration>
        {
            new ServiceRegistration
            {
                ImplementationType = "Injectio.Tests.Service1",
                Lifetime = "global::Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient",
                Duplicate = "DuplicateStrategy.Replace",
                ServiceTypes = new HashSet<string>
                {
                    "Injectio.Tests.IService1"
                }
            }
        };

        var result = CodeGenerator.GenerateExtensionClass(modeulRegistrations, registrations, nameof(CodeGeneratorTests), nameof(CodeGeneratorTests), true);

        return Verifier.Verify(result).UseDirectory("Snapshots");
    }

}

public interface IService1 { }

public interface IService2 { }

public class Service1 : IService1 { }

public class Service2 : IService2 { }

public class ServiceMultiple : IService1, IService2 { }
