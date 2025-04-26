using FluentAssertions;

using Injectio.Acceptance.Tests.Services;

using Microsoft.Extensions.DependencyInjection;

using Xunit.Abstractions;

namespace Injectio.Acceptance.Tests;

[Collection(DependencyInjectionCollection.CollectionName)]
public class LocalServiceTests(ITestOutputHelper output, DependencyInjectionFixture fixture) : DependencyInjectionBase(output, fixture)
{
    [Fact]
    public void ShouldResolveLocalService()
    {
        var localService = Services.GetRequiredService<ILocalService>();
        localService.Should().NotBeNull();
        localService.Should().BeOfType<LocalService>();

        var localAttributeService = Services.GetRequiredService<ILocalAttributeService>();
        localAttributeService.Should().NotBeNull();
        localAttributeService.Should().BeOfType<LocalAttributeService>();
    }

    [Fact]
    public void ShouldResolveLocalServiceSameInstance()
    {
        var localAttributeInstance = Services.GetRequiredService<LocalAttributeService>();
        localAttributeInstance.Should().NotBeNull();
        localAttributeInstance.Should().BeOfType<LocalAttributeService>();

        var localAttributeService = Services.GetRequiredService<ILocalAttributeService>();
        localAttributeService.Should().NotBeNull();
        localAttributeService.Should().BeOfType<LocalAttributeService>();

        localAttributeInstance.Should().BeSameAs(localAttributeService);
    }

    [Fact]
    public void ShouldResolveExplicitService()
    {
        var localService = Services.GetRequiredService<ILocalAttributeNameService>();
        localService.Should().NotBeNull();
        localService.Should().BeOfType<LocalAttributeNameService>();
    }

    [Fact]
    public void ShouldCallServiceRegistrationWithTags()
    {
        // trigger loading of the service registration
        var localService = Services.GetRequiredService<ILocalAttributeNameService>();
        localService.Should().NotBeNull();
        localService.Should().BeOfType<LocalAttributeNameService>();

        // check that the tags were set
        var tags = ServiceRegistration.Tags;
        tags.Should().NotBeNull();
        tags.Should().ContainKey(nameof(ServiceRegistration.RegisterWithTagsSet));
        tags.Should().ContainKey(nameof(ServiceRegistration.RegisterWithTagsReadOnly));
        tags.Should().ContainKey(nameof(ServiceRegistration.RegisterWithTagsCollection));
        tags.Should().ContainKey(nameof(ServiceRegistration.RegisterServices));
    }
}
