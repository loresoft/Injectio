namespace Injectio.Attributes;

/// <summary>
/// Attribute to indicate the target class should be registered for dependency injection as a transient service.
/// </summary>
/// <example>
/// Register the class as an implementation for IService
/// <code>
/// [RegisterTransient(typeof(IService))]
/// public class TransientService : IService { }
/// </code>
/// </example>
[AttributeUsage(attributeTargets.Class, AllowMultiple = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterTransientAttribute : RegisterAttribute
{
    /// <summary>
    /// The type of the service to add.
    /// </summary>
    public Type ServiceType { get; set; }
    /// <summary>
    /// The type of the implementation to use.
    /// </summary>
    public Type ImplementationType { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterTransientAttribute"/> class.
    /// </summary>
    public RegisterTransientAttribute() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterTransientAttribute"/> class.
    /// </summary>
    /// <param name="serviceType">The type of the service to add.</param>
    public RegisterTransientAttribute(Type serviceType)
    {
        ServiceType = serviceType;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterTransientAttribute"/> class.
    /// </summary>
    /// <param name="serviceType">The type of the service to add.</param>
    /// <param name="implementationType">The type of the implementation to use.</param>
    public RegisterTransientAttribute(Type serviceType, Type implementationType)
    {
        ServiceType = serviceType;
        ImplementationType = implementationType;
    }
}
