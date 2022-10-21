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
    SelfWithInterfaces = 2
}
