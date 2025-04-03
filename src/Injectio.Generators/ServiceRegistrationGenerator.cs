using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

using Injectio.Generators.Extensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Injectio.Generators;

[Generator]
public class ServiceRegistrationGenerator : IIncrementalGenerator
{
    private static readonly SymbolDisplayFormat _fullyQualifiedNullableFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
        );

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var pipeline = context.SyntaxProvider.CreateSyntaxProvider(
                predicate: SyntacticPredicate,
                transform: SemanticTransform
            )
            .Where(static context => context is not null);

        // Emit the diagnostics, if needed
        var diagnostics = pipeline
            .Select(static (item, _) => item!.Diagnostics)
            .Where(static item => item?.Count > 0);

        context.RegisterSourceOutput(diagnostics, ReportDiagnostic);

        // select contexts with registrations
        var registrations = pipeline
            .Where(static context => context?.ServiceRegistrations?.Count > 0 || context?.ModuleRegistrations?.Count > 0)
            .Collect();

        // include config options
        var assemblyName = context.CompilationProvider
            .Select(static (c, _) => c.AssemblyName);

        var methodName = context.AnalyzerConfigOptionsProvider
            .Select(static (c, _) =>
            {
                c.GlobalOptions.TryGetValue("build_property.InjectioName", out var methodName);
                c.GlobalOptions.TryGetValue("build_property.InjectioInternal", out var methodInternal);
                return new MethodOptions(methodName, methodInternal);
            });

        var options = assemblyName.Combine(methodName);
        var generation = registrations.Combine(options);

        context.RegisterSourceOutput(generation, ExecuteGeneration);
    }

    private void ExecuteGeneration(
        SourceProductionContext sourceContext,
        (ImmutableArray<ServiceRegistrationContext?> Registrations, (string? AssemblyName, MethodOptions? MethodOptions) Options) source)
    {
        var serviceRegistrations = source.Registrations
            .SelectMany(m => m?.ServiceRegistrations ?? Array.Empty<ServiceRegistration>())
            .Where(m => m is not null)
            .ToArray();

        var moduleRegistrations = source.Registrations
            .SelectMany(m => m?.ModuleRegistrations ?? Array.Empty<ModuleRegistration>())
            .Where(m => m is not null)
            .ToArray();

        // compute extension method name
        var methodName = source.Options.MethodOptions?.Name;
        if (methodName.IsNullOrWhiteSpace())
            methodName = Regex.Replace(source.Options.AssemblyName, "\\W", "");

        var methodInternal = source.Options.MethodOptions?.Internal;

        // generate registration method
        var result = ServiceRegistrationWriter.GenerateExtensionClass(
            moduleRegistrations,
            serviceRegistrations,
            source.Options.AssemblyName,
            methodName,
            methodInternal);

        // add source file
        sourceContext.AddSource("Injectio.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static void ReportDiagnostic(SourceProductionContext context, EquatableArray<Diagnostic>? diagnostics)
    {
        if (diagnostics == null)
            return;

        foreach (var diagnostic in diagnostics)
            context.ReportDiagnostic(diagnostic);
    }

    private static bool SyntacticPredicate(SyntaxNode syntaxNode, CancellationToken cancellationToken)
    {
        return syntaxNode switch
        {
            ClassDeclarationSyntax { AttributeLists.Count: > 0 } declaration =>
                !declaration.Modifiers.Any(SyntaxKind.AbstractKeyword)
                && !declaration.Modifiers.Any(SyntaxKind.StaticKeyword),

            RecordDeclarationSyntax { AttributeLists.Count: > 0 } declaration =>
                !declaration.Modifiers.Any(SyntaxKind.AbstractKeyword)
                && !declaration.Modifiers.Any(SyntaxKind.StaticKeyword),

            MemberDeclarationSyntax { AttributeLists.Count: > 0 } declaration =>
                !declaration.Modifiers.Any(SyntaxKind.AbstractKeyword),

            _ => false,
        };
    }

    private static ServiceRegistrationContext? SemanticTransform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.Node switch
        {
            ClassDeclarationSyntax => SemanticTransformClass(context),
            RecordDeclarationSyntax => SemanticTransformClass(context),
            MethodDeclarationSyntax => SemanticTransformMethod(context),
            _ => null
        };
    }

    private static ServiceRegistrationContext? SemanticTransformMethod(GeneratorSyntaxContext context)
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

        var (diagnostics, hasServiceCollection, hasTagCollection) = ValidateMethod(methodDeclaration, methodSymbol);
        if (diagnostics.Any())
            return new ServiceRegistrationContext(diagnostics);

        var registration = new ModuleRegistration
        (
            ClassName: methodSymbol.ContainingType.ToDisplayString(_fullyQualifiedNullableFormat),
            MethodName: methodSymbol.Name,
            IsStatic: methodSymbol.IsStatic,
            HasTagCollection: hasTagCollection
        );

        return new ServiceRegistrationContext(ModuleRegistrations: new[] { registration });
    }

    private static ServiceRegistrationContext? SemanticTransformClass(GeneratorSyntaxContext context)
    {
        if (context.Node is not (TypeDeclarationSyntax declaration and (ClassDeclarationSyntax or RecordDeclarationSyntax)))
            return null;

        var classSymbol = context.SemanticModel.GetDeclaredSymbol(declaration);
        if (classSymbol is null)
            return null;

        var attributes = classSymbol.GetAttributes();

        // support multiple register attributes on a class
        var registrations = new List<ServiceRegistration>();

        foreach (var attribute in attributes)
        {
            var registration = CreateServiceRegistration(classSymbol, attribute);
            if (registration is not null)
                registrations.Add(registration);
        }

        if (registrations.Count == 0)
            return null;

        return new ServiceRegistrationContext(ServiceRegistrations: registrations.ToArray());
    }

    private static (EquatableArray<Diagnostic> diagnostics, bool hasServiceCollection, bool hasTagCollection) ValidateMethod(MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol)
    {
        var diagnostics = new List<Diagnostic>();
        var hasServiceCollection = false;
        var hasTagCollection = false;

        var methodName = methodSymbol.Name;

        // validate first parameter should be service collection
        if (methodSymbol.Parameters.Length is 1 or 2)
        {
            var parameterSymbol = methodSymbol.Parameters[0];
            hasServiceCollection = IsServiceCollection(parameterSymbol);
            if (!hasServiceCollection)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.InvalidServiceCollectionParameter,
                    methodDeclaration.GetLocation(),
                    parameterSymbol.Name,
                    methodName
                );
                diagnostics.Add(diagnostic);
            }
        }

        // validate second parameter should be string collection
        if (methodSymbol.Parameters.Length is 2)
        {
            var parameterSymbol = methodSymbol.Parameters[1];
            hasTagCollection = IsStringCollection(parameterSymbol);
            if (!hasTagCollection)
            {
                var diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.InvalidModuleParameter,
                    methodDeclaration.GetLocation(),
                    parameterSymbol.Name,
                    methodName
                );
                diagnostics.Add(diagnostic);
            }
        }

        if (methodSymbol.Parameters.Length is 1 or 2)
            return (diagnostics.ToArray(), hasServiceCollection, hasTagCollection);

        // invalid parameter count
        var parameterDiagnostic = Diagnostic.Create(
            DiagnosticDescriptors.InvalidServiceCollectionParameter,
            methodDeclaration.GetLocation(),
            methodName
        );
        diagnostics.Add(parameterDiagnostic);

        return (diagnostics.ToArray(), hasServiceCollection, hasTagCollection);
    }

    private static ServiceRegistration? CreateServiceRegistration(INamedTypeSymbol classSymbol, AttributeData attribute)
    {
        // check for known attribute
        if (!IsKnownAttribute(attribute, out var serviceLifetime))
            return null;

        // defaults
        var serviceTypes = new HashSet<string>();
        string implementationType = null!;
        string? implementationFactory = null;
        string? duplicateStrategy = null;
        string? registrationStrategy = null;
        var tags = new HashSet<string>();
        string? serviceKey = null;

        var attributeClass = attribute.AttributeClass;
        if (attributeClass is { IsGenericType: true } && attributeClass.TypeArguments.Length == attributeClass.TypeParameters.Length)
        {
            // if generic attribute, get service and implementation from generic type parameters
            for (var index = 0; index < attributeClass.TypeParameters.Length; index++)
            {
                var typeParameter = attributeClass.TypeParameters[index];
                var typeArgument = attributeClass.TypeArguments[index];

                if (typeParameter.Name == "TService" || index == 0)
                {
                    var service = typeArgument.ToDisplayString(_fullyQualifiedNullableFormat);
                    serviceTypes.Add(service);
                }
                else if (typeParameter.Name == "TImplementation" || index == 1)
                {
                    implementationType = typeArgument.ToDisplayString(_fullyQualifiedNullableFormat);
                }
            }
        }

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
                case "ServiceKey":
                    serviceKey = parameter.Value.ToCSharpString();
                    break;
                case "ImplementationType":
                    implementationType = value.ToString();
                    break;
                case "Factory":
                    implementationFactory = value.ToString();
                    break;
                case "Duplicate":
                    duplicateStrategy = ResolveDuplicateStrategy(value);
                    break;
                case "Registration":
                    registrationStrategy = ResolveRegistrationStrategy(value);
                    break;
                case "Tags":
                    var tagsItems = value
                        .ToString()
                        .Split(',', ';')
                        .Where(v => v.HasValue());

                    foreach (var tagItem in tagsItems)
                        tags.Add(tagItem);

                    break;
            }
        }

        // default to ignore duplicate service registrations
        duplicateStrategy ??= KnownTypes.DuplicateStrategySkipShortName;

        // if implementation and service types not set, default to self with interfaces
        if (registrationStrategy == null
            && implementationType == null
            && serviceTypes.Count == 0)
        {
            registrationStrategy = KnownTypes.RegistrationStrategySelfWithInterfacesShortName;
        }

        // no implementation type set, use class attribute is on
        if (implementationType.IsNullOrWhiteSpace())
        {
            implementationType = classSymbol.ToDisplayString(_fullyQualifiedNullableFormat);
        }

        // add implemented interfaces
        bool includeInterfaces = registrationStrategy is KnownTypes.RegistrationStrategyImplementedInterfacesShortName or KnownTypes.RegistrationStrategySelfWithInterfacesShortName;
        if (includeInterfaces)
        {
            foreach (var implementedInterface in classSymbol.AllInterfaces)
            {
                // This interface is typically not injected into services and, more specifically, record types auto-implement it.
                if(implementedInterface.ConstructedFrom.ToString() == "System.IEquatable<T>") continue;
                var interfaceName = implementedInterface.ToDisplayString(_fullyQualifiedNullableFormat);
                serviceTypes.Add(interfaceName);
            }
        }

        // add class attribute is on; default service type if not set
        bool includeSelf = registrationStrategy is KnownTypes.RegistrationStrategySelfShortName or KnownTypes.RegistrationStrategySelfWithInterfacesShortName;
        if (includeSelf || serviceTypes.Count == 0)
            serviceTypes.Add(implementationType!);

        return new ServiceRegistration(
            serviceLifetime,
            implementationType!,
            serviceTypes.ToArray(),
            serviceKey,
            implementationFactory,
            duplicateStrategy ?? KnownTypes.DuplicateStrategySkipShortName,
            registrationStrategy ?? KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
            tags.ToArray());
    }

    private static bool IsKnownAttribute(AttributeData attribute, out string serviceLifetime)
    {
        if (IsSingletonAttribute(attribute))
        {
            serviceLifetime = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton";
            return true;
        }

        if (IsScopedAttribute(attribute))
        {
            serviceLifetime = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped";
            return true;
        }

        if (IsTransientAttribute(attribute))
        {
            serviceLifetime = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient";
            return true;
        }

        serviceLifetime = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient";
        return false;
    }

    private static bool IsTransientAttribute(AttributeData attribute)
    {
        return attribute?.AttributeClass is
        {
            Name: KnownTypes.TransientAttributeShortName or KnownTypes.TransientAttributeTypeName,
            ContainingNamespace:
            {
                Name: "Attributes",
                ContainingNamespace.Name: "Injectio"
            }
        };
    }

    private static bool IsSingletonAttribute(AttributeData attribute)
    {
        return attribute?.AttributeClass is
        {
            Name: KnownTypes.SingletonAttributeShortName or KnownTypes.SingletonAttributeTypeName,
            ContainingNamespace:
            {
                Name: "Attributes",
                ContainingNamespace.Name: "Injectio"
            }
        };
    }

    private static bool IsScopedAttribute(AttributeData attribute)
    {
        return attribute?.AttributeClass is
        {
            Name: KnownTypes.ScopedAttributeShortName or KnownTypes.ScopedAttributeTypeName,
            ContainingNamespace:
            {
                Name: "Attributes",
                ContainingNamespace.Name: "Injectio"
            }
        };
    }

    private static bool IsMethodAttribute(AttributeData attribute)
    {
        return attribute?.AttributeClass is
        {
            Name: KnownTypes.ModuleAttributeShortName or KnownTypes.ModuleAttributeTypeName,
            ContainingNamespace:
            {
                Name: "Attributes",
                ContainingNamespace.Name: "Injectio"
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
                    ContainingNamespace.Name: "Microsoft"
                }
            }
        };
    }

    private static bool IsStringCollection(IParameterSymbol parameterSymbol)
    {
        var type = parameterSymbol?.Type as INamedTypeSymbol;

        return type is
        {
            Name: "IEnumerable" or "IReadOnlySet" or "IReadOnlyCollection" or "ICollection" or "ISet" or "HashSet",
            IsGenericType: true,
            TypeArguments.Length: 1,
            TypeParameters.Length: 1,
            ContainingNamespace:
            {
                Name: "Generic",
                ContainingNamespace:
                {
                    Name: "Collections",
                    ContainingNamespace.Name: "System"
                }
            }
        };
    }

    private static string ResolveDuplicateStrategy(object? value)
    {
        return value switch
        {
            int v => v switch
            {
                0 => KnownTypes.DuplicateStrategySkipShortName,
                1 => KnownTypes.DuplicateStrategyReplaceShortName,
                2 => KnownTypes.DuplicateStrategyAppendShortName,
                _ => KnownTypes.DuplicateStrategySkipShortName
            },
            string text => text,
            _ => KnownTypes.DuplicateStrategySkipShortName
        };
    }

    private static string ResolveRegistrationStrategy(object? value)
    {
        return value switch
        {
            int v => v switch
            {
                0 => KnownTypes.RegistrationStrategySelfShortName,
                1 => KnownTypes.RegistrationStrategyImplementedInterfacesShortName,
                2 => KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
                _ => KnownTypes.RegistrationStrategySelfWithInterfacesShortName
            },
            string text => text,
            _ => KnownTypes.RegistrationStrategySelfWithInterfacesShortName
        };
    }
}
