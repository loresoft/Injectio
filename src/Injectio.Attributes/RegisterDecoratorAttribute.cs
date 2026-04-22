namespace Injectio.Attributes;

/// <summary>
/// Attribute to indicate the target class should be registered as a decorator for an existing service.
/// The decorator wraps the previously registered service implementation and inherits its <see cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.Lifetime"/>.
/// </summary>
/// <example>Decorate <c>IService</c> with a logging wrapper
///   <code>
///   [RegisterDecorator(ServiceType = typeof(IService))]
///   public class LoggingDecorator : IService
///   {
///       public LoggingDecorator(IService inner) { }
///   }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterDecoratorAttribute : Attribute
{
    /// <summary>
    /// The <see cref="Type"/> of the service to decorate.
    /// </summary>
    public Type? ServiceType { get; set; }

    /// <summary>
    /// The <see cref="Type"/> that implements the decorator. If not set, the class the attribute is on will be used.
    /// </summary>
    public Type? ImplementationType { get; set; }

    /// <summary>
    /// Gets or sets the key of the keyed service to decorate.
    /// Leave unset (and <see cref="AnyKey"/> false) to decorate the non-keyed registration.
    /// </summary>
    public object? ServiceKey { get; set; }

    /// <summary>
    /// When <c>true</c>, the decorator is applied to every keyed registration of <see cref="ServiceType"/>,
    /// regardless of its key. Equivalent to <c>KeyedService.AnyKey</c>.
    /// </summary>
    public bool AnyKey { get; set; }

    /// <summary>
    /// Name of a static factory method to construct the decorator.
    /// </summary>
    /// <remarks>
    /// The factory signature must be <c>(IServiceProvider, TService) -&gt; TService</c> for non-keyed services
    /// or <c>(IServiceProvider, object?, TService) -&gt; TService</c> for keyed services.
    /// </remarks>
    public string? Factory { get; set; }

    /// <summary>
    /// Gets or sets the order in which the decorator is applied. Lower values are applied first (innermost).
    /// </summary>
    public int Order { get; set; }

    /// <summary>
    /// Gets or sets the comma delimited list of registration tags.
    /// </summary>
    public string? Tags { get; set; }
}

#if NET7_0_OR_GREATER
/// <summary>
/// Attribute to indicate the target class should be registered as a decorator for <typeparamref name="TService"/>.
/// </summary>
/// <typeparam name="TService">The type of the service to decorate.</typeparam>
/// <example>
///   <code>
///   [RegisterDecorator&lt;IService&gt;]
///   public class LoggingDecorator : IService
///   {
///       public LoggingDecorator(IService inner) { }
///   }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterDecoratorAttribute<TService> : RegisterDecoratorAttribute
    where TService : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterDecoratorAttribute{TService}"/> class.
    /// </summary>
    public RegisterDecoratorAttribute()
    {
        ServiceType = typeof(TService);
    }
}

/// <summary>
/// Attribute to indicate the target class should be registered as a decorator for <typeparamref name="TService"/>
/// using <typeparamref name="TImplementation"/> as the decorator implementation.
/// </summary>
/// <typeparam name="TService">The type of the service to decorate.</typeparam>
/// <typeparam name="TImplementation">The type of the decorator implementation.</typeparam>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterDecoratorAttribute<TService, TImplementation> : RegisterDecoratorAttribute<TService>
    where TService : class
    where TImplementation : class, TService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterDecoratorAttribute{TService, TImplementation}"/> class.
    /// </summary>
    public RegisterDecoratorAttribute()
    {
        ServiceType = typeof(TService);
        ImplementationType = typeof(TImplementation);
    }
}
#endif
