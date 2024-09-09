using System.Diagnostics.CodeAnalysis;

using Microsoft.CodeAnalysis;

namespace Injectio.Generators;

public record ServiceRegistrationContext(
    EquatableArray<Diagnostic>? Diagnostics = null,
    EquatableArray<ServiceRegistration>? ServiceRegistrations = null,
    EquatableArray<ModuleRegistration>? ModuleRegistrations = null
);
