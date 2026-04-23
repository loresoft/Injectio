namespace Injectio.Generators.Models;

public record ModuleRegistration(
    string ClassName,
    string MethodName,
    bool IsStatic,
    bool HasTagCollection
);
