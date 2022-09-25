namespace Injectio.Attributes;

/// <summary>
/// Attribute to indicate the target class should be register for dependency injection as a scoped service
/// </summary>
/// <example>Register the class as an implementation of IService2 with appending duplicates
///   <code>
///   [RegisterScoped(Duplicate = DuplicateStrategy.Append)]
///   public class ScopedWithInterfacesService2 : IService2 { }
///   </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
public class RegisterScopedAttribute : RegisterAttribute
{
}
