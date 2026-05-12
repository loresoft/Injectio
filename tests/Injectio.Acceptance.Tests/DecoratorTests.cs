using AwesomeAssertions;

using Injectio.Acceptance.Tests.Services;

using Microsoft.Extensions.DependencyInjection;

using XUnit.Hosting.Logging;

namespace Injectio.Acceptance.Tests;

[Collection(DependencyInjectionCollection.CollectionName)]
public class DecoratorTests(DependencyInjectionFixture fixture) : DependencyInjectionBase(fixture)
{
    [Fact]
    public void ShouldResolveChainedDecoratorsInOrder()
    {
        var greeter = Services.GetRequiredService<IGreeter>();

        greeter.Should().BeOfType<CachingGreeter>();
        greeter.Greet().Should().Be("caching(logging(base))");

        var caching = (CachingGreeter)greeter;
        caching.Inner.Should().BeOfType<LoggingGreeter>();

        var logging = (LoggingGreeter)caching.Inner;
        logging.Inner.Should().BeOfType<BaseGreeter>();
    }

    [Fact]
    public void ShouldDecorateClosedGenericViaOpenDecorator()
    {
        var repo = Services.GetRequiredService<IRepo<string>>();

        repo.Should().BeOfType<LoggingRepo<string>>();
        repo.Describe().Should().Be("logging(repo<string>)");
    }

    [Fact]
    public void ShouldDecorateKeyedClosedGenericViaOpenDecorator()
    {
        var repo = Services.GetRequiredKeyedService<IRepo<int>>("numbers");

        repo.Should().BeOfType<KeyedLoggingRepo<int>>();
        repo.Describe().Should().Be("keyed-logging(repo<Int32>)");
    }

    [Fact]
    public void ShouldDecorateUsingFactory()
    {
        var greeter = Services.GetRequiredService<IFactoryGreeter>();

        greeter.Should().BeOfType<FactoryGreeterDecorator>();
        greeter.Greet().Should().Be("factory(factory-base)");
    }

    [Fact]
    public void ShouldDecorateKeyedUsingFactory()
    {
        var thing = Services.GetRequiredKeyedService<IKeyedFactoryThing>("factory");

        thing.Should().BeOfType<KeyedFactoryThingDecorator>();
        thing.Name.Should().Be("factory-factory(keyed-factory)");
    }

    [Fact]
    public void ShouldDecorateWhenTagsMatch()
    {
        var greeter = Services.GetRequiredService<ITaggedGreeter>();

        greeter.Should().BeOfType<TaggedGreeterDecorator>();
        greeter.Greet().Should().Be("tagged(tagged-base)");
    }

    [Fact]
    public void ShouldDecorateWithAdditionalDependency()
    {
        var greeter = Services.GetRequiredService<ILoggerGreeter>();

        greeter.Should().BeOfType<LoggerGreeterDecorator>();
        greeter.Greet().Should().Be("logger(logger-base)");

        var decorator = (LoggerGreeterDecorator)greeter;
        decorator.Inner.Should().BeOfType<LoggerGreeter>();
        decorator.Logger.Should().NotBeNull();

        // Retrieve the MemoryLoggerProvider to access the logs
        var memoryLoggerProvider = Services.GetRequiredService<MemoryLoggerProvider>();
        memoryLoggerProvider.Should().NotBeNull();

        // Verify that the expected log entry was created
        var logs = memoryLoggerProvider.Logs();

        logs.Should().ContainSingle(log =>
            log.Category == "Injectio.Acceptance.Tests.Services.LoggerGreeterDecorator"
            && log.Message == "logger logger-base"
        );
    }

    [Fact]
    public void ShouldDecorateEveryKeyedVariantWhenAnyKey()
    {
        var alpha = Services.GetRequiredKeyedService<IKeyedThing>("alpha");
        var beta = Services.GetRequiredKeyedService<IKeyedThing>("beta");

        alpha.Should().BeOfType<WrappedThing>();
        alpha.Name.Should().Be("wrapped(alpha)");

        beta.Should().BeOfType<WrappedThing>();
        beta.Name.Should().Be("wrapped(beta)");
    }
}
