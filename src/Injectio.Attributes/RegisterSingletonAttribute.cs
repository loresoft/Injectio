namespace Injectio.Attributes;

/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a singleton service
/// </summary>
/// <example>Register the class as an implementation for IService
///   <code>
///   [RegisterSingleton]
///   public class SingletonService : IService { }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterSingletonAttribute : RegisterAttribute
{
}

#if NET7_0_OR_GREATER
/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a singleton service
/// </summary>
/// <typeparam name="TService">The type of the service to add.</typeparam>
/// <example>Register the class as an implementation for IService
///   <code>
///   [RegisterSingleton&lt;IService&gt;]
///   public class SingletonService : IService { }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterSingletonAttribute<TService> : RegisterSingletonAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterSingletonAttribute"/> class.
    /// </summary>
    public RegisterSingletonAttribute()
    {
        ServiceType = typeof(TService);
    }
}

/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a singleton service
/// </summary>
/// <typeparam name="TService">The type of the service to add.</typeparam>
/// <typeparam name="TImplementation">The type of the implementation to use.</typeparam>
/// <example>Register the SingletonService class as an implementation for IService
///   <code>
///   [RegisterSingleton&lt;IService, SingletonService&gt;]
///   public class SingletonService: IService { }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterSingletonAttribute<TService, TImplementation> : RegisterSingletonAttribute<TService>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterSingletonAttribute"/> class.
    /// </summary>
    public RegisterSingletonAttribute()
    {
        ServiceType = typeof(TService);
        ImplementationType = typeof(TImplementation);
    }
}
#endif
