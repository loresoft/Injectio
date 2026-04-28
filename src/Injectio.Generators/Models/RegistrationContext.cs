using Injectio.Generators.Infrastructure;

namespace Injectio.Generators.Models;

public record RegistrationContext(
    EquatableArray<ServiceRegistration> ServiceRegistrations,
    EquatableArray<ModuleRegistration> ModuleRegistrations,
    EquatableArray<DecoratorRegistration> DecoratorRegistrations,
    string? AssemblyName,
    MethodOptions? MethodOptions
);
