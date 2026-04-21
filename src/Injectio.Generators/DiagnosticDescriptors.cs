using Microsoft.CodeAnalysis;

namespace Injectio.Generators;

public static class DiagnosticDescriptors
{
    private const string Category = "Usage";

    public static readonly DiagnosticDescriptor InvalidMethodSignature = new(
        id: "INJ0001",
        title: "RegisterServices method has invalid signature",
        messageFormat: "Method '{0}' marked with [RegisterServices] must have IServiceCollection as its first parameter",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor InvalidMethodSecondParameter = new(
        id: "INJ0002",
        title: "RegisterServices method has invalid second parameter",
        messageFormat: "Method '{0}' marked with [RegisterServices] has an invalid second parameter; expected a string collection (e.g., ISet<string>)",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor MethodTooManyParameters = new(
        id: "INJ0003",
        title: "RegisterServices method has too many parameters",
        messageFormat: "Method '{0}' marked with [RegisterServices] has {1} parameters; expected 1 or 2",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor FactoryMethodNotFound = new(
        id: "INJ0004",
        title: "Factory method not found",
        messageFormat: "Factory method '{0}' was not found on type '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor FactoryMethodNotStatic = new(
        id: "INJ0005",
        title: "Factory method must be static",
        messageFormat: "Factory method '{0}' on type '{1}' must be static",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor FactoryMethodInvalidSignature = new(
        id: "INJ0006",
        title: "Factory method has invalid signature",
        messageFormat: "Factory method '{0}' on type '{1}' must accept IServiceProvider as its first parameter and optionally object? as its second parameter",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor ServiceTypeMismatch = new(
        id: "INJ0007",
        title: "Implementation does not implement service type",
        messageFormat: "Type '{0}' does not implement or inherit from service type '{1}'",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor AbstractImplementationType = new(
        id: "INJ0008",
        title: "Implementation type is abstract",
        messageFormat: "Implementation type '{0}' is abstract and cannot be instantiated without a factory method",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static readonly DiagnosticDescriptor RegisterServicesMethodOnAbstractClass = new(
        id: "INJ0009",
        title: "RegisterServices on non-static method in abstract class",
        messageFormat: "Method '{0}' marked with [RegisterServices] is a non-static method on abstract class '{1}'; the class cannot be instantiated",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
}
