using Injectio.Attributes;

namespace Injectio.Acceptance.Tests.Services;

public interface IOrderedService { }

[RegisterTransient<IOrderedService>(Duplicate = DuplicateStrategy.Append, Order = 3)]
public class ThirdOrderedService : IOrderedService { }

[RegisterScoped<IOrderedService>(Duplicate = DuplicateStrategy.Append, Order = 1)]
public class FirstOrderedService : IOrderedService { }

[RegisterSingleton<IOrderedService>(Duplicate = DuplicateStrategy.Append, Order = 2)]
public class SecondOrderedService : IOrderedService { }

public interface IOrderedSkipService { }

[RegisterSingleton<IOrderedSkipService>(Order = 2)]
public class LateOrderedSkipService : IOrderedSkipService { }

[RegisterSingleton<IOrderedSkipService>(Order = 1)]
public class EarlyOrderedSkipService : IOrderedSkipService { }
