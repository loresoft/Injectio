using System;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

using AwesomeAssertions;

using Injectio.Attributes;
using Injectio.Generators;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

using Xunit;

namespace Injectio.Tests;

public class ServiceRegistrationDiagnosticTests
{
    [Fact]
    public async Task DiagnoseRegisterServicesInvalidFirstParameter()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public static class RegistrationModule
            {
                [RegisterServices]
                public static void Register(string test)
                {
                }
            }

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJ0001");
    }

    [Fact]
    public async Task DiagnoseRegisterServicesInvalidSecondParameter()
    {
        const string source = """
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

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJ0002");
    }

    [Fact]
    public async Task DiagnoseRegisterServicesTooManyParameters()
    {
        const string source = """
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

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJ0003");
    }

    [Fact]
    public async Task DiagnoseRegisterServicesNoParameters()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public static class RegistrationModule
            {
                [RegisterServices]
                public static void Register()
                {
                }
            }

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJ0001");
    }

    [Fact]
    public async Task DiagnoseFactoryMethodNotFound()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterTransient(ServiceType = typeof(IService), Factory = "NonExistentMethod")]
            public class MyService : IService
            {
            }

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJ0004");
    }

    [Fact]
    public async Task DiagnoseFactoryMethodNotStatic()
    {
        const string source = """
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

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJ0005");
    }

    [Fact]
    public async Task DiagnoseFactoryMethodInvalidSignature()
    {
        const string source = """
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

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJ0006");
    }

    [Fact]
    public async Task DiagnoseServiceTypeMismatch()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }
            public interface IOtherService { }

            [RegisterTransient(ServiceType = typeof(IOtherService))]
            public class MyService : IService
            {
            }

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJ0007");
    }

    [Fact]
    public async Task DiagnoseRegisterServicesOnAbstractClassNonStaticMethod()
    {
        const string source = """

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

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJ0009");
    }

    [Fact]
    public async Task DiagnoseAbstractImplementationTypeWithoutFactory()
    {
        const string source = """

            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton]
            public abstract class AbstractService : IService
            {
            }

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().ContainSingle(d => d.Id == "INJ0008");
    }

    [Fact]
    public async Task NoDiagnosticsForAbstractImplementationTypeWithFactory()
    {
        const string source = """

            using System;
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton(ServiceType = typeof(IService), Factory = nameof(Create))]
            public abstract class AbstractService : IService
            {
                public static IService Create(IServiceProvider serviceProvider)
                {
                    return null!;
                }
            }

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NoDiagnosticsForValidRegistration()
    {
        const string source = """

            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton]
            public class MyService : IService
            {
            }

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NoDiagnosticsForValidFactory()
    {
        const string source = """

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

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NoDiagnosticsForValidKeyedFactory()
    {
        const string source = """

            using System;
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterTransient(ServiceType = typeof(IService), ServiceKey = "key", Factory = nameof(ServiceFactory))]
            public class MyService : IService
            {
                public static IService ServiceFactory(IServiceProvider serviceProvider, object? serviceKey)
                {
                    return new MyService();
                }
            }

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NoDiagnosticsForValidRegisterServicesMethod()
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

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().BeEmpty();
    }

    [Fact]
    public async Task NoDiagnosticsForValidRegisterServicesWithTags()
    {
        const string source = """

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

            """;

        var diagnostics = await GetDiagnosticsAsync(source);

        diagnostics.Should().BeEmpty();
    }

    private static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
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

        var analyzer = new ServiceRegistrationAnalyzer();
        var compilationWithAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // return only Injectio diagnostics
        return diagnostics
            .Where(d => d.Id.StartsWith("INJ"))
            .ToImmutableArray();
    }
}
