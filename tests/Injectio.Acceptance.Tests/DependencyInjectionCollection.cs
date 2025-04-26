namespace Injectio.Acceptance.Tests;

[CollectionDefinition(CollectionName)]
public class DependencyInjectionCollection : ICollectionFixture<DependencyInjectionFixture>
{
    public const string CollectionName = nameof(DependencyInjectionCollection);
}
