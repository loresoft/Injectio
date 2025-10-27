namespace Injectio.Attributes;

/// <summary>
/// Attribute to indicate the target field should be register for dependency injection as a singleton service
/// </summary>
/// <example>Register the field as an implementation for IService
///   <code>
///   public class MyServiceExporter {
///     [RegisterStaticObject]
///     public static IService myField = new MyService();
///   }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterStaticObjectAttribute : RegisterAttribute
{

}

#if NET7_0_OR_GREATER
/// <summary>
/// Attribute to indicate the target field should be register for dependency injection as a singleton service
/// </summary>
/// <example>Register the field as an implementation for IService
///   <code>
///   public class MyServiceExporter {
///     [RegisterStaticObject&lt;IService&gt;]
///     public static MyService myField = new MyService();
///   }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterStaticObjectAttribute<TService> : RegisterStaticObjectAttribute
    where TService : class
{
    /// <summary>
    /// Initializes a new instance of the <see cref="RegisterStaticObjectAttribute"/> class.
    /// </summary>
    public RegisterStaticObjectAttribute()
    {
        ServiceType = typeof(TService);
    }
}
#endif
