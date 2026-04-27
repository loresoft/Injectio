namespace Injectio.Attributes;

/// <summary>
/// Service registration duplicate strategies
/// </summary>
[global::Microsoft.CodeAnalysis.Embedded]
internal enum DuplicateStrategy
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

/// <summary>
/// Register service type strategies
/// </summary>
[global::Microsoft.CodeAnalysis.Embedded]
internal enum RegistrationStrategy
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
    SelfWithInterfaces = 2,
    /// <summary>
    /// Registers each matching concrete type as all of its implemented interfaces and itself.
    /// For the interfaces a proxy-factory resolves the service from its type-name, so only one instance is created per lifetime
    /// </summary>
    /// <remarks>For open-generic registrations, this behaves like <see cref="SelfWithInterfaces"/></remarks>
    SelfWithProxyFactory = 3
}

/// <summary>
/// Attribute to indicate the target class should be register for dependency injection
/// </summary>
[global::Microsoft.CodeAnalysis.Embedded]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
internal abstract class RegisterAttribute : Attribute
{
    /// <summary>
    /// The <see cref="Type"/> of the service
    /// </summary>
    /// <seealso cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceType"/>
    public Type? ServiceType { get; set; }

    /// <summary>
    /// The <see cref="Type"/> that implements the service.  If not set, the class the interface is on will be used.
    /// </summary>
    /// <seealso cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceType"/>
    public Type? ImplementationType { get; set; }

    /// <summary>
    /// Gets or sets the key of the service.
    /// </summary>
    /// <value>The service key.</value>
    /// <seealso cref="P:Microsoft.Extensions.DependencyInjection.ServiceDescriptor.ServiceKey "/>
    public object? ServiceKey { get; set; }

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

/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a scoped service
/// </summary>
/// <example>Register the class as an implementation for IService
///   <code>
///   [RegisterScoped]
///   public class ScopedService : IService { }
///   </code>
/// </example>
[global::Microsoft.CodeAnalysis.Embedded]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
internal class RegisterScopedAttribute : RegisterAttribute;

/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a singleton service
/// </summary>
/// <example>Register the class as an implementation for IService
///   <code>
///   [RegisterSingleton]
///   public class SingletonService : IService { }
///   </code>
/// </example>
[global::Microsoft.CodeAnalysis.Embedded]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
internal class RegisterSingletonAttribute : RegisterAttribute;

/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a transient service
/// </summary>
/// <example>Register the class as an implementation for IService
///   <code>
///   [RegisterTransient]
///   public class TransientService : IService { }
///   </code>
/// </example>
[global::Microsoft.CodeAnalysis.Embedded]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
internal class RegisterTransientAttribute : RegisterAttribute;

/// <summary>Attribute to indicate the method should be called to register services</summary>
/// <example>use the RegisterServices attribute
///   <code>
///   public class RegistrationModule
///   {
///       [RegisterServices]
///       public static void Register(IServiceCollection services)
///       {
///           services.TryAddTransient&lt;IModuleService, ModuleService&gt;();
///       }
///   }
///   </code>
/// </example>
[global::Microsoft.CodeAnalysis.Embedded]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
[global::System.AttributeUsage(global::System.AttributeTargets.Method)]
internal class RegisterServicesAttribute : Attribute;

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
[global::Microsoft.CodeAnalysis.Embedded]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal class RegisterDecoratorAttribute : Attribute
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
/// Attribute to indicate the target class should be register for dependency injection as a scoped service
/// </summary>
/// <typeparam name="TService">The type of the service to add.</typeparam>
/// <example>Register the class as an implementation for IService
///   <code>
///   [RegisterScoped&lt;IService&gt;]
///   public class ScopedService : IService { }
///   </code>
/// </example>
[global::Microsoft.CodeAnalysis.Embedded]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
internal class RegisterScopedAttribute<TService> : RegisterScopedAttribute
    where TService : class;

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
[global::Microsoft.CodeAnalysis.Embedded]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
internal class RegisterScopedAttribute<TService, TImplementation> : RegisterScopedAttribute<TService>
    where TService : class
    where TImplementation : class, TService;

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
[global::Microsoft.CodeAnalysis.Embedded]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
internal class RegisterSingletonAttribute<TService> : RegisterSingletonAttribute
    where TService : class;

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
[global::Microsoft.CodeAnalysis.Embedded]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
internal class RegisterSingletonAttribute<TService, TImplementation> : RegisterSingletonAttribute<TService>
    where TService : class
    where TImplementation : class, TService;

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
[global::Microsoft.CodeAnalysis.Embedded]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
[global::System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
internal class RegisterTransientAttribute<TService> : RegisterTransientAttribute
    where TService : class;

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
[global::Microsoft.CodeAnalysis.Embedded]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true)]
internal class RegisterTransientAttribute<TService, TImplementation> : RegisterTransientAttribute<TService>
    where TService : class
    where TImplementation : class, TService;

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
[global::Microsoft.CodeAnalysis.Embedded]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal class RegisterDecoratorAttribute<TService> : RegisterDecoratorAttribute
    where TService : class;

/// <summary>
/// Attribute to indicate the target class should be registered as a decorator for <typeparamref name="TService"/>
/// using <typeparamref name="TImplementation"/> as the decorator implementation.
/// </summary>
/// <typeparam name="TService">The type of the service to decorate.</typeparam>
/// <typeparam name="TImplementation">The type of the decorator implementation.</typeparam>
[global::Microsoft.CodeAnalysis.Embedded]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
[global::System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
[global::System.AttributeUsage(global::System.AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
internal class RegisterDecoratorAttribute<TService, TImplementation> : RegisterDecoratorAttribute<TService>
    where TService : class
    where TImplementation : class, TService;
#endif
