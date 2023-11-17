using Microsoft.CodeAnalysis;

namespace Injectio.Generators;

public sealed class ServiceRegistrationContext : IEquatable<ServiceRegistrationContext>
{
    public ServiceRegistrationContext(
         IEnumerable<Diagnostic>? diagnostics = null,
         IEnumerable<ServiceRegistration>? serviceRegistrations = null,
         IEnumerable<ModuleRegistration>? moduleRegistrations = null)
    {
        Diagnostics = new EquatableArray<Diagnostic>(diagnostics);
        ServiceRegistrations = new EquatableArray<ServiceRegistration>(serviceRegistrations);
        ModuleRegistrations = new EquatableArray<ModuleRegistration>(moduleRegistrations);
    }

    public EquatableArray<Diagnostic> Diagnostics { get; }

    public EquatableArray<ServiceRegistration> ServiceRegistrations { get; }

    public EquatableArray<ModuleRegistration> ModuleRegistrations { get; }

    public bool Equals(ServiceRegistrationContext? other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Diagnostics.Equals(other.Diagnostics)
               && ServiceRegistrations.Equals(other.ServiceRegistrations)
               && ModuleRegistrations.Equals(other.ModuleRegistrations);
    }

    public override bool Equals(object? obj)
    {
        return obj is ServiceRegistrationContext serviceContext
               && Equals(serviceContext);
    }

    public override int GetHashCode()
    {
        return HashCode.Seed
            .Combine(Diagnostics)
            .Combine(ServiceRegistrations)
            .Combine(ModuleRegistrations);
    }

    public static bool operator ==(ServiceRegistrationContext? left, ServiceRegistrationContext? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ServiceRegistrationContext? left, ServiceRegistrationContext? right)
    {
        return !Equals(left, right);
    }
}
