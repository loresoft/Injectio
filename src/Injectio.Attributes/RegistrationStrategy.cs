namespace Injectio.Attributes;

/// <summary>
/// Register service type strategies
/// </summary>
public enum RegistrationStrategy
{
    /// <summary>
    /// Registers each matching concrete type as itself
    /// </summary>
    Self = 0,
    /// <summary>
    /// Registers each matching concrete type as all of its implemented interfaces.
    /// </summary>
    ImplementedInterfaces = 1,
    /// <summary>
    /// Registers each matching concrete type as all of its implemented interfaces and itself
    /// </summary>
    SelfWithInterfaces = 2,
    /// <summary>
    /// Registers each matching concrete type as all of its implemented interfaces and itself.
    /// For the interfaces a proxy-factory resolves the service from its type-name, so only one instance is created per lifetime
    /// </summary>
    /// <remarks>For open-generic registrations, this behaves like <see cref="SelfWithInterfaces"/></remarks>
    SelfWithProxyFactory = 3
}
