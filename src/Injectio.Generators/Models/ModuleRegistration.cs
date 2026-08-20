namespace Injectio.Generators.Models;

public enum ModuleParameterType
{
    ServiceCollection,
    HostApplicationBuilder
}

public record ModuleRegistration(
    string ClassName,
    string MethodName,
    bool IsStatic,
    bool HasTagCollection,
    ModuleParameterType ParameterType = ModuleParameterType.ServiceCollection
);
