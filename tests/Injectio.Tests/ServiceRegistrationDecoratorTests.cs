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

using VerifyXunit;

using Xunit;

namespace Injectio.Tests;

public class ServiceRegistrationDecoratorTests
{
    [Fact]
    public Task GenerateDecoratorSimple()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton]
            public class Service : IService { }

            [RegisterDecorator(ServiceType = typeof(IService))]
            public class LoggingDecorator : IService
            {
                public LoggingDecorator(IService inner) { }
            }
            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateDecoratorGenericAttribute()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton]
            public class Service : IService { }

            [RegisterDecorator<IService>]
            public class LoggingDecorator : IService
            {
                public LoggingDecorator(IService inner) { }
            }
            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateDecoratorChainedOrder()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton]
            public class Service : IService { }

            [RegisterDecorator<IService>(Order = 2)]
            public class CachingDecorator : IService
            {
                public CachingDecorator(IService inner) { }
            }

            [RegisterDecorator<IService>(Order = 1)]
            public class LoggingDecorator : IService
            {
                public LoggingDecorator(IService inner) { }
            }
            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateDecoratorKeyed()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton<IService>(ServiceKey = "Alpha")]
            public class Service : IService { }

            [RegisterDecorator<IService>(ServiceKey = "Alpha")]
            public class LoggingDecorator : IService
            {
                public LoggingDecorator(IService inner) { }
            }
            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateDecoratorAnyKey()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton<IService>(ServiceKey = "Alpha")]
            public class ServiceA : IService { }

            [RegisterSingleton<IService>(ServiceKey = "Beta")]
            public class ServiceB : IService { }

            [RegisterDecorator<IService>(AnyKey = true)]
            public class LoggingDecorator : IService
            {
                public LoggingDecorator(IService inner) { }
            }
            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateDecoratorFactory()
    {
        const string source = """
            using System;
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton]
            public class Service : IService { }

            [RegisterDecorator<IService>(Factory = nameof(Create))]
            public class LoggingDecorator : IService
            {
                public LoggingDecorator(IService inner) { }

                public static IService Create(IServiceProvider serviceProvider, IService inner)
                    => new LoggingDecorator(inner);
            }
            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateDecoratorOpenGeneric()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IRepo<T> { }

            [RegisterSingleton(ServiceType = typeof(IRepo<>), ImplementationType = typeof(Repo<>))]
            public class Repo<T> : IRepo<T> { }

            [RegisterDecorator(ServiceType = typeof(IRepo<>))]
            public class LoggingRepo<T> : IRepo<T>
            {
                public LoggingRepo(IRepo<T> inner) { }
            }
            """;

        return Verify(source);
    }

    [Fact]
    public Task GenerateDecoratorTags()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton(Tags = "FrontEnd")]
            public class Service : IService { }

            [RegisterDecorator<IService>(Tags = "FrontEnd")]
            public class LoggingDecorator : IService
            {
                public LoggingDecorator(IService inner) { }
            }
            """;

        return Verify(source);
    }

    // ------- Diagnostics -------

    [Fact]
    public async Task DiagnoseDecoratorDoesNotImplementService()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }
            public interface IOther { }

            [RegisterSingleton]
            public class Service : IService { }

            [RegisterDecorator(ServiceType = typeof(IService))]
            public class BadDecorator : IOther { }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        diagnostics.Should().Contain(d => d.Id == "INJ0010");
    }

    [Fact]
    public async Task DiagnoseDecoratorMissingServiceType()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton]
            public class Service : IService { }

            [RegisterDecorator]
            public class LoggingDecorator : IService
            {
                public LoggingDecorator(IService inner) { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        diagnostics.Should().Contain(d => d.Id == "INJ0011");
    }

    [Fact]
    public async Task DiagnoseDecoratorFactoryNotFound()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton]
            public class Service : IService { }

            [RegisterDecorator<IService>(Factory = "Missing")]
            public class LoggingDecorator : IService
            {
                public LoggingDecorator(IService inner) { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        diagnostics.Should().Contain(d => d.Id == "INJ0013");
    }

    [Fact]
    public async Task DiagnoseDecoratorFactoryInvalidSignature()
    {
        const string source = """
            using System;
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton]
            public class Service : IService { }

            [RegisterDecorator<IService>(Factory = nameof(Create))]
            public class LoggingDecorator : IService
            {
                public LoggingDecorator(IService inner) { }

                public IService Create() => this;
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        diagnostics.Should().Contain(d => d.Id == "INJ0014");
    }

    [Fact]
    public async Task DiagnoseDecoratorOpenGenericKeyed()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IRepo<T> { }

            [RegisterSingleton(ServiceType = typeof(IRepo<>), ImplementationType = typeof(Repo<>))]
            public class Repo<T> : IRepo<T> { }

            [RegisterDecorator(ServiceType = typeof(IRepo<>), ServiceKey = "X")]
            public class LoggingRepo<T> : IRepo<T>
            {
                public LoggingRepo(IRepo<T> inner) { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        diagnostics.Should().Contain(d => d.Id == "INJ0015");
    }

    [Fact]
    public async Task DiagnoseDecoratorTargetNotRegistered()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterDecorator<IService>]
            public class LoggingDecorator : IService
            {
                public LoggingDecorator(IService inner) { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        diagnostics.Should().Contain(d => d.Id == "INJ0016");
    }

    [Fact]
    public async Task NoDiagnosticForValidDecorator()
    {
        const string source = """
            using Injectio.Attributes;

            namespace Injectio.Sample;

            public interface IService { }

            [RegisterSingleton]
            public class Service : IService { }

            [RegisterDecorator<IService>]
            public class LoggingDecorator : IService
            {
                public LoggingDecorator(IService inner) { }
            }
            """;

        var diagnostics = await GetDiagnosticsAsync(source);
        diagnostics.Should().BeEmpty();
    }

    private static Task Verify(string source)
    {
        var output = GetAllGeneratedOutput<ServiceRegistrationGenerator>(source);

        return Verifier
            .Verify(output)
            .UseDirectory("Snapshots")
            .ScrubLinesContaining("GeneratedCodeAttribute");
    }

    private static string GetAllGeneratedOutput<T>(string source)
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
        driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

        var generated = outputCompilation.SyntaxTrees
            .Skip(originalTreeCount)
            .Select(t => $"// {System.IO.Path.GetFileName(t.FilePath)}\n{t}")
            .ToArray();

        return string.Join("\n\n// ==========\n\n", generated);
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

        return diagnostics
            .Where(d => d.Id.StartsWith("INJ"))
            .ToImmutableArray();
    }
}
