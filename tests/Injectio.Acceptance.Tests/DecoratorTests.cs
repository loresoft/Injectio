using AwesomeAssertions;

using Injectio.Acceptance.Tests.Services;

using Microsoft.Extensions.DependencyInjection;

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
