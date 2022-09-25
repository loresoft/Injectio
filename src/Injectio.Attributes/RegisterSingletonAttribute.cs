namespace Injectio.Attributes;

/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a singleton service
/// </summary>
/// <example>Register the class as a singleton implementation of IService
///   <code>
///   [RegisterSingleton(Registration = RegistrationStrategy.SelfWithInterfaces)]
///   public class SingletonService : IService { }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class RegisterSingletonAttribute : RegisterAttribute
{
}
