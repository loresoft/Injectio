using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using Injectio.Attributes;
using Injectio.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

using VerifyXunit;

using Xunit;

namespace Injectio.Tests;

[UsesVerify]
public class ServiceRegistrationGeneratorTests
{
    [Fact]
    public Task GenerateRegisterSingletonSelfWithInterfaces()
    {
        var source = @"
using Injectio.Attributes;

namespace Injectio.Sample;

public interface IService { }

[RegisterSingleton(Registration = RegistrationStrategy.SelfWithInterfaces)]
public class SingletonService : IService
{ }
";

        var (diagnostics, output) = GetGeneratedOutput<ServiceRegistrationGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateRegisterScopedSelfWithInterfaces()
    {
        var source = @"
using Injectio.Attributes;

namespace Injectio.Sample;

public interface IService { }

[RegisterScoped]
public class SingletonService : IService
{ }
";

        var (diagnostics, output) = GetGeneratedOutput<ServiceRegistrationGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateRegisterTransientSelfWithInterfaces()
    {
        var source = @"
using Injectio.Attributes;

namespace Injectio.Sample;

public interface IService { }

[RegisterTransient]
public class SingletonService : IService
{ }
";

        var (diagnostics, output) = GetGeneratedOutput<ServiceRegistrationGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateRegisterMixedWithInterfaces()
    {
        var source = @"
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
";

        var (diagnostics, output) = GetGeneratedOutput<ServiceRegistrationGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateRegisterSingletonImplementedInterfaces()
    {
        var source = @"
using Injectio.Attributes;

namespace Injectio.Sample;

public interface IService { }

[RegisterSingleton(Registration = RegistrationStrategy.ImplementedInterfaces)]
public class SingletonService : IService
{ }
";

        var (diagnostics, output) = GetGeneratedOutput<ServiceRegistrationGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateRegisterSingletonImplementedInterfacesMultiple()
    {
        var source = @"
using Injectio.Attributes;

namespace Injectio.Sample;

public interface IService1 { }

public interface IService2 { }

[RegisterSingleton(Registration = RegistrationStrategy.ImplementedInterfaces)]
public class SingletonService : IService1, IService2
{ }
";

        var (diagnostics, output) = GetGeneratedOutput<ServiceRegistrationGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateRegisterServicesInstance()
    {
        var source = @"
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
";

        var (diagnostics, output) = GetGeneratedOutput<ServiceRegistrationGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateRegisterServicesStatic()
    {
        var source = @"
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
";

        var (diagnostics, output) = GetGeneratedOutput<ServiceRegistrationGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateRegisterSingletonFactory()
    {
        var source = @"
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
";

        var (diagnostics, output) = GetGeneratedOutput<ServiceRegistrationGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateRegisterSingletonOpenGeneric()
    {
        var source = @"
using Injectio.Attributes;

namespace Injectio.Sample;

public interface IOpenGeneric<T>
{ }

[RegisterSingleton(ImplementationType = typeof(OpenGeneric<>), ServiceType = typeof(IOpenGeneric<>))]
public class OpenGeneric<T> : IOpenGeneric<T>
{ }
";

        var (diagnostics, output) = GetGeneratedOutput<ServiceRegistrationGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateRegisterSingletonTags()
    {
        var source = @"
using Injectio.Attributes;

namespace Injectio.Sample;

public interface IServiceTag
{ }

[RegisterSingleton(Tags = ""Client,FrontEnd"")]
public class ServiceTag : IServiceTag
{ }
";

        var (diagnostics, output) = GetGeneratedOutput<ServiceRegistrationGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    [Fact]
    public Task GenerateRegisterSingletonServiceKeys()
    {
        var source = @"
using Injectio.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Injectio.Sample;

[RegisterSingleton<IServiceKeyed>(ServiceKey = ""Alpha"")]
public class ServiceAlphaKeyed : IServiceKeyed
{ }

[RegisterSingleton<IServiceKeyed>(ServiceKey = ""Beta"")]
public class ServiceBetaKeyed : IServiceKeyed
{ }

[RegisterSingleton<IServiceKeyed>(ServiceKey = ServiceType.Alpha)]
public class ServiceAlphaTypeKeyed : IServiceKeyed
{ }

[RegisterSingleton<IServiceKeyed>(ServiceKey = ServiceType.Beta)]
public class ServiceBetaTypeKeyed : IServiceKeyed
{ }

[RegisterSingleton<IServiceKeyed>(ServiceKey = ""Charlie"", Factory = nameof(ServiceFactory))]
[RegisterSingleton<IServiceKeyed>(ServiceKey = ""Delta"", Factory = nameof(ServiceFactory))]
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
";

        var (diagnostics, output) = GetGeneratedOutput<ServiceRegistrationGenerator>(source);

        diagnostics.Should().BeEmpty();

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }



    private static (ImmutableArray<Diagnostic> Diagnostics, string Output) GetGeneratedOutput<T>(string source)
        where T : IIncrementalGenerator, new()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(T).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(RegisterServicesAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
            });

        var compilation = CSharpCompilation.Create(
            "Test.Generator",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var originalTreeCount = compilation.SyntaxTrees.Length;
        var generator = new T();

        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var diagnostics);

        var trees = outputCompilation.SyntaxTrees.ToList();

        return (diagnostics, trees.Count != originalTreeCount ? trees[^1].ToString() : string.Empty);
    }
}
