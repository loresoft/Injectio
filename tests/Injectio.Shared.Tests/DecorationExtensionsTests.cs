using AwesomeAssertions;

using Injectio.Extensions;

using Microsoft.Extensions.DependencyInjection;

namespace Injectio.Shared.Tests;

public interface IGreeter
{
    string Greet();
}

public class SimpleGreeter : IGreeter
{
    public string Greet() => "Hello";
}

public class DecoratedGreeter(IGreeter inner) : IGreeter
{
    public IGreeter Inner => inner;
    public string Greet() => $"[Decorated] {inner.Greet()}";
}

public class AnotherDecoratedGreeter(IGreeter inner) : IGreeter
{
    public IGreeter Inner => inner;
    public string Greet() => $"[Another] {inner.Greet()}";
}

public interface IRepository<T>
{
    T? Get(int id);
}

public class InMemoryRepository<T> : IRepository<T>
{
    public T? Get(int id) => default;
}

public class CachingRepository<T>(IRepository<T> inner) : IRepository<T>
{
    public IRepository<T> Inner => inner;
    public T? Get(int id) => inner.Get(id);
}

public class DecorationExtensionsTests
{
    [Fact]
    public void WhenDecorateWithFactoryThenResolvedServiceIsDecorated()
    {
        var services = new ServiceCollection();
        services.AddTransient<IGreeter, SimpleGreeter>();

        services.Decorate<IGreeter>((sp, inner) => new DecoratedGreeter(inner));

        using var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredService<IGreeter>();

        greeter.Should().BeOfType<DecoratedGreeter>();
        greeter.Greet().Should().Be("[Decorated] Hello");
    }

    [Fact]
    public void WhenDecorateWithFactoryThenLifetimeIsPreserved()
    {
        var services = new ServiceCollection();
        services.AddScoped<IGreeter, SimpleGreeter>();

        services.Decorate<IGreeter>((sp, inner) => new DecoratedGreeter(inner));

        services[0].Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void WhenDecorateWithFactoryThenReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddTransient<IGreeter, SimpleGreeter>();

        var result = services.Decorate<IGreeter>((sp, inner) => new DecoratedGreeter(inner));

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void WhenDecorateCalledMultipleTimesThenDecoratorsStack()
    {
        var services = new ServiceCollection();
        services.AddTransient<IGreeter, SimpleGreeter>();

        services.Decorate<IGreeter>((sp, inner) => new DecoratedGreeter(inner));
        services.Decorate<IGreeter>((sp, inner) => new AnotherDecoratedGreeter(inner));

        using var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredService<IGreeter>();

        greeter.Should().BeOfType<AnotherDecoratedGreeter>();
        greeter.Greet().Should().Be("[Another] [Decorated] Hello");
    }

    [Fact]
    public void WhenDecorateWithImplementationInstanceThenInstanceIsWrapped()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGreeter>(new SimpleGreeter());

        services.Decorate<IGreeter>((sp, inner) => new DecoratedGreeter(inner));

        using var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredService<IGreeter>();

        greeter.Should().BeOfType<DecoratedGreeter>();
    }

    [Fact]
    public void WhenDecorateWithFactoryRegistrationThenFactoryIsWrapped()
    {
        var services = new ServiceCollection();
        services.AddTransient<IGreeter>(sp => new SimpleGreeter());

        services.Decorate<IGreeter>((sp, inner) => new DecoratedGreeter(inner));

        using var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredService<IGreeter>();

        greeter.Should().BeOfType<DecoratedGreeter>();
    }

    [Fact]
    public void WhenNoRegistrationExistsThenDecorateIsNoOp()
    {
        var services = new ServiceCollection();

        services.Decorate<IGreeter>((sp, inner) => new DecoratedGreeter(inner));

        services.Should().BeEmpty();
    }

    [Fact]
    public void WhenDecorateWithTypeParametersThenDecoratorIsResolved()
    {
        var services = new ServiceCollection();
        services.AddTransient<IGreeter, SimpleGreeter>();

        services.Decorate<IGreeter, DecoratedGreeter>();

        using var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredService<IGreeter>();

        greeter.Should().BeOfType<DecoratedGreeter>();
    }

    [Fact]
    public void WhenDecorateKeyedServiceWithFactoryThenKeyedServiceIsDecorated()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IGreeter, SimpleGreeter>("key1");

        services.DecorateKeyed<IGreeter>("key1", (sp, key, inner) => new DecoratedGreeter(inner));

        using var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredKeyedService<IGreeter>("key1");

        greeter.Should().BeOfType<DecoratedGreeter>();
    }

    [Fact]
    public void WhenDecorateKeyedServiceThenNonKeyedIsSkipped()
    {
        var services = new ServiceCollection();
        services.AddTransient<IGreeter, SimpleGreeter>();
        services.AddKeyedTransient<IGreeter, SimpleGreeter>("key1");

        services.DecorateKeyed<IGreeter>("key1", (sp, key, inner) => new DecoratedGreeter(inner));

        using var provider = services.BuildServiceProvider();
        // Non-keyed should remain undecorated
        var nonKeyed = provider.GetRequiredService<IGreeter>();
        nonKeyed.Should().BeOfType<SimpleGreeter>();

        // Keyed should be decorated
        var keyed = provider.GetRequiredKeyedService<IGreeter>("key1");
        keyed.Should().BeOfType<DecoratedGreeter>();
    }

    [Fact]
    public void WhenDecorateKeyedWithDifferentKeyThenNotDecorated()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IGreeter, SimpleGreeter>("key1");

        services.DecorateKeyed<IGreeter>("key2", (sp, key, inner) => new DecoratedGreeter(inner));

        using var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredKeyedService<IGreeter>("key1");

        greeter.Should().BeOfType<SimpleGreeter>();
    }

    [Fact]
    public void WhenDecorateKeyedWithAnyKeyThenAllKeyedAreDecorated()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IGreeter, SimpleGreeter>("key1");
        services.AddKeyedTransient<IGreeter, SimpleGreeter>("key2");

        services.DecorateKeyed<IGreeter>(
            KeyedService.AnyKey,
            (sp, key, inner) => new DecoratedGreeter(inner));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredKeyedService<IGreeter>("key1").Should().BeOfType<DecoratedGreeter>();
        provider.GetRequiredKeyedService<IGreeter>("key2").Should().BeOfType<DecoratedGreeter>();
    }

    [Fact]
    public void WhenDecorateKeyedWithTypeParametersThenDecoratorIsResolved()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IGreeter, SimpleGreeter>("key1");

        services.DecorateKeyed<IGreeter, DecoratedGreeter>("key1");

        using var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredKeyedService<IGreeter>("key1");

        greeter.Should().BeOfType<DecoratedGreeter>();
    }

    [Fact]
    public void WhenDecorateKeyedThenLifetimeIsPreserved()
    {
        var services = new ServiceCollection();
        services.AddKeyedScoped<IGreeter, SimpleGreeter>("key1");

        services.DecorateKeyed<IGreeter>("key1", (sp, key, inner) => new DecoratedGreeter(inner));

        services[0].Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void WhenDecorateKeyedWithInstanceThenInstanceIsWrapped()
    {
        var services = new ServiceCollection();
        services.AddKeyedSingleton<IGreeter>("key1", new SimpleGreeter());

        services.DecorateKeyed<IGreeter>("key1", (sp, key, inner) => new DecoratedGreeter(inner));

        using var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredKeyedService<IGreeter>("key1");

        greeter.Should().BeOfType<DecoratedGreeter>();
    }

    [Fact]
    public void WhenDecorateKeyedWithFactoryRegistrationThenFactoryIsWrapped()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IGreeter>("key1", (sp, key) => new SimpleGreeter());

        services.DecorateKeyed<IGreeter>("key1", (sp, key, inner) => new DecoratedGreeter(inner));

        using var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredKeyedService<IGreeter>("key1");

        greeter.Should().BeOfType<DecoratedGreeter>();
    }

    [Fact]
    public void WhenDecorateOpenGenericThenClosedGenericsAreDecorated()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRepository<string>, InMemoryRepository<string>>();
        services.AddTransient<IRepository<int>, InMemoryRepository<int>>();

        services.DecorateOpenGeneric(typeof(IRepository<>), typeof(CachingRepository<>));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IRepository<string>>().Should().BeOfType<CachingRepository<string>>();
        provider.GetRequiredService<IRepository<int>>().Should().BeOfType<CachingRepository<int>>();
    }

    [Fact]
    public void WhenDecorateOpenGenericThenLifetimeIsPreserved()
    {
        var services = new ServiceCollection();
        services.AddScoped<IRepository<string>, InMemoryRepository<string>>();

        services.DecorateOpenGeneric(typeof(IRepository<>), typeof(CachingRepository<>));

        services[0].Lifetime.Should().Be(ServiceLifetime.Scoped);
    }

    [Fact]
    public void WhenDecorateOpenGenericThenReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRepository<string>, InMemoryRepository<string>>();

        var result = services.DecorateOpenGeneric(typeof(IRepository<>), typeof(CachingRepository<>));

        result.Should().BeSameAs(services);
    }

    [Fact]
    public void WhenDecorateOpenGenericThenNonGenericRegistrationsAreSkipped()
    {
        var services = new ServiceCollection();
        services.AddTransient<IGreeter, SimpleGreeter>();
        services.AddTransient<IRepository<string>, InMemoryRepository<string>>();

        services.DecorateOpenGeneric(typeof(IRepository<>), typeof(CachingRepository<>));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IGreeter>().Should().BeOfType<SimpleGreeter>();
        provider.GetRequiredService<IRepository<string>>().Should().BeOfType<CachingRepository<string>>();
    }

    [Fact]
    public void WhenDecorateOpenGenericThenOpenGenericDescriptorsAreSkipped()
    {
        var services = new ServiceCollection();
        services.AddTransient(typeof(IRepository<>), typeof(InMemoryRepository<>));

        services.DecorateOpenGeneric(typeof(IRepository<>), typeof(CachingRepository<>));

        // The open-generic descriptor should remain unchanged
        services[0].ImplementationType.Should().Be(typeof(InMemoryRepository<>));
    }

    [Fact]
    public void WhenDecorateOpenGenericWithKeyedServiceThenKeyedIsDecorated()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IRepository<string>, InMemoryRepository<string>>("cache");

        services.DecorateOpenGeneric(typeof(IRepository<>), typeof(CachingRepository<>), "cache");

        using var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredKeyedService<IRepository<string>>("cache");

        repo.Should().BeOfType<CachingRepository<string>>();
    }

    [Fact]
    public void WhenDecorateOpenGenericThenKeyedRegistrationsAreSkipped()
    {
        var services = new ServiceCollection();
        services.AddTransient<IRepository<string>, InMemoryRepository<string>>();
        services.AddKeyedTransient<IRepository<string>, InMemoryRepository<string>>("cache");

        services.DecorateOpenGeneric(typeof(IRepository<>), typeof(CachingRepository<>));

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<IRepository<string>>().Should().BeOfType<CachingRepository<string>>();
        provider.GetRequiredKeyedService<IRepository<string>>("cache").Should().BeOfType<InMemoryRepository<string>>();
    }

    [Fact]
    public void WhenDecorateOpenGenericKeyedWithDifferentKeyThenKeyedRegistrationIsSkipped()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IRepository<string>, InMemoryRepository<string>>("cache");

        services.DecorateOpenGeneric(typeof(IRepository<>), typeof(CachingRepository<>), "other");

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredKeyedService<IRepository<string>>("cache").Should().BeOfType<InMemoryRepository<string>>();
    }

    [Fact]
    public void WhenDecorateOpenGenericKeyedWithAnyKeyThenAllKeyedRegistrationsAreDecorated()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IRepository<string>, InMemoryRepository<string>>("cache");
        services.AddKeyedTransient<IRepository<int>, InMemoryRepository<int>>("audit");

        services.DecorateOpenGeneric(typeof(IRepository<>), typeof(CachingRepository<>), KeyedService.AnyKey);

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredKeyedService<IRepository<string>>("cache").Should().BeOfType<CachingRepository<string>>();
        provider.GetRequiredKeyedService<IRepository<int>>("audit").Should().BeOfType<CachingRepository<int>>();
    }

    [Fact]
    public void WhenDecorateSkipsKeyedRegistrations()
    {
        var services = new ServiceCollection();
        services.AddKeyedTransient<IGreeter, SimpleGreeter>("key1");

        services.Decorate<IGreeter>((sp, inner) => new DecoratedGreeter(inner));

        using var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredKeyedService<IGreeter>("key1");

        // Keyed registration should not be affected by non-keyed Decorate
        greeter.Should().BeOfType<SimpleGreeter>();
    }

    [Fact]
    public void WhenDecorateWithInstanceRegistrationThenSingletonLifetimePreserved()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGreeter>(new SimpleGreeter());

        services.Decorate<IGreeter>((sp, inner) => new DecoratedGreeter(inner));

        services[0].Lifetime.Should().Be(ServiceLifetime.Singleton);
    }
}
