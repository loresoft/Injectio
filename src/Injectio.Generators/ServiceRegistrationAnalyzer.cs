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
            DiagnosticDescriptors.RegisterServicesMethodOnAbstractClass,
            DiagnosticDescriptors.DecoratorDoesNotImplementService,
            DiagnosticDescriptors.DecoratorMissingServiceType,
            DiagnosticDescriptors.DecoratorMissingInnerConstructor,
            DiagnosticDescriptors.DecoratorFactoryNotFound,
            DiagnosticDescriptors.DecoratorFactoryInvalidSignature
        );

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
        if (attributes.Length == 0)
            return;

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
            Diagnostic diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.RegisterServicesMethodOnAbstractClass,
                location,
                methodSymbol.Name,
                methodSymbol.ContainingType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));

            context.ReportDiagnostic(diagnostic);
        }

        ValidateMethod(context, methodSymbol, location);
    }

    private static void ValidateMethod(
        SymbolAnalysisContext context,
        IMethodSymbol methodSymbol,
        Location location)
    {
        if (methodSymbol.Parameters.Length > 2)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.MethodTooManyParameters,
                location,
                methodSymbol.Name,
                methodSymbol.Parameters.Length.ToString());

            context.ReportDiagnostic(diagnostic);
            return;
        }

        if (methodSymbol.Parameters.Length == 0)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.InvalidMethodSignature,
                location,
                methodSymbol.Name);

            context.ReportDiagnostic(diagnostic);
            return;
        }

        var hasServiceCollection = SymbolHelpers.IsServiceCollection(methodSymbol.Parameters[0]);

        if (!hasServiceCollection)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.InvalidMethodSignature,
                location,
                methodSymbol.Name);

            context.ReportDiagnostic(diagnostic);
            return;
        }

        if (methodSymbol.Parameters.Length != 2)
            return;

        var hasTagCollection = SymbolHelpers.IsStringCollection(methodSymbol.Parameters[1]);
        if (!hasTagCollection)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.InvalidMethodSecondParameter,
                location,
                methodSymbol.Name);

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol classSymbol)
            return;

        if (classSymbol.IsStatic)
            return;

        var attributes = classSymbol.GetAttributes();
        if (attributes.Length == 0)
            return;

        Location location = classSymbol.Locations.Length > 0
            ? classSymbol.Locations[0]
            : Location.None;

        // Lazily compute assignable service types only when validation needs them.
        INamedTypeSymbol? assignableServiceTypeSymbol = null;
        HashSet<ITypeSymbol>? assignableServiceTypes = null;

        // Reuse the same type hierarchy walk across multiple attributes with the same implementation type.
        HashSet<ITypeSymbol> GetAssignableServiceTypesForType(INamedTypeSymbol typeSymbol)
        {
            if (assignableServiceTypeSymbol is not null && SymbolEqualityComparer.Default.Equals(assignableServiceTypeSymbol, typeSymbol))
                return assignableServiceTypes!;

            assignableServiceTypeSymbol = typeSymbol;
            return GetAssignableServiceTypes(typeSymbol);
        }

        foreach (var attribute in attributes)
        {
            if (SymbolHelpers.IsDecoratorAttribute(attribute))
                AnalyzeDecoratorAttribute(context, classSymbol, attribute, location, GetAssignableServiceTypesForType);
            else if (SymbolHelpers.IsKnownAttribute(attribute, out _))
                AnalyzeRegistrationAttribute(context, classSymbol, attribute, location, GetAssignableServiceTypesForType);
        }
    }

    private static void AnalyzeDecoratorAttribute(
        SymbolAnalysisContext context,
        INamedTypeSymbol classSymbol,
        AttributeData attribute,
        Location location,
        Func<INamedTypeSymbol, HashSet<ITypeSymbol>> getAssignableServiceTypes)
    {
        ITypeSymbol? serviceTypeSymbol = null;
        string? factory = null;
        bool hasServiceKey = false;
        bool isAnyKey = false;

        var attributeClass = attribute.AttributeClass;
        if (attributeClass is { IsGenericType: true } && attributeClass.TypeArguments.Length >= 1)
            serviceTypeSymbol = attributeClass.TypeArguments[0];

        foreach (var parameter in attribute.NamedArguments)
        {
            var name = parameter.Key;
            var value = parameter.Value.Value;

            switch (name)
            {
                case "ServiceType":
                    if (value is ITypeSymbol svc)
                        serviceTypeSymbol = svc;

                    break;
                case "Factory":
                    factory = value?.ToString();

                    break;
                case "ServiceKey":
                    hasServiceKey = value is not null;

                    break;
                case "AnyKey":
                    if (value is bool b)
                        isAnyKey = b;

                    break;
            }
        }

        // INJ0011 — missing service type
        if (serviceTypeSymbol is null)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.DecoratorMissingServiceType,
                location,
                classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));

            context.ReportDiagnostic(diagnostic);

            return;
        }

        // INJ0010 — class does not implement service
        if (!SymbolEqualityComparer.Default.Equals(serviceTypeSymbol, classSymbol))
        {
            var assignableTypes = getAssignableServiceTypes(classSymbol);
            var implementsService = ContainsServiceType(assignableTypes, serviceTypeSymbol);

            if (!implementsService)
            {
                Diagnostic diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.DecoratorDoesNotImplementService,
                    location,
                    classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat),
                    serviceTypeSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));

                context.ReportDiagnostic(diagnostic);
            }
        }

        // INJ0012/13/14 — constructor or factory validation
        if (factory.HasValue())
        {
            ValidateDecoratorFactory(context, classSymbol, factory!, serviceTypeSymbol, hasServiceKey || isAnyKey, location);
        }
        else
        {
            var hasCompatibleCtor = false;
            foreach (var ctor in classSymbol.InstanceConstructors)
            {
                if (ctor.DeclaredAccessibility == Accessibility.Private)
                    continue;

                if (ctor.Parameters.Length == 0)
                    continue;

                foreach (var param in ctor.Parameters)
                {
                    if (IsAssignableServiceType(param.Type, serviceTypeSymbol))
                    {
                        hasCompatibleCtor = true;
                        break;
                    }
                }

                if (hasCompatibleCtor)
                    break;
            }

            if (!hasCompatibleCtor && classSymbol.InstanceConstructors.Length > 0)
            {
                Diagnostic diagnostic = Diagnostic.Create(
                    DiagnosticDescriptors.DecoratorMissingInnerConstructor,
                    location,
                    classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat),
                    serviceTypeSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));

                context.ReportDiagnostic(diagnostic);
            }
        }
    }

    private static void ValidateDecoratorFactory(
        SymbolAnalysisContext context,
        INamedTypeSymbol classSymbol,
        string factoryMethodName,
        ITypeSymbol serviceTypeSymbol,
        bool isKeyed,
        Location location)
    {
        var members = classSymbol.GetMembers(factoryMethodName);
        var hasFactoryMethod = false;
        var expectedParamCount = isKeyed ? 3 : 2;

        foreach (var member in members)
        {
            if (member is not IMethodSymbol method)
                continue;

            hasFactoryMethod = true;

            if (!method.IsStatic)
                continue;

            if (method.Parameters.Length != expectedParamCount)
                continue;

            if (!SymbolHelpers.IsServiceProvider(method.Parameters[0]))
                continue;

            if (isKeyed && method.Parameters[1].Type.SpecialType != SpecialType.System_Object)
                continue;

            var innerParameter = method.Parameters[expectedParamCount - 1];
            if (!IsAssignableServiceType(innerParameter.Type, serviceTypeSymbol))
                continue;

            return; // valid overload found
        }

        if (!hasFactoryMethod)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.DecoratorFactoryNotFound,
                location,
                factoryMethodName,
                classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));

            context.ReportDiagnostic(diagnostic);
            return;
        }

        Diagnostic invalidSignature = Diagnostic.Create(
            DiagnosticDescriptors.DecoratorFactoryInvalidSignature,
            location,
            factoryMethodName,
            classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));

        context.ReportDiagnostic(invalidSignature);
    }

    private static void AnalyzeRegistrationAttribute(
        SymbolAnalysisContext context,
        INamedTypeSymbol classSymbol,
        AttributeData attribute,
        Location location,
        Func<INamedTypeSymbol, HashSet<ITypeSymbol>> getAssignableServiceTypes)
    {
        var serviceTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        INamedTypeSymbol? implementationTypeSymbol = null;
        ITypeSymbol? implementationType = null;
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
                    serviceTypes.Add(typeArgument);
                }
                else if (typeParameter.Name == "TImplementation" || index == 1)
                {
                    implementationTypeSymbol = typeArgument as INamedTypeSymbol;
                    implementationType = typeArgument;
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
                    if (value is ITypeSymbol serviceTypeSymbol)
                        serviceTypes.Add(serviceTypeSymbol);

                    break;
                case "ImplementationType":
                    var implSymbol = value as INamedTypeSymbol;
                    implementationTypeSymbol = implSymbol;
                    implementationType = implSymbol;

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
        var effectiveTypeSymbol = implementationTypeSymbol ?? classSymbol;

        // determine effective registration strategy
        if (registrationStrategy == null && implementationType == null && serviceTypes.Count == 0)
            registrationStrategy = KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;

        // add interface-based service types for validation
        bool includeInterfaces = registrationStrategy is KnownTypes.RegistrationStrategyImplementedInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;

        if (includeInterfaces)
        {
            foreach (var interfaceSymbol in effectiveTypeSymbol.AllInterfaces)
            {
                if (SymbolHelpers.IsSystemEquatable(interfaceSymbol))
                    continue;

                serviceTypes.Add(interfaceSymbol);
            }
        }

        bool includeSelf = registrationStrategy is KnownTypes.RegistrationStrategySelfShortName
            or KnownTypes.RegistrationStrategySelfWithInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;

        if (includeSelf || serviceTypes.Count == 0)
            serviceTypes.Add(effectiveTypeSymbol);

        // validate abstract implementation type without factory
        if (classSymbol.IsAbstract && implementationFactory.IsNullOrWhiteSpace() && SymbolEqualityComparer.Default.Equals(effectiveTypeSymbol, classSymbol))
        {
            Diagnostic diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.AbstractImplementationType,
                location,
                effectiveTypeSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));

            context.ReportDiagnostic(diagnostic);
        }

        // validate factory method
        if (implementationFactory.HasValue())
            ValidateFactoryMethod(context, classSymbol, implementationFactory!, location);

        // validate service type assignability (skip when a factory is specified, as the factory produces the service)
        if (implementationFactory.IsNullOrWhiteSpace())
            ValidateServiceTypes(context, effectiveTypeSymbol, serviceTypes, location, getAssignableServiceTypes);
    }

    private static void ValidateFactoryMethod(
        SymbolAnalysisContext context,
        INamedTypeSymbol classSymbol,
        string factoryMethodName,
        Location location)
    {
        var members = classSymbol.GetMembers(factoryMethodName);
        var hasFactoryMethod = false;
        var hasStaticOverload = false;

        foreach (var member in members)
        {
            if (member is not IMethodSymbol method)
                continue;

            hasFactoryMethod = true;

            if (!method.IsStatic)
                continue;

            hasStaticOverload = true;

            if (method.Parameters.Length is not (1 or 2))
                continue;

            if (!SymbolHelpers.IsServiceProvider(method.Parameters[0]))
                continue;

            // validate second parameter is object? (for keyed services)
            if (method.Parameters.Length == 2 && method.Parameters[1].Type.SpecialType != SpecialType.System_Object)
                continue;

            // found a valid overload
            return;
        }

        if (!hasFactoryMethod)
        {
            Diagnostic diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.FactoryMethodNotFound,
                location,
                factoryMethodName,
                classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));

            context.ReportDiagnostic(diagnostic);

            return;
        }

        Diagnostic invalidSignature = Diagnostic.Create(
            hasStaticOverload ? DiagnosticDescriptors.FactoryMethodInvalidSignature : DiagnosticDescriptors.FactoryMethodNotStatic,
            location,
            factoryMethodName,
            classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));

        context.ReportDiagnostic(invalidSignature);
    }

    private static void ValidateServiceTypes(
        SymbolAnalysisContext context,
        INamedTypeSymbol classSymbol,
        HashSet<ITypeSymbol> serviceTypes,
        Location location,
        Func<INamedTypeSymbol, HashSet<ITypeSymbol>> getAssignableServiceTypes)
    {
        var assignableTypes = getAssignableServiceTypes(classSymbol);

        foreach (var serviceType in serviceTypes)
        {
            if (SymbolEqualityComparer.Default.Equals(serviceType, classSymbol))
                continue;

            if (ContainsServiceType(assignableTypes, serviceType))
                continue;

            Diagnostic diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ServiceTypeMismatch,
                location,
                classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat),
                serviceType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));

            context.ReportDiagnostic(diagnostic);
        }
    }

    private static HashSet<ITypeSymbol> GetAssignableServiceTypes(INamedTypeSymbol classSymbol)
    {
        var assignableTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        AddAssignableServiceTypes(assignableTypes, classSymbol);

        if (classSymbol.IsUnboundGenericType)
            AddAssignableServiceTypes(assignableTypes, classSymbol.OriginalDefinition);

        return assignableTypes;
    }

    private static void AddAssignableServiceTypes(
        HashSet<ITypeSymbol> assignableTypes,
        INamedTypeSymbol classSymbol,
        bool includeSelf = false)
    {
        if (includeSelf)
            AddAssignableServiceType(assignableTypes, classSymbol);

        foreach (var iface in classSymbol.AllInterfaces)
            AddAssignableServiceType(assignableTypes, iface);

        var baseType = classSymbol.BaseType;
        while (baseType is not null)
        {
            AddAssignableServiceType(assignableTypes, baseType);

            baseType = baseType.BaseType;
        }
    }

    private static void AddAssignableServiceType(
        HashSet<ITypeSymbol> assignableTypes,
        ITypeSymbol typeSymbol)
    {
        assignableTypes.Add(typeSymbol);

        if (typeSymbol is not INamedTypeSymbol namedTypeSymbol)
            return;

        var openGenericDefinition = GetOpenGenericDefinition(namedTypeSymbol);
        if (openGenericDefinition is not null)
            assignableTypes.Add(openGenericDefinition);
    }

    private static bool ContainsServiceType(
        HashSet<ITypeSymbol> assignableTypes,
        ITypeSymbol serviceType)
    {
        if (assignableTypes.Contains(serviceType))
            return true;

        return serviceType is INamedTypeSymbol serviceNamedType
            && GetOpenGenericDefinition(serviceNamedType) is { } openGenericDefinition
            && assignableTypes.Contains(openGenericDefinition);
    }

    private static bool IsAssignableServiceType(
        ITypeSymbol assignableType,
        ITypeSymbol serviceType)
    {
        if (SymbolEqualityComparer.Default.Equals(assignableType, serviceType))
            return true;

        if (assignableType is not INamedTypeSymbol assignableNamedType)
            return false;

        var assignableOpenGenericDefinition = GetOpenGenericDefinition(assignableNamedType);
        if (assignableOpenGenericDefinition is null)
            return false;

        return serviceType is INamedTypeSymbol serviceNamedType
            && GetOpenGenericDefinition(serviceNamedType) is { } serviceOpenGenericDefinition
            && SymbolEqualityComparer.Default.Equals(assignableOpenGenericDefinition, serviceOpenGenericDefinition);
    }

    private static INamedTypeSymbol? GetOpenGenericDefinition(INamedTypeSymbol typeSymbol)
    {
        if (!typeSymbol.IsGenericType)
            return null;

        if (typeSymbol.IsUnboundGenericType)
            return typeSymbol.OriginalDefinition;

        foreach (var typeArgument in typeSymbol.TypeArguments)
        {
            if (typeArgument.TypeKind != TypeKind.TypeParameter)
                return null;
        }

        return typeSymbol.OriginalDefinition;
    }
}
