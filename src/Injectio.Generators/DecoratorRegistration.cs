namespace Injectio.Generators;

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
