namespace Injectio.Attributes;

/// <summary>
/// Service registration duplicate strategies
/// </summary>
public enum DuplicateStrategy
{
    /// <summary>
    /// Skips registrations for services that already exists.
    /// </summary>
    Skip,
    /// <summary>
    /// Replaces existing service registrations.
    /// </summary>
    Replace,
    /// <summary>
    /// Appends a new registration for existing services.
    /// </summary>
    Append
}
