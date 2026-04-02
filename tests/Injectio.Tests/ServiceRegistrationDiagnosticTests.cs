using System;
using System.Collections.Immutable;
using System.Linq;

using AwesomeAssertions;

using Injectio.Attributes;
using Injectio.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Injectio.Tests;

public class ServiceRegistrationDiagnosticTests
{
    [Fact]
    public void DiagnoseRegisterServicesInvalidFirstParameter()
    {
        var source = @"
using Injectio.Attributes;

namespace Injectio.Sample;

public static class RegistrationModule
{
    [RegisterServices]
    public static void Register(string test)
    {
    }
}
";

        var diagnostics = GetDiagnostics(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJECT0001");
    }

    [Fact]
    public void DiagnoseRegisterServicesInvalidSecondParameter()
    {
        var source = @"
using Injectio.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Injectio.Sample;

public static class RegistrationModule
{
    [RegisterServices]
    public static void Register(IServiceCollection services, string test)
    {
    }
}
";

        var diagnostics = GetDiagnostics(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJECT0002");
    }

    [Fact]
    public void DiagnoseRegisterServicesTooManyParameters()
    {
        var source = @"
using Injectio.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Injectio.Sample;

public static class RegistrationModule
{
    [RegisterServices]
    public static void Register(IServiceCollection services, string a, string b)
    {
    }
}
";

        var diagnostics = GetDiagnostics(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJECT0003");
    }

    [Fact]
    public void DiagnoseRegisterServicesNoParameters()
    {
        var source = @"
using Injectio.Attributes;

namespace Injectio.Sample;

public static class RegistrationModule
{
    [RegisterServices]
    public static void Register()
    {
    }
}
";

        var diagnostics = GetDiagnostics(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJECT0001");
    }

    [Fact]
    public void DiagnoseFactoryMethodNotFound()
    {
        var source = @"
using Injectio.Attributes;

namespace Injectio.Sample;

public interface IService { }

[RegisterTransient(ServiceType = typeof(IService), Factory = ""NonExistentMethod"")]
public class MyService : IService
{
}
";

        var diagnostics = GetDiagnostics(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJECT0004");
    }

    [Fact]
    public void DiagnoseFactoryMethodNotStatic()
    {
        var source = @"
using System;
using Injectio.Attributes;

namespace Injectio.Sample;

public interface IService { }

[RegisterTransient(ServiceType = typeof(IService), Factory = nameof(ServiceFactory))]
public class MyService : IService
{
    public IService ServiceFactory(IServiceProvider serviceProvider)
    {
        return new MyService();
    }
}
";

        var diagnostics = GetDiagnostics(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJECT0005");
    }

    [Fact]
    public void DiagnoseFactoryMethodInvalidSignature()
    {
        var source = @"
using Injectio.Attributes;

namespace Injectio.Sample;

public interface IService { }

[RegisterTransient(ServiceType = typeof(IService), Factory = nameof(ServiceFactory))]
public class MyService : IService
{
    public static IService ServiceFactory(string notServiceProvider)
    {
        return new MyService();
    }
}
";

        var diagnostics = GetDiagnostics(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJECT0006");
    }

    [Fact]
    public void DiagnoseServiceTypeMismatch()
    {
        var source = @"
using Injectio.Attributes;

namespace Injectio.Sample;

public interface IService { }
public interface IOtherService { }

[RegisterTransient(ServiceType = typeof(IOtherService))]
public class MyService : IService
{
}
";

        var diagnostics = GetDiagnostics(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJECT0007");
    }

    [Fact]
    public void DiagnoseRegisterServicesOnAbstractClassNonStaticMethod()
    {
        var source = @"
using Injectio.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Injectio.Sample;

public abstract class RegistrationModule
{
    [RegisterServices]
    public void Register(IServiceCollection services)
    {
    }
}
";

        var diagnostics = GetDiagnostics(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJECT0009");
    }

    [Fact]
    public void NoDiagnosticsForValidRegistration()
    {
        var source = @"
using Injectio.Attributes;

namespace Injectio.Sample;

public interface IService { }

[RegisterSingleton]
public class MyService : IService
{
}
";

        var diagnostics = GetDiagnostics(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void NoDiagnosticsForValidFactory()
    {
        var source = @"
using System;
using Injectio.Attributes;

namespace Injectio.Sample;

public interface IService { }

[RegisterTransient(ServiceType = typeof(IService), Factory = nameof(ServiceFactory))]
public class MyService : IService
{
    public static IService ServiceFactory(IServiceProvider serviceProvider)
    {
        return new MyService();
    }
}
";

        var diagnostics = GetDiagnostics(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void NoDiagnosticsForValidKeyedFactory()
    {
        var source = @"
using System;
using Injectio.Attributes;

namespace Injectio.Sample;

public interface IService { }

[RegisterTransient(ServiceType = typeof(IService), ServiceKey = ""key"", Factory = nameof(ServiceFactory))]
public class MyService : IService
{
    public static IService ServiceFactory(IServiceProvider serviceProvider, object? serviceKey)
    {
        return new MyService();
    }
}
";

        var diagnostics = GetDiagnostics(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void NoDiagnosticsForValidRegisterServicesMethod()
    {
        var source = @"
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
";

        var diagnostics = GetDiagnostics(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public void NoDiagnosticsForValidRegisterServicesWithTags()
    {
        var source = @"
using System.Collections.Generic;
using Injectio.Attributes;
using Microsoft.Extensions.DependencyInjection;

namespace Injectio.Sample;

public static class RegistrationModule
{
    [RegisterServices]
    public static void Register(IServiceCollection services, IEnumerable<string> tags)
    {
    }
}
";

        var diagnostics = GetDiagnostics(source);

        diagnostics.Should().BeEmpty();
    }

    private static ImmutableArray<Diagnostic> GetDiagnostics(string source)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
            .Select(assembly => MetadataReference.CreateFromFile(assembly.Location))
            .Concat(new[]
            {
                MetadataReference.CreateFromFile(typeof(ServiceRegistrationGenerator).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(RegisterServicesAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(IServiceCollection).Assembly.Location),
            });

        var compilation = CSharpCompilation.Create(
            "Test.Diagnostics",
            new[] { syntaxTree },
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new ServiceRegistrationGenerator();
        var driver = CSharpGeneratorDriver.Create(generator);
        driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out var diagnostics);

        // return only Injectio diagnostics (INJECT*)
        return diagnostics
            .Where(d => d.Id.StartsWith("INJECT"))
            .ToImmutableArray();
    }
}
