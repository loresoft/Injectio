using Injectio.Attributes;

namespace Injectio.Generators;

public sealed class ServiceRegistration : IEquatable<ServiceRegistration>
{
    public ServiceRegistration(
        string lifetime,
        string implementationType,
        IEnumerable<string> serviceTypes,
        string factory,
        DuplicateStrategy duplicate,
        RegistrationStrategy registration)
    {
        Lifetime = lifetime;
        ImplementationType = implementationType;
        ServiceTypes = new EquatableArray<string>(serviceTypes);
        Factory = factory;
        Duplicate = duplicate;
        Registration = registration;
    }

    public string Lifetime { get; }

    public string ImplementationType { get; }

    public EquatableArray<string> ServiceTypes { get; }

    public string Factory { get; }

    public DuplicateStrategy Duplicate { get; }

    public RegistrationStrategy Registration { get; }

    public bool Equals(ServiceRegistration other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Lifetime == other.Lifetime
               && ImplementationType == other.ImplementationType
               && ServiceTypes.Equals(other.ServiceTypes)
               && Factory == other.Factory
               && Duplicate == other.Duplicate
               && Registration == other.Registration;
    }

    public override bool Equals(object obj)
    {
        return obj is ServiceRegistration serviceRegistration
               && Equals(serviceRegistration);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(
            Lifetime,
            ImplementationType,
            ServiceTypes,
            Factory,
            Duplicate,
            Registration);
    }

    public static bool operator ==(ServiceRegistration left, ServiceRegistration right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ServiceRegistration left, ServiceRegistration right)
    {
        return !Equals(left, right);
    }
}
