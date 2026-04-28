using Injectio.Generators.Infrastructure;

namespace Injectio.Generators.Models;

public record ServiceRegistration(
    string Lifetime,
    string ImplementationType,
    EquatableArray<string> ServiceTypes,
    string? ServiceKey,
    string? Factory,
    string Duplicate,
    string Registration,
    EquatableArray<string> Tags,
    bool IsOpenGeneric = false
);
