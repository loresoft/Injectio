using Injectio.Attributes;

using Microsoft.Extensions.Logging;

namespace Injectio.Acceptance.Tests.Services;

public interface IGreeter
{
    string Greet();
}

[RegisterSingleton<IGreeter>]
public class BaseGreeter : IGreeter
{
    public string Greet() => "base";
}

[RegisterDecorator<IGreeter>(Order = 1)]
public class LoggingGreeter : IGreeter
{
    public IGreeter Inner { get; }

    public LoggingGreeter(IGreeter inner) => Inner = inner;

    public string Greet() => $"logging({Inner.Greet()})";
}

[RegisterDecorator<IGreeter>(Order = 2)]
public class CachingGreeter : IGreeter
{
    public IGreeter Inner { get; }

    public CachingGreeter(IGreeter inner) => Inner = inner;

    public string Greet() => $"caching({Inner.Greet()})";
}

public interface IRepo<T>
{
    string Describe();
}

[RegisterSingleton<IRepo<string>, StringRepo>]
public class StringRepo : IRepo<string>
{
    public string Describe() => "repo<string>";
}

[RegisterDecorator(ServiceType = typeof(IRepo<>))]
public class LoggingRepo<T> : IRepo<T>
{
    public IRepo<T> Inner { get; }

    public LoggingRepo(IRepo<T> inner) => Inner = inner;

    public string Describe() => $"logging({Inner.Describe()})";
}

[RegisterSingleton<IRepo<int>, NumberRepo>(ServiceKey = "numbers")]
public class NumberRepo : IRepo<int>
{
    public string Describe() => "repo<Int32>";
}

[RegisterDecorator(ServiceType = typeof(IRepo<>), ServiceKey = "numbers")]
public class KeyedLoggingRepo<T> : IRepo<T>
{
    public IRepo<T> Inner { get; }

    public KeyedLoggingRepo(IRepo<T> inner) => Inner = inner;

    public string Describe() => $"keyed-logging({Inner.Describe()})";
}

public interface IFactoryGreeter
{
    string Greet();
}

[RegisterSingleton<IFactoryGreeter>]
public class FactoryGreeter : IFactoryGreeter
{
    public string Greet() => "factory-base";
}

[RegisterDecorator<IFactoryGreeter>(Factory = nameof(Create))]
public class FactoryGreeterDecorator : IFactoryGreeter
{
    public IFactoryGreeter Inner { get; }

    private FactoryGreeterDecorator(IFactoryGreeter inner) => Inner = inner;

    public static IFactoryGreeter Create(IServiceProvider serviceProvider, IFactoryGreeter inner)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return new FactoryGreeterDecorator(inner);
    }

    public string Greet() => $"factory({Inner.Greet()})";
}

public interface ITaggedGreeter
{
    string Greet();
}

[RegisterSingleton<ITaggedGreeter>(Tags = "Alpha")]
public class TaggedGreeter : ITaggedGreeter
{
    public string Greet() => "tagged-base";
}

[RegisterDecorator<ITaggedGreeter>(Tags = "Alpha")]
public class TaggedGreeterDecorator : ITaggedGreeter
{
    public ITaggedGreeter Inner { get; }

    public TaggedGreeterDecorator(ITaggedGreeter inner) => Inner = inner;

    public string Greet() => $"tagged({Inner.Greet()})";
}

public interface ILoggerGreeter
{
    string Greet();
}

[RegisterSingleton<ILoggerGreeter>]
public class LoggerGreeter : ILoggerGreeter
{
    public string Greet() => "logger-base";
}

[RegisterDecorator<ILoggerGreeter>]
public class LoggerGreeterDecorator : ILoggerGreeter
{
    public ILoggerGreeter Inner { get; }

    public ILogger<LoggerGreeterDecorator> Logger { get; }

    public LoggerGreeterDecorator(ILoggerGreeter inner, ILogger<LoggerGreeterDecorator> logger)
    {
        Inner = inner;
        Logger = logger;
    }

    public string Greet()
    {
        Logger.LogInformation("logger {inner}", Inner.Greet());
        return $"logger({Inner.Greet()})";
    }
}

public interface IKeyedThing
{
    string Name { get; }
}

[RegisterSingleton<IKeyedThing>(ServiceKey = "alpha")]
public class AlphaThing : IKeyedThing
{
    public string Name => "alpha";
}

[RegisterSingleton<IKeyedThing>(ServiceKey = "beta")]
public class BetaThing : IKeyedThing
{
    public string Name => "beta";
}

[RegisterDecorator<IKeyedThing>(AnyKey = true)]
public class WrappedThing : IKeyedThing
{
    public IKeyedThing Inner { get; }

    public WrappedThing(IKeyedThing inner) => Inner = inner;

    public string Name => $"wrapped({Inner.Name})";
}

public interface IKeyedFactoryThing
{
    string Name { get; }
}

[RegisterSingleton<IKeyedFactoryThing>(ServiceKey = "factory")]
public class KeyedFactoryThing : IKeyedFactoryThing
{
    public string Name => "keyed-factory";
}

[RegisterDecorator<IKeyedFactoryThing>(ServiceKey = "factory", Factory = nameof(Create))]
public class KeyedFactoryThingDecorator : IKeyedFactoryThing
{
    public IKeyedFactoryThing Inner { get; }

    public object? ServiceKey { get; }

    private KeyedFactoryThingDecorator(object? serviceKey, IKeyedFactoryThing inner)
    {
        ServiceKey = serviceKey;
        Inner = inner;
    }

    public static IKeyedFactoryThing Create(IServiceProvider serviceProvider, object? serviceKey, IKeyedFactoryThing inner)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);

        return new KeyedFactoryThingDecorator(serviceKey, inner);
    }

    public string Name => $"{ServiceKey}-factory({Inner.Name})";
}
