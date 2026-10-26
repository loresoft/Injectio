using AwesomeAssertions;

using Injectio.Acceptance.Tests.Services;

using Microsoft.Extensions.DependencyInjection;

namespace Injectio.Acceptance.Tests;

[Collection(DependencyInjectionCollection.CollectionName)]
public class OrderTests(DependencyInjectionFixture fixture) : DependencyInjectionBase(fixture)
{
    [Fact]
    public void ShouldResolveAppendedServicesInOrder()
    {
        using var scope = Services.CreateScope();

        var services = scope.ServiceProvider
            .GetServices<IOrderedService>()
            .ToList();

        services.Should().HaveCount(3);
        services[0].Should().BeOfType<FirstOrderedService>();
        services[1].Should().BeOfType<SecondOrderedService>();
        services[2].Should().BeOfType<ThirdOrderedService>();
    }

    [Fact]
    public void ShouldSkipDuplicateUsingLowestOrder()
    {
        var service = Services.GetRequiredService<IOrderedSkipService>();

        service.Should().BeOfType<EarlyOrderedSkipService>();
    }
}
