using System.Collections.Immutable;

using Injectio.Generators.Extensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Injectio.Generators;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class ServiceRegistrationAnalyzer : DiagnosticAnalyzer
{
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
            if (SymbolHelpers.IsMethodAttribute(attribute))
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
                methodSymbol.ContainingType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)));
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

        var hasServiceCollection = SymbolHelpers.IsServiceCollection(methodSymbol.Parameters[0]);

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
            var hasTagCollection = SymbolHelpers.IsStringCollection(methodSymbol.Parameters[1]);

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

        if (classSymbol.IsStatic)
            return;

        var attributes = classSymbol.GetAttributes();

        foreach (var attribute in attributes)
        {
            if (!SymbolHelpers.IsKnownAttribute(attribute, out _))
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
                    serviceTypes.Add(typeArgument.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));
                }
                else if (typeParameter.Name == "TImplementation" || index == 1)
                {
                    implementationType = typeArgument.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
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
                    var serviceType = serviceTypeSymbol?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat) ?? value.ToString();
                    serviceTypes.Add(serviceType);
                    break;
                case "ImplementationType":
                    var implSymbol = value as INamedTypeSymbol;
                    implementationType = implSymbol?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat) ?? value.ToString();
                    break;
                case "Factory":
                    implementationFactory = value.ToString();
                    break;
                case "Registration":
                    registrationStrategy = SymbolHelpers.ResolveRegistrationStrategy(value);
                    break;
            }
        }

        // resolve effective implementation type
        var implTypeName = implementationType.IsNullOrWhiteSpace()
            ? classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)
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

                serviceTypes.Add(iface.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));
            }
        }

        bool includeSelf = registrationStrategy is KnownTypes.RegistrationStrategySelfShortName
            or KnownTypes.RegistrationStrategySelfWithInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;

        if (includeSelf || serviceTypes.Count == 0)
            serviceTypes.Add(implTypeName);

        // validate abstract implementation type without factory
        if (classSymbol.IsAbstract && implementationFactory.IsNullOrWhiteSpace() && implTypeName == classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat))
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

        // validate service type assignability
        ValidateServiceTypes(context, classSymbol, serviceTypes, location);
    }

    private static void ValidateFactoryMethod(
        SymbolAnalysisContext context,
        INamedTypeSymbol classSymbol,
        string factoryMethodName,
        Location location)
    {
        var className = classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
        var members = classSymbol.GetMembers(factoryMethodName);
        var factoryMethods = new List<IMethodSymbol>();

        foreach (var member in members)
        {
            if (member is IMethodSymbol method)
                factoryMethods.Add(method);
        }

        if (factoryMethods.Count == 0)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.FactoryMethodNotFound,
                location,
                factoryMethodName,
                className);

            context.ReportDiagnostic(diagnostic);

            return;
        }

        // find at least one valid overload; only report if none exist
        var hasStaticOverload = false;

        foreach (var method in factoryMethods)
        {
            if (!method.IsStatic)
                continue;

            hasStaticOverload = true;

            if (method.Parameters.Length is not (1 or 2))
                continue;

            if (!SymbolHelpers.IsServiceProvider(method.Parameters[0]))
                continue;

            // validate second parameter is object? (for keyed services)
            if (method.Parameters.Length == 2
                && method.Parameters[1].Type.SpecialType != SpecialType.System_Object)
                continue;

            // found a valid overload
            return;
        }

        Diagnostic invalidSignature = Diagnostic.Create(
            hasStaticOverload ? DiagnosticDescriptors.FactoryMethodInvalidSignature : DiagnosticDescriptors.FactoryMethodNotStatic,
            location,
            factoryMethodName,
            className);

        context.ReportDiagnostic(invalidSignature);
    }

    private static void ValidateServiceTypes(
        SymbolAnalysisContext context,
        INamedTypeSymbol classSymbol,
        HashSet<string> serviceTypes,
        Location location)
    {
        var implTypeName = classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);

        foreach (var serviceType in serviceTypes)
        {
            if (serviceType == implTypeName)
                continue;

            var implementsService = false;

            foreach (var iface in classSymbol.AllInterfaces)
            {
                var ifaceName = iface.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                if (ifaceName == serviceType)
                {
                    implementsService = true;
                    break;
                }

                // also check unbound generic form (e.g. IOpenGeneric<> vs IOpenGeneric<T>)
                var unboundIface = SymbolHelpers.ToUnboundGenericType(iface);
                if (!SymbolEqualityComparer.Default.Equals(unboundIface, iface))
                {
                    var unboundName = unboundIface.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                    if (unboundName == serviceType)
                    {
                        implementsService = true;
                        break;
                    }
                }
            }

            if (!implementsService)
            {
                var baseType = classSymbol.BaseType;
                while (baseType is not null)
                {
                    var baseName = baseType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                    if (baseName == serviceType)
                    {
                        implementsService = true;
                        break;
                    }

                    var unboundBase = SymbolHelpers.ToUnboundGenericType(baseType);
                    if (!SymbolEqualityComparer.Default.Equals(unboundBase, baseType))
                    {
                        var unboundBaseName = unboundBase.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                        if (unboundBaseName == serviceType)
                        {
                            implementsService = true;
                            break;
                        }
                    }

                    baseType = baseType.BaseType;
                }
            }

            if (!implementsService)
            {
                Diagnostic diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.ServiceTypeMismatch,
                    location,
                    implTypeName,
                    serviceType);

                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
