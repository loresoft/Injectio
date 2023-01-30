namespace Injectio.Attributes;

/// <summary>
/// Attribute to indicate the target class should be register for dependency injection
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public abstract class RegisterAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterAttribute"/> class.
    /// </summary>
    protected RegisterAttribute()
    {
        Duplicate = DuplicateStrategy.Skip;
        Registration = RegistrationStrategy.SelfWithInterfaces;
    }


    /// <summary>
    /// The <see cref="Type"/> that implements the service.  If not set, the class the interface is on will be used.
    /// </summary>
    /// <seealso cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceType"/>
    public Type? ImplementationType { get; set; }

    /// <summary>
    /// The <see cref="Type"/> of the service
    /// </summary>
    /// <seealso cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceType"/>
    public Type? ServiceType { get; set; }


    /// <summary>
    /// Name of a factory method to create new instances of the service implementation
    /// </summary>
    /// <remarks>
    /// The method signature must be Func{IServiceProvider, object} and must be static
    /// </remarks>
    /// <seealso cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ImplementationFactory"/>
    public string? Factory { get; set; }

    /// <summary>
    /// Gets or sets the duplicate.
    /// </summary>
    /// <value>
    /// The duplicate.
    /// </value>
    public DuplicateStrategy Duplicate { get; set; }

    /// <summary>
    /// Gets or sets the registration.
    /// </summary>
    /// <value>
    /// The registration.
    /// </value>
    public RegistrationStrategy Registration { get; set; }

    /// <summary>
    /// Gets or sets the comma delimited list of service registration tags.
    /// </summary>
    /// <value>
    /// The comma delimited list of service registration tags.
    /// </value>
    public string? Tags { get; set; }
}
