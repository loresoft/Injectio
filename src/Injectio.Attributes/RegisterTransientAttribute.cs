namespace Injectio.Attributes;

/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a transient service
/// </summary>
/// <example>Register the class as an implementation for IService
///   <code>
///   [RegisterTransient]
///   public class TransientService : IService { }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterTransientAttribute : RegisterAttribute
{
}

#if NET7_0_OR_GREATER
/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a transient service
/// </summary>
/// <typeparam name="TService">The type of the service to add.</typeparam>
/// <example>Register the class as an implementation for IService
///   <code>
///   [RegisterTransient&lt;IService&gt;]
///   public class TransientService : IService { }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterTransientAttribute<TService> : RegisterTransientAttribute
    where TService : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterTransientAttribute"/> class.
    /// </summary>
    public RegisterTransientAttribute()
    {
        ServiceType = typeof(TService);
    }
}

/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a transient service
/// </summary>
/// <typeparam name="TService">The type of the service to add.</typeparam>
/// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
/// <example>Register the TransientService class as an implementation for IService
///   <code>
///   [RegisterTransient&lt;IService, TransientService&gt;]
///   public class TransientService: IService { }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterTransientAttribute<TService, TImplementation> : RegisterTransientAttribute<TService>
    where TService : class
    where TImplementation : class, TService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterTransientAttribute"/> class.
    /// </summary>
    public RegisterTransientAttribute()
    {
        ServiceType = typeof(TService);
        ImplementationType = typeof(TImplementation);
    }
}
#endif
