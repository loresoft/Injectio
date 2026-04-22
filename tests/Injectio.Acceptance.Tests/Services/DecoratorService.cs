using Injectio.Attributes;

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
