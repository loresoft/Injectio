namespace Injectio.Generators;

public record ServiceRegistrationContext(
    EquatableArray<ServiceRegistration>? ServiceRegistrations = null,
    EquatableArray<ModuleRegistration>? ModuleRegistrations = null,
    EquatableArray<StaticObjectRegistration>? StaticObjectRegistrations = null
);
