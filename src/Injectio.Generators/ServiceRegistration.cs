using System.Diagnostics.CodeAnalysis;

namespace Injectio.Generators;

[ExcludeFromCodeCoverage]
public sealed class ServiceRegistration : IEquatable<ServiceRegistration>
{
    public ServiceRegistration(
        string lifetime,
        string implementationType,
        IEnumerable<string> serviceTypes,
        string? serviceKey,
        string? factory,
        string duplicate,
        string registration,
        IEnumerable<string> tags)
    {
        Lifetime = lifetime;
        ImplementationType = implementationType;
        ServiceTypes = new EquatableArray<string>(serviceTypes);
        ServiceKey = serviceKey;
        Factory = factory;
        Duplicate = duplicate;
        Registration = registration;
        Tags = new EquatableArray<string>(tags);
    }

    public string Lifetime { get; }

    public string ImplementationType { get; }

    public EquatableArray<string> ServiceTypes { get; }

    public string? ServiceKey { get; }

    public string? Factory { get; }

    public string Duplicate { get; }

    public string Registration { get; }

    public EquatableArray<string> Tags { get; }

    public bool Equals(ServiceRegistration other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return Lifetime == other.Lifetime
               && ImplementationType == other.ImplementationType
               && ServiceTypes.Equals(other.ServiceTypes)
               && ServiceKey == other.ServiceKey
               && Factory == other.Factory
               && Duplicate == other.Duplicate
               && Registration == other.Registration
               && Tags.Equals(other.Tags);
    }

    public override bool Equals(object? obj)
    {
        return obj is ServiceRegistration serviceRegistration
               && Equals(serviceRegistration);
    }

    public override int GetHashCode()
    {
        return HashCode.Seed
            .Combine(Lifetime)
            .Combine(ImplementationType)
            .CombineAll(ServiceTypes)
            .Combine(ServiceKey)
            .Combine(Factory)
            .Combine(Duplicate)
            .Combine(Registration)
            .CombineAll(Tags);
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
