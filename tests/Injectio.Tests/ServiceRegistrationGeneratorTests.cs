using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using AwesomeAssertions;

using Injectio.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

using VerifyXunit;

using Xunit;

namespace Injectio.Tests;

public class ServiceRegistrationGeneratorTests
{
    [Fact]
    public Task GenerateRegisterSingletonSelfWithInterfaces()
    {
        const string source = """

            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton(Registration = RegistrationStrategy.SelfWithInterfaces)]
            public class SingletonService : IService
            { }

            """;

        return Verify(source);
    }

    [Fact]
    public void GenerateRegisterServicesHostApplicationBuilder()
    {
        const string source = """

            using System.Collections.Generic;
            using Injectio.Attributes;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.Hosting;

            namespace Injectio.Sample;

            public static class ServiceCollectionModule
            {
                [RegisterServices]
                public static void Register(IServiceCollection services)
                {
                }
            }

            public static class StaticHostModule
            {
                [RegisterServices]
                public static void Register(IHostApplicationBuilder hostApplicationBuilder, IReadOnlySet<string> tags)
                {
                }
            }

            public class InstanceHostModule
            {
                [RegisterServices]
                public void Register(IHostApplicationBuilder hostApplicationBuilder)
                {
                }
            }

            """;

        var output = GetGeneratedOutput<ServiceRegistrationGenerator>(source, includeHostingReference: true);

        var serviceGuardIndex = output.IndexOf("typeof(ServiceRegistrationMarker)", StringComparison.Ordinal);
        var serviceModuleIndex = output.IndexOf("global::Injectio.Sample.ServiceCollectionModule.Register(serviceCollection);", StringComparison.Ordinal);
        var forwardingIndex = output.IndexOf("hostApplicationBuilder.Services.AddTestGenerator(tags);", StringComparison.Ordinal);
        var hostGuardIndex = output.IndexOf("typeof(HostRegistrationMarker)", StringComparison.Ordinal);
        var staticHostModuleIndex = output.IndexOf("global::Injectio.Sample.StaticHostModule.Register(hostApplicationBuilder, tagSet);", StringComparison.Ordinal);
        var instanceHostModuleIndex = output.IndexOf("module0001.Register(hostApplicationBuilder);", StringComparison.Ordinal);

        (serviceGuardIndex >= 0
            && serviceModuleIndex > serviceGuardIndex
            && forwardingIndex > serviceModuleIndex
            && hostGuardIndex > forwardingIndex
            && staticHostModuleIndex > hostGuardIndex
            && instanceHostModuleIndex > staticHostModuleIndex).Should().BeTrue();
        output.Should().Contain("private sealed class ServiceRegistrationMarker;");
        output.Should().Contain("private sealed class HostRegistrationMarker;");
    }

    [Fact]
    public void GenerateRegisterServicesInvalidHostApplicationBuilderMethod()
    {
        const string source = """

            using Injectio.Attributes;
            using Microsoft.Extensions.Hosting;

            namespace Injectio.Sample;

            public static class InvalidHostModule
            {
                [RegisterServices]
                public static void Register(IHostApplicationBuilder hostApplicationBuilder, string tag)
                {
                }
            }

            """;

        var output = GetGeneratedOutput<ServiceRegistrationGenerator>(source, includeHostingReference: true);

        output.Should().NotContain("global::Injectio.Sample.InvalidHostModule.Register");
    }

    [Fact]
    public void DoesNotGenerateHostApplicationBuilderOverloadWithoutHostingReference()
    {
        const string source = """

            using Injectio.Attributes;
            using Microsoft.Extensions.DependencyInjection;

            namespace Injectio.Sample;

            public static class RegistrationModule
            {
                [RegisterServices]
                public static void Register(IServiceCollection services)
                {
                }
            }

            """;

        var output = GetGeneratedOutput<ServiceRegistrationGenerator>(source, includeHostingReference: false);

        output.Should().NotContain("global::Microsoft.Extensions.Hosting.IHostApplicationBuilder");
        output.Should().Contain("private sealed class ServiceRegistrationMarker;");
        output.Should().NotContain("HostRegistrationMarker");
    }

    [Fact]
    public Task GenerateRegisterScopedSelfWithInterfaces()
    {
        const string source = """

            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterScoped]
            public class SingletonService : IService
            { }

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterScopedKeyedSelfWithInterfaces()
    {
        const string source = """

            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterScoped(ServiceKey = "Alpha")]
            public class SingletonService : IService
            { }

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterTransientSelfWithInterfaces()
    {
        const string source = """

            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterTransient]
            public class SingletonService : IService
            { }

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterMixedWithInterfaces()
    {
        const string source = """

            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService1
            {
            }

            public interface IService2
            {
            }

            [RegisterTransient(ServiceType = typeof(IService1))]
            [RegisterScoped(ServiceType = typeof(IService2), Duplicate = DuplicateStrategy.Append)]
            public class ServiceMultiple : IService1, IService2
            {
            }

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterSingletonImplementedInterfaces()
    {
        const string source = """

            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton(Registration = RegistrationStrategy.ImplementedInterfaces)]
            public class SingletonService : IService
            { }

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterSingletonImplementedInterfacesMultiple()
    {
        const string source = """

            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService1 { }

            public interface IService2 { }

            [RegisterSingleton(Registration = RegistrationStrategy.ImplementedInterfaces)]
            public class SingletonService : IService1, IService2
            { }

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterServicesInstance()
    {
        const string source = """

            using System;
            using Injectio.Attributes;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.DependencyInjection.Extensions;

            namespace Injectio.Sample;

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

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterServicesStatic()
    {
        const string source = """

            using Injectio.Attributes;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.DependencyInjection.Extensions;

            namespace Injectio.Sample;

            public interface IModuleService
            {
            }

            public class ModuleService : IModuleService
            {
            }

            public static class RegistrationModule
            {
                [RegisterServices]
                public static void Register(IServiceCollection services)
                {
                    services.TryAddTransient<IModuleService, ModuleService>();
                }
            }

            """;

        return Verify(source);
    }

    [Fact]
    public void GenerateRegisterServicesMultipleStaticMethodsInSameClass()
    {
        const string source = """

            using Injectio.Attributes;
            using Microsoft.Extensions.DependencyInjection;

            namespace Injectio.Sample;

            public static class RegistrationModule
            {
                [RegisterServices]
                public static void RegisterFirst(IServiceCollection services)
                {
                }

                [RegisterServices]
                public static void RegisterSecond(IServiceCollection services)
                {
                }
            }

            """;

        var output = GetGeneratedOutput<ServiceRegistrationGenerator>(source);

        (output.Contains("global::Injectio.Sample.RegistrationModule.RegisterFirst(serviceCollection);", StringComparison.Ordinal)
            && output.Contains("global::Injectio.Sample.RegistrationModule.RegisterSecond(serviceCollection);", StringComparison.Ordinal))
            .Should().BeTrue();
    }

    [Fact]
    public Task GenerateRegisterServicesInvalidMethod()
    {
        const string source = """

            using Injectio.Attributes;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.DependencyInjection.Extensions;

            namespace Injectio.Sample;

            public interface IModuleService
            {
            }

            public class ModuleService : IModuleService
            {
            }

            public static class RegistrationModule
            {
                [RegisterServices]
                public static void Register(IServiceCollection services, string test)
                {
                    services.TryAddTransient<IModuleService, ModuleService>();
                }
            }

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterServicesInvalidService()
    {
        const string source = """

            using Injectio.Attributes;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.DependencyInjection.Extensions;

            namespace Injectio.Sample;

            public interface IModuleService
            {
            }

            public class ModuleService : IModuleService
            {
            }

            public static class RegistrationModule
            {
                [RegisterServices]
                public static void Register(string test)
                {
                    services.TryAddTransient<IModuleService, ModuleService>();
                }
            }

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterSingletonFactory()
    {
        const string source = """

            using System;
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService
            { }

            public interface IFactoryService
            { }

            [RegisterTransient(ServiceType = typeof(IFactoryService), Factory = nameof(ServiceFactory))]
            public class FactoryService : IFactoryService
            {
                private readonly IService _service;

                public FactoryService1(IService service1)
                {
                    _service1 = service1;
                }

                public static IFactoryService1 ServiceFactory(IServiceProvider serviceProvider)
                {
                    return new FactoryService1(serviceProvider.GetRequiredService<IService>());
                }
            }

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterSingletonOpenGeneric()
    {
        const string source = """

            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IOpenGeneric<T>
            { }

            [RegisterSingleton(ImplementationType = typeof(OpenGeneric<>), ServiceType = typeof(IOpenGeneric<>))]
            public class OpenGeneric<T> : IOpenGeneric<T>
            { }

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterSingletonSelfAsOpenGeneric()
    {
        const string source = """

            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IOpenGeneric<T>
            { }

            [RegisterSingleton]
            public class OpenGeneric<T> : IOpenGeneric<T>
            { }

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterSingletonSelfAsClosedGeneric()
    {
        const string source = """

            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IClosedGeneric<T>
            { }

            [RegisterSingleton]
            public class Service : IClosedGeneric<object>
            { }

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterSingletonTags()
    {
        const string source = """

            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IServiceTag
            { }

            [RegisterSingleton(Tags = "Client,FrontEnd")]
            public class ServiceTag : IServiceTag
            { }

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterSingletonGeneric()
    {
        const string source = """

            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton<IService, SingletonService>(Duplicate = DuplicateStrategy.Replace)]
            public class SingletonService : IService
            { }

            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateRegisterSingletonServiceKeys()
    {
        const string source = """

            using System;
            using Injectio.Attributes;
            using Microsoft.Extensions.DependencyInjection;
            using Microsoft.Extensions.DependencyInjection.Extensions;

            namespace Injectio.Sample;

            public interface IServiceKeyed
            { }

            [RegisterSingleton(ServiceType = typeof(IServiceKeyed), ServiceKey = "Alpha")]
            public class ServiceAlphaKeyed : IServiceKeyed
            { }

            [RegisterSingleton(ServiceType = typeof(IServiceKeyed), ServiceKey = "Beta")]
            public class ServiceBetaKeyed : IServiceKeyed
            { }

            [RegisterSingleton(ServiceType = typeof(IServiceKeyed), ServiceKey = ServiceType.Alpha)]
            public class ServiceAlphaTypeKeyed : IServiceKeyed
            { }

            [RegisterSingleton(ServiceType = typeof(IServiceKeyed), ServiceKey = ServiceType.Beta)]
            public class ServiceBetaTypeKeyed : IServiceKeyed
            { }

            [RegisterSingleton(ServiceType = typeof(IServiceKeyed), ServiceKey = "Charlie", Factory = nameof(ServiceFactory))]
            [RegisterSingleton(ServiceType = typeof(IServiceKeyed), ServiceKey = "Delta", Factory = nameof(ServiceFactory))]
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

            """;

        return Verify(source);
    }


    private static Task Verify(string source)
    {
        var output = GetGeneratedOutput<ServiceRegistrationGenerator>(source);

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    private static string GetGeneratedOutput<T>(string source, bool includeHostingReference = false)
        where T : IIncrementalGenerator, new()
    {
        var parseOptions = CSharpParseOptions.Default.WithPreprocessorSymbols(
            "NET7_0_OR_GREATER",
            "NET8_0_OR_GREATER",
            "NET9_0_OR_GREATER",
            "NET10_0_OR_GREATER");

        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var hostingAssembly = typeof(Microsoft.Extensions.Hosting.IHostApplicationBuilder).Assembly;
        IEnumerable<MetadataReference> references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic
                && !string.IsNullOrWhiteSpace(assembly.Location)
                && assembly != hostingAssembly)
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(
            [
                MetadataReference.CreateFromFile(typeof(T).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
            ]);

        if (includeHostingReference)
            references = references.Append(MetadataReference.CreateFromFile(hostingAssembly.Location));

        var compilation = CSharpCompilation.Create(
            "Test.Generator",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var originalTreeCount = compilation.SyntaxTrees.Length;
        var generator = new T();

        var driver = CSharpGeneratorDriver.Create(
            generators: [generator.AsSourceGenerator()],
            parseOptions: parseOptions);

        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var generated = outputCompilation.SyntaxTrees
            .Skip(originalTreeCount)
            .FirstOrDefault(t => Path.GetFileName(t.FilePath) == "Injectio.g.cs");

        return generated?.ToString() ?? string.Empty;
    }
}
