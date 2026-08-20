using AwesomeAssertions;

using Injectio.Acceptance.Tests.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Injectio.Acceptance.Tests;

[Collection(DependencyInjectionCollection.CollectionName)]
public class IdempotentRegistrationTests
{
    [Fact]
    public void WhenServiceRegistrationIsCalledTwiceThenServiceModulesRunOnce()
    {
        var services = new ServiceCollection();
        var invocationCount = ServiceRegistration.ServiceInvocationCount;

        services.AddInjectioAcceptanceTests("Alpha");
        services.AddInjectioAcceptanceTests("Beta");

        ServiceRegistration.ServiceInvocationCount.Should().Be(invocationCount + 1);
    }

    [Fact]
    public void WhenServiceRegistrationPrecedesHostRegistrationThenEachPortionRunsOnce()
    {
        var builder = Host.CreateApplicationBuilder();
        var invocationCounts = (
            Service: ServiceRegistration.ServiceInvocationCount,
            Host: ServiceRegistration.HostInvocationCount);

        builder.Services.AddInjectioAcceptanceTests();
        builder.AddInjectioAcceptanceTests();

        var actual = (
            Service: ServiceRegistration.ServiceInvocationCount,
            Host: ServiceRegistration.HostInvocationCount);
        actual.Should().Be((invocationCounts.Service + 1, invocationCounts.Host + 1));
    }

    [Fact]
    public void WhenHostRegistrationIsCalledTwiceThenEachPortionRunsOnce()
    {
        var builder = Host.CreateApplicationBuilder();
        var invocationCounts = (
            Service: ServiceRegistration.ServiceInvocationCount,
            Host: ServiceRegistration.HostInvocationCount);

        builder.AddInjectioAcceptanceTests();
        builder.AddInjectioAcceptanceTests();

        var actual = (
            Service: ServiceRegistration.ServiceInvocationCount,
            Host: ServiceRegistration.HostInvocationCount);
        actual.Should().Be((invocationCounts.Service + 1, invocationCounts.Host + 1));
    }
}
