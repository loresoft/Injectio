using Xunit.Abstractions;

using XUnit.Hosting;

namespace Injectio.Acceptance.Tests;

[Collection(DependencyInjectionCollection.CollectionName)]
public abstract class DependencyInjectionBase(ITestOutputHelper output, DependencyInjectionFixture fixture)
    : TestHostBase<DependencyInjectionFixture>(output, fixture)
{
}
