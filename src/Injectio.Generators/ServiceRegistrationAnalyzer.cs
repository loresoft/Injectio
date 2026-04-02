using System.Collections.Immutable;

using Injectio.Generators.Extensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Injectio.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ServiceRegistrationAnalyzer : DiagnosticAnalyzer
{
    private static readonly SymbolDisplayFormat _fullyQualifiedNullableFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
        );

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } =
        ImmutableArray.Create(
            DiagnosticDescriptors.InvalidMethodSignature,
            DiagnosticDescriptors.InvalidMethodSecondParameter,
            DiagnosticDescriptors.MethodTooManyParameters,
            DiagnosticDescriptors.FactoryMethodNotFound,
            DiagnosticDescriptors.FactoryMethodNotStatic,
            DiagnosticDescriptors.FactoryMethodInvalidSignature,
            DiagnosticDescriptors.ServiceTypeMismatch,
            DiagnosticDescriptors.AbstractImplementationType,
            DiagnosticDescriptors.RegisterServicesMethodOnAbstractClass);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    private static void AnalyzeMethod(SymbolAnalysisContext context)
    {
        if (context.Symbol is not IMethodSymbol methodSymbol)
            return;

        var attributes = methodSymbol.GetAttributes();
        var isKnown = false;

        foreach (var attribute in attributes)
        {
            if (IsMethodAttribute(attribute))
            {
                isKnown = true;
                break;
            }
        }

        if (!isKnown)
            return;

        var location = methodSymbol.Locations.Length > 0
            ? methodSymbol.Locations[0]
            : Location.None;

        // warn if non-static method on abstract class
        if (!methodSymbol.IsStatic && methodSymbol.ContainingType.IsAbstract)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.RegisterServicesMethodOnAbstractClass,
                location,
                methodSymbol.Name,
                methodSymbol.ContainingType.ToDisplayString(_fullyQualifiedNullableFormat)));
        }

        ValidateMethod(context, methodSymbol, location);
    }

    private static void ValidateMethod(SymbolAnalysisContext context, IMethodSymbol methodSymbol, Location location)
    {
        if (methodSymbol.Parameters.Length > 2)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.MethodTooManyParameters,
                location,
                methodSymbol.Name,
                methodSymbol.Parameters.Length.ToString()));
            return;
        }

        if (methodSymbol.Parameters.Length == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidMethodSignature,
                location,
                methodSymbol.Name));
            return;
        }

        var hasServiceCollection = IsServiceCollection(methodSymbol.Parameters[0]);

        if (!hasServiceCollection)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.InvalidMethodSignature,
                location,
                methodSymbol.Name));
            return;
        }

        if (methodSymbol.Parameters.Length == 2)
        {
            var hasTagCollection = IsStringCollection(methodSymbol.Parameters[1]);

            if (!hasTagCollection)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.InvalidMethodSecondParameter,
                    location,
                    methodSymbol.Name));
            }
        }
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol classSymbol)
            return;

        if (classSymbol.IsAbstract || classSymbol.IsStatic)
            return;

        var attributes = classSymbol.GetAttributes();

        foreach (var attribute in attributes)
        {
            if (!IsKnownAttribute(attribute, out _))
                continue;

            var location = classSymbol.Locations.Length > 0
                ? classSymbol.Locations[0]
                : Location.None;

            AnalyzeRegistrationAttribute(context, classSymbol, attribute, location);
        }
    }

    private static void AnalyzeRegistrationAttribute(
        SymbolAnalysisContext context,
        INamedTypeSymbol classSymbol,
        AttributeData attribute,
        Location location)
    {
        var serviceTypes = new HashSet<string>();
        string? implementationType = null;
        string? implementationFactory = null;
        string? registrationStrategy = null;

        var attributeClass = attribute.AttributeClass;
        if (attributeClass is { IsGenericType: true } && attributeClass.TypeArguments.Length == attributeClass.TypeParameters.Length)
        {
            for (var index = 0; index < attributeClass.TypeParameters.Length; index++)
            {
                var typeParameter = attributeClass.TypeParameters[index];
                var typeArgument = attributeClass.TypeArguments[index];

                if (typeParameter.Name == "TService" || index == 0)
                {
                    serviceTypes.Add(typeArgument.ToDisplayString(_fullyQualifiedNullableFormat));
                }
                else if (typeParameter.Name == "TImplementation" || index == 1)
                {
                    implementationType = typeArgument.ToDisplayString(_fullyQualifiedNullableFormat);
                }
            }
        }

        foreach (var parameter in attribute.NamedArguments)
        {
            var name = parameter.Key;
            var value = parameter.Value.Value;

            if (string.IsNullOrEmpty(name) || value == null)
                continue;

            switch (name)
            {
                case "ServiceType":
                    var serviceTypeSymbol = value as INamedTypeSymbol;
                    var serviceType = serviceTypeSymbol?.ToDisplayString(_fullyQualifiedNullableFormat) ?? value.ToString();
                    serviceTypes.Add(serviceType);
                    break;
                case "ImplementationType":
                    var implSymbol = value as INamedTypeSymbol;
                    implementationType = implSymbol?.ToDisplayString(_fullyQualifiedNullableFormat) ?? value.ToString();
                    break;
                case "Factory":
                    implementationFactory = value.ToString();
                    break;
                case "Registration":
                    registrationStrategy = ResolveRegistrationStrategy(value);
                    break;
            }
        }

        // resolve effective implementation type
        var implTypeName = implementationType.IsNullOrWhiteSpace()
            ? classSymbol.ToDisplayString(_fullyQualifiedNullableFormat)
            : implementationType!;

        // determine effective registration strategy
        if (registrationStrategy == null && implementationType == null && serviceTypes.Count == 0)
            registrationStrategy = KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;

        // add interface-based service types for validation
        bool includeInterfaces = registrationStrategy is KnownTypes.RegistrationStrategyImplementedInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;

        if (includeInterfaces)
        {
            foreach (var iface in classSymbol.AllInterfaces)
            {
                if (iface.ConstructedFrom.ToString() == "System.IEquatable<T>")
                    continue;

                serviceTypes.Add(iface.ToDisplayString(_fullyQualifiedNullableFormat));
            }
        }

        bool includeSelf = registrationStrategy is KnownTypes.RegistrationStrategySelfShortName
            or KnownTypes.RegistrationStrategySelfWithInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;

        if (includeSelf || serviceTypes.Count == 0)
            serviceTypes.Add(implTypeName);

        // validate abstract implementation type without factory
        if (classSymbol.IsAbstract && implementationFactory.IsNullOrWhiteSpace() && implTypeName == classSymbol.ToDisplayString(_fullyQualifiedNullableFormat))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.AbstractImplementationType,
                location,
                implTypeName));
        }

        // validate factory method
        if (implementationFactory.HasValue())
        {
            ValidateFactoryMethod(context, classSymbol, implementationFactory!, location);
        }

        // validate service type assignability (only for explicitly specified service types)
        ValidateServiceTypes(context, classSymbol, serviceTypes, location);
    }

    private static void ValidateFactoryMethod(
        SymbolAnalysisContext context,
        INamedTypeSymbol classSymbol,
        string factoryMethodName,
        Location location)
    {
        var className = classSymbol.ToDisplayString(_fullyQualifiedNullableFormat);
        var members = classSymbol.GetMembers(factoryMethodName);
        var factoryMethods = new List<IMethodSymbol>();

        foreach (var member in members)
        {
            if (member is IMethodSymbol method)
                factoryMethods.Add(method);
        }

        if (factoryMethods.Count == 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.FactoryMethodNotFound,
                location,
                factoryMethodName,
                className));
            return;
        }

        foreach (var method in factoryMethods)
        {
            if (!method.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FactoryMethodNotStatic,
                    location,
                    factoryMethodName,
                    className));
                return;
            }

            if (method.Parameters.Length is not (1 or 2))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FactoryMethodInvalidSignature,
                    location,
                    factoryMethodName,
                    className));
                return;
            }

            if (!IsServiceProvider(method.Parameters[0]))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.FactoryMethodInvalidSignature,
                    location,
                    factoryMethodName,
                    className));
            }
        }
    }

    private static void ValidateServiceTypes(
        SymbolAnalysisContext context,
        INamedTypeSymbol classSymbol,
        HashSet<string> serviceTypes,
        Location location)
    {
        var implTypeName = classSymbol.ToDisplayString(_fullyQualifiedNullableFormat);

        foreach (var serviceType in serviceTypes)
        {
            if (serviceType == implTypeName)
                continue;

            var implementsService = false;

            foreach (var iface in classSymbol.AllInterfaces)
            {
                var ifaceName = iface.ToDisplayString(_fullyQualifiedNullableFormat);
                if (ifaceName == serviceType)
                {
                    implementsService = true;
                    break;
                }
            }

            if (!implementsService)
            {
                var baseType = classSymbol.BaseType;
                while (baseType is not null)
                {
                    var baseName = baseType.ToDisplayString(_fullyQualifiedNullableFormat);
                    if (baseName == serviceType)
                    {
                        implementsService = true;
                        break;
                    }
                    baseType = baseType.BaseType;
                }
            }

            if (!implementsService)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.ServiceTypeMismatch,
                    location,
                    implTypeName,
                    serviceType));
            }
        }
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

    private static bool IsKnownAttribute(AttributeData attribute, out string serviceLifetime)
    {
        if (attribute?.AttributeClass is
            {
                Name: KnownTypes.SingletonAttributeShortName or KnownTypes.SingletonAttributeTypeName,
                ContainingNamespace: { Name: "Attributes", ContainingNamespace.Name: "Injectio" }
            })
        {
            serviceLifetime = KnownTypes.ServiceLifetimeSingletonFullName;
            return true;
        }

        if (attribute?.AttributeClass is
            {
                Name: KnownTypes.ScopedAttributeShortName or KnownTypes.ScopedAttributeTypeName,
                ContainingNamespace: { Name: "Attributes", ContainingNamespace.Name: "Injectio" }
            })
        {
            serviceLifetime = KnownTypes.ServiceLifetimeScopedFullName;
            return true;
        }

        if (attribute?.AttributeClass is
            {
                Name: KnownTypes.TransientAttributeShortName or KnownTypes.TransientAttributeTypeName,
                ContainingNamespace: { Name: "Attributes", ContainingNamespace.Name: "Injectio" }
            })
        {
            serviceLifetime = KnownTypes.ServiceLifetimeTransientFullName;
            return true;
        }

        serviceLifetime = KnownTypes.ServiceLifetimeTransientFullName;
        return false;
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

    private static bool IsServiceProvider(IParameterSymbol parameterSymbol)
    {
        return parameterSymbol?.Type is
        {
            Name: "IServiceProvider",
            ContainingNamespace:
            {
                Name: "System",
                ContainingNamespace.IsGlobalNamespace: true
            }
        };
    }

    private static string ResolveRegistrationStrategy(object? value)
    {
        return value switch
        {
            int v => v switch
            {
                KnownTypes.RegistrationStrategySelfValue => KnownTypes.RegistrationStrategySelfShortName,
                KnownTypes.RegistrationStrategyImplementedInterfacesValue => KnownTypes.RegistrationStrategyImplementedInterfacesShortName,
                KnownTypes.RegistrationStrategySelfWithInterfacesValue => KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
                KnownTypes.RegistrationStrategySelfWithProxyFactoryValue => KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName,
                _ => KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName
            },
            string text => text,
            _ => KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName
        };
    }
}
