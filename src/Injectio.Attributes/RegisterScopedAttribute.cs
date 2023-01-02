namespace Injectio.Attributes;

/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a scoped service
/// </summary>
/// <example>Register the class as an implementation for IService
///   <code>
///   [RegisterScoped]
///   public class ScopedService : IService { }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterScopedAttribute : RegisterAttribute
{
}

#if NET7_0_OR_GREATER
/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a scoped service
/// </summary>
/// <typeparam name="TService">The type of the service to add.</typeparam>
/// <example>Register the class as an implementation for IService
///   <code>
///   [RegisterScoped&lt;IService&gt;]
///   public class ScopedService : IService { }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterScopedAttribute<TService> : RegisterScopedAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterScopedAttribute"/> class.
    /// </summary>
    public RegisterScopedAttribute()
    {
        ServiceType = typeof(TService);
    }
}

/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a scoped service
/// </summary>
/// <typeparam name="TService">The type of the service to add.</typeparam>
/// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
/// <example>Register the ScopedService class as an implementation for IService
///   <code>
///   [RegisterScoped&lt;IService, ScopedService&gt;]
///   public class ScopedService: IService { }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterScopedAttribute<TService, TImplementation> : RegisterScopedAttribute<TService>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterScopedAttribute"/> class.
    /// </summary>
    public RegisterScopedAttribute()
    {
        ServiceType = typeof(TService);
        ImplementationType = typeof(TImplementation);
    }
}
#endif
