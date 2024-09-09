namespace Injectio.Generators;

public record ModuleRegistration(
    string ClassName,
    string MethodName,
    bool IsStatic,
    bool HasTagCollection
);
