namespace Injectio.Attributes;

/// <summary>
/// Registgered service type strategies
/// </summary>
public enum RegistrationStrategy
{
    /// <summary>
    /// Registers each matching concrete type as itself
    /// </summary>
    Self,
    /// <summary>
    /// Registers each matching concrete type as all of its implemented interfaces.
    /// </summary>
    ImplementedInterfaces,
    /// <summary>
    /// Registers each matching concrete type as all of its implemented interfaces and itself
    /// </summary>
    SelfWithInterfaces
}