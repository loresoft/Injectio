namespace Injectio.Attributes;

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
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
[System.Diagnostics.Conditional("REGISTER_SERVICE_USAGES")]
public class RegisterServicesAttribute : Attribute
{

}
