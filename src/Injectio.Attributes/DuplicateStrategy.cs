namespace Injectio.Attributes;

/// <summary>
/// Service registration duplicate strategies
/// </summary>
public enum DuplicateStrategy
{
    /// <summary>
    /// Skips registrations for services that already exists.
    /// </summary>
    Skip = 0,
    /// <summary>
    /// Replaces existing service registrations.
    /// </summary>
    Replace = 1,
    /// <summary>
    /// Appends a new registration for existing services.
    /// </summary>
    Append = 2
}
