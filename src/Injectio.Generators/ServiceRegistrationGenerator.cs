using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

using Injectio.Attributes;
using Injectio.Generators.Extensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Injectio.Generators;

[Generator]
public class ServiceRegistrationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var pipeline = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: SyntacticPredicate,
                transform: SemanticTransform
            )
            .Where(static context => context is not null);

        // Emit the diagnostics, if needed
        var diagnostics = pipeline
            .Select(static (item, _) => item.Diagnostics)
            .Where(static item => item.Count > 0);

        context.RegisterSourceOutput(diagnostics, ReportDiagnostic);

        // select contexts with registrations
        var registrations = pipeline
            .Where(static context => context.ServiceRegistrations.Count > 0 || context.ModuleRegistrations.Count > 0)
            .Collect();

        // include config options
        var assemblyName = context.CompilationProvider
            .Select(static (c, _) => c.AssemblyName);

        var methodName = context.AnalyzerConfigOptionsProvider
            .Select(static (c, _) =>
            {
                c.GlobalOptions.TryGetValue("build_property.injectioname", out var methodName);
                return methodName;
            });

        var options = assemblyName.Combine(methodName);
        var generation = registrations.Combine(options);

        context.RegisterSourceOutput(generation, ExecuteGeneration);
    }

    private void ExecuteGeneration(
        SourceProductionContext sourceContext,
        (ImmutableArray<ServiceRegistrationContext> Registrations, (string AssemblyName, string MethodName) Options) source)
    {
        var serviceRegistrations = source.Registrations
            .SelectMany(m => m.ServiceRegistrations)
            .Where(m => m is not null)
            .ToArray();

        var moduleRegistrations = source.Registrations
            .SelectMany(m => m.ModuleRegistrations)
            .Where(m => m is not null)
            .ToArray();

        // compute extension method name
        var methodName = source.Options.MethodName;
        if (methodName.IsNullOrWhiteSpace())
            methodName = Regex.Replace(source.Options.AssemblyName, "\\W", "");

        // generate registration method
        var result = ServiceRegistrationWriter.GenerateExtensionClass(
            moduleRegistrations,
            serviceRegistrations,
            source.Options.AssemblyName,
            methodName);

        // add source file
        sourceContext.AddSource("Injectio.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static void ReportDiagnostic(SourceProductionContext context, EquatableArray<Diagnostic> diagnostics)
    {
        foreach (var diagnostic in diagnostics)
            context.ReportDiagnostic(diagnostic);
    }

    private static bool SyntacticPredicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode is ClassDeclarationSyntax { AttributeLists.Count: > 0 } classDeclaration
                   && !classDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword)
                   && !classDeclaration.Modifiers.Any(SyntaxKind.StaticKeyword)
               || syntaxNode is MemberDeclarationSyntax { AttributeLists.Count: > 0 } memberDeclaration
                   && !memberDeclaration.Modifiers.Any(SyntaxKind.AbstractKeyword);
    }

    private static ServiceRegistrationContext SemanticTransform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.Node switch
        {
            ClassDeclarationSyntax => SemanticTransformClass(context),
            MethodDeclarationSyntax => SemanticTransformMethod(context),
            _ => null
        };
    }

    private static ServiceRegistrationContext SemanticTransformMethod(GeneratorSyntaxContext context)
    {
        if (context.Node is not MethodDeclarationSyntax methodDeclaration)
            return null;

        var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
        if (methodSymbol is null)
            return null;

        // make sure attribute is for registration
        var attributes = methodSymbol.GetAttributes();
        var isKnown = attributes.Any(IsMethodAttribute);
        if (!isKnown)
            return null;

        var diagnostics = ValidateMethod(methodDeclaration, methodSymbol).ToArray();
        if (diagnostics.Any())
            return new ServiceRegistrationContext(diagnostics);

        var registration = new ModuleRegistration
        (
            className: methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
            methodName: methodSymbol.Name,
            isStatic: methodSymbol.IsStatic
        );

        return new ServiceRegistrationContext(moduleRegistrations: new[] { registration });
    }

    private static ServiceRegistrationContext SemanticTransformClass(GeneratorSyntaxContext context)
    {
        if (context.Node is not ClassDeclarationSyntax classSyntax)
            return null;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(classSyntax);
        if (classSymbol is null)
            return null;

        var attributes = classSymbol.GetAttributes();

        // support multiple register attributes on a class
        var registrations = attributes
            .Select(attribute => CreateServiceRegistration(classSymbol, attribute))
            .Where(registration => registration != null)
            .ToArray();

        if (registrations.Length == 0)
            return null;

        return new ServiceRegistrationContext(serviceRegistrations: registrations);
    }

    private static IEnumerable<Diagnostic> ValidateMethod(MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol)
    {
        var hasServiceCollection = false;
        var methodName = methodSymbol.Name;

        foreach (var parameterSymbol in methodSymbol.Parameters)
        {
            bool isServiceCollection = IsServiceCollection(parameterSymbol);

            if (isServiceCollection)
            {
                hasServiceCollection = true;
                continue;
            }

            var parameterDiagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "SD0010",
                    title: "Invalid Module Parameter",
                    messageFormat: "Invalid parameter {0} for registration method {1}.  Module registration will be skipped.",
                    category: "Usage",
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true
                ),
                methodDeclaration.GetLocation(),
                parameterSymbol.Name,
                methodName
            );

            yield return parameterDiagnostic;
        }

        if (hasServiceCollection)
            yield break;

        // no parameter for service collection
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(
                id: "SD0011",
                title: "Invalid Module Parameter",
                messageFormat: "A parameter of type IServiceCollection was not found for method {0}. Module registration will be skipped.",
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true
            ),
            methodDeclaration.GetLocation(),
            methodName
        );

        yield return diagnostic;
    }

    private static ServiceRegistration CreateServiceRegistration(INamedTypeSymbol classSymbol, AttributeData attribute)
    {
        var attributeName = attribute.AttributeClass?.ToString();

        // check for known attribute
        if (!IsKnownAttribute(attributeName, out var serviceLifetime))
            return null;

        // defaults
        var serviceTypes = new HashSet<string>();
        string implementationType = null;
        string implementationFactory = null;
        DuplicateStrategy? duplicateStrategy = null;
        RegistrationStrategy? registrationStrategy = null;

        foreach (var parameter in attribute.NamedArguments)
        {
            // match name with service registration configuration
            var name = parameter.Key;
            var value = parameter.Value.Value;

            if (string.IsNullOrEmpty(name) || value == null)
                continue;

            switch (name)
            {
                case "ServiceType":
                    serviceTypes.Add(value.ToString());
                    break;
                case "ImplementationType":
                    implementationType = value.ToString();
                    break;
                case "Factory":
                    implementationFactory = value.ToString();
                    break;
                case "Duplicate":
                    duplicateStrategy = ParseEnum<DuplicateStrategy>(value);
                    break;
                case "Registration":
                    registrationStrategy = ParseEnum<RegistrationStrategy>(value);
                    break;
            }
        }

        // set defaults
        duplicateStrategy ??= DuplicateStrategy.Skip;

        if (registrationStrategy == null
            && implementationType == null
            && serviceTypes.Count == 0)
        {
            registrationStrategy = RegistrationStrategy.SelfWithInterfaces;
        }

        if (implementationType.IsNullOrWhiteSpace())
        {
            implementationType = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        bool includeInterfaces = registrationStrategy is RegistrationStrategy.ImplementedInterfaces or RegistrationStrategy.SelfWithInterfaces;
        if (includeInterfaces)
        {
            foreach (var implementedInterface in classSymbol.AllInterfaces)
            {
                var interfaceName = implementedInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                serviceTypes.Add(interfaceName);
            }
        }

        bool includeSelf = registrationStrategy is RegistrationStrategy.Self or RegistrationStrategy.SelfWithInterfaces;
        if (includeSelf || serviceTypes.Count == 0)
            serviceTypes.Add(implementationType);

        return new ServiceRegistration(
            serviceLifetime,
            implementationType,
            serviceTypes,
            implementationFactory,
            duplicateStrategy ?? DuplicateStrategy.Skip,
            registrationStrategy ?? RegistrationStrategy.SelfWithInterfaces);
    }

    private static TEnum? ParseEnum<TEnum>(object value) where TEnum : struct
    {
        return value switch
        {
            int numberValue => Enum.IsDefined(typeof(TEnum), value) ? (TEnum)value : null,
            string stringValue => Enum.TryParse<TEnum>(stringValue, out var strategy) ? strategy : null,
            _ => null
        };
    }

    private static bool IsKnownAttribute(string attributeName, out string serviceLifetime)
    {
        serviceLifetime = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient";

        switch (attributeName)
        {
            case KnownTypes.TransientAttributeShortName:
            case KnownTypes.TransientAttributeTypeName:
            case KnownTypes.TransientAttributeFullName:
                serviceLifetime = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient";
                return true;
            case KnownTypes.SingletonAttributeShortName:
            case KnownTypes.SingletonAttributeTypeName:
            case KnownTypes.SingletonAttributeFullName:
                serviceLifetime = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton";
                return true;
            case KnownTypes.ScopedAttributeShortName:
            case KnownTypes.ScopedAttributeTypeName:
            case KnownTypes.ScopedAttributeFullName:
                serviceLifetime = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped";
                return true;
        }

        return false;
    }

    private static bool IsMethodAttribute(AttributeData attribute)
    {
        return attribute?.AttributeClass is
        {
            Name: KnownTypes.ModuleAttributeShortName or KnownTypes.ModuleAttributeTypeName,
            ContainingNamespace:
            {
                Name: "Attributes",
                ContainingNamespace:
                {
                    Name: "Injectio"
                }
            }
        };
    }

    private static bool IsServiceCollection(IParameterSymbol parameterSymbol)
    {
        return parameterSymbol?.Type is
        {
            Name: "IServiceCollection" or "ServiceCollection",
            ContainingNamespace:
            {
                Name: "DependencyInjection",
                ContainingNamespace:
                {
                    Name: "Extensions",
                    ContainingNamespace:
                    {
                        Name: "Microsoft"
                    }
                }
            }
        };
    }
}
