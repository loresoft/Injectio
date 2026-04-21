using XUnit.Hosting;

namespace Injectio.Acceptance.Tests;

[Collection(DependencyInjectionCollection.CollectionName)]
public abstract class DependencyInjectionBase(DependencyInjectionFixture fixture)
    : TestHostBase<DependencyInjectionFixture>(fixture)
{
}
