using Injectio.Generators.Infrastructure;

namespace Injectio.Generators.Models;

public record DecoratorRegistration(
    string DecoratorType,
    string ServiceType,
    string? ServiceKey,
    bool IsAnyKey,
    string? Factory,
    int Order,
    EquatableArray<string> Tags,
    bool IsOpenGeneric = false
);
