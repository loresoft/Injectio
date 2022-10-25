namespace Injectio.Attributes;

/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a Transient service
/// </summary>
/// <example>Register the class as a transient implementation of IFactoryService using a factory
///   <code>
///   [RegisterTransient(ServiceType = typeof(IFactoryService), Factory = nameof(ServiceFactory))]
///   public class FactoryService : IFactoryService
///   {
///       private readonly IService _service;
///       public FactoryService1(IService service)
///       {
///          _service = service;
///       }
///       public static IFactoryService ServiceFactory(IServiceProvider serviceProvider)
///       {
///           return new FactoryService(serviceProvider.GetRequiredService(typeof(IService)));
///       }
///   }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterTransientAttribute : RegisterAttribute
{
}
