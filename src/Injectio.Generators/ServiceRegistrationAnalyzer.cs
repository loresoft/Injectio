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
            DiagnosticDescriptors.DecoratorFactoryInvalidSignature,
            DiagnosticDescriptors.DecoratorOpenGenericKeyed,
            DiagnosticDescriptors.DecoratorTargetNotRegistered
        );

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeMethod, SymbolKind.Method);
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        context.RegisterCompilationStartAction(AnalyzeCompilation);
    }

    private static void AnalyzeCompilation(CompilationStartAnalysisContext context)
    {
        var decorators = new List<DecoratorInfo>();
        var registeredServices = new HashSet<string>(StringComparer.Ordinal);
        var hasModule = false;

        context.RegisterSymbolAction(symbolContext =>
        {
            if (symbolContext.Symbol is IMethodSymbol methodSymbol)
            {
                foreach (var attribute in methodSymbol.GetAttributes())
                {
                    if (SymbolHelpers.IsMethodAttribute(attribute))
                    {
                        lock (registeredServices) hasModule = true;
                        break;
                    }
                }
                return;
            }

            if (symbolContext.Symbol is not INamedTypeSymbol classSymbol)
                return;

            if (classSymbol.IsStatic)
                return;

            var attributes = classSymbol.GetAttributes();

            foreach (var attribute in attributes)
            {
                if (SymbolHelpers.IsDecoratorAttribute(attribute))
                {
                    var info = ExtractDecoratorInfo(classSymbol, attribute);
                    if (info != null)
                        lock (decorators) decorators.Add(info);
                    continue;
                }

                if (!SymbolHelpers.IsKnownAttribute(attribute, out _))
                    continue;

                CollectRegisteredServiceTypes(classSymbol, attribute, registeredServices);
            }
        }, SymbolKind.NamedType, SymbolKind.Method);

        context.RegisterCompilationEndAction(endContext =>
        {
            foreach (var decorator in decorators)
            {
                // INJ0015 — open-generic + keyed combination
                if (decorator.IsOpenGeneric && (decorator.HasServiceKey || decorator.IsAnyKey))
                {
                    endContext.ReportDiagnostic(Diagnostic.Create(
                        DiagnosticDescriptors.DecoratorOpenGenericKeyed,
                        decorator.Location,
                        decorator.DecoratorType,
                        decorator.ServiceType));
                }

                // INJ0016 — target not registered (only when no module could register it dynamically)
                if (!hasModule && !decorator.HasServiceKey && !decorator.IsAnyKey)
                {
                    if (!registeredServices.Contains(decorator.ServiceType))
                    {
                        endContext.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.DecoratorTargetNotRegistered,
                            decorator.Location,
                            decorator.DecoratorType,
                            decorator.ServiceType));
                    }
                }
            }
        });
    }

    private sealed class DecoratorInfo
    {
        public string DecoratorType { get; set; } = string.Empty;
        public string ServiceType { get; set; } = string.Empty;
        public bool IsOpenGeneric { get; set; }
        public bool HasServiceKey { get; set; }
        public bool IsAnyKey { get; set; }
        public Location Location { get; set; } = Location.None;
    }

    private static DecoratorInfo? ExtractDecoratorInfo(INamedTypeSymbol classSymbol, AttributeData attribute)
    {
        string? serviceType = null;
        bool hasServiceKey = false;
        bool isAnyKey = false;
        bool isOpenGeneric = false;

        var attributeClass = attribute.AttributeClass;
        if (attributeClass is { IsGenericType: true } && attributeClass.TypeArguments.Length >= 1)
        {
            var typeArgument = attributeClass.TypeArguments[0];
            if (typeArgument is INamedTypeSymbol namedService && namedService.IsGenericType && namedService.IsUnboundGenericType)
                isOpenGeneric = true;

            serviceType = typeArgument.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
        }

        foreach (var parameter in attribute.NamedArguments)
        {
            var name = parameter.Key;
            var value = parameter.Value.Value;

            switch (name)
            {
                case "ServiceType":
                    if (value is INamedTypeSymbol svc)
                    {
                        if (svc.IsGenericType && svc.IsUnboundGenericType)
                            isOpenGeneric = true;
                        serviceType = svc.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                    }
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

        if (serviceType is null)
            return null;

        var location = classSymbol.Locations.Length > 0 ? classSymbol.Locations[0] : Location.None;

        return new DecoratorInfo
        {
            DecoratorType = classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat),
            ServiceType = serviceType,
            IsOpenGeneric = isOpenGeneric,
            HasServiceKey = hasServiceKey,
            IsAnyKey = isAnyKey,
            Location = location,
        };
    }

    private static void CollectRegisteredServiceTypes(
        INamedTypeSymbol classSymbol,
        AttributeData attribute,
        HashSet<string> registeredServices)
    {
        string? implementationType = null;
        string? registrationStrategy = null;
        var localServiceTypes = new List<string>();

        var attributeClass = attribute.AttributeClass;
        if (attributeClass is { IsGenericType: true } && attributeClass.TypeArguments.Length == attributeClass.TypeParameters.Length)
        {
            for (var index = 0; index < attributeClass.TypeParameters.Length; index++)
            {
                var typeParameter = attributeClass.TypeParameters[index];
                var typeArgument = attributeClass.TypeArguments[index];

                if (typeParameter.Name == "TService" || index == 0)
                    localServiceTypes.Add(typeArgument.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));
                else if (typeParameter.Name == "TImplementation" || index == 1)
                    implementationType = typeArgument.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
            }
        }

        foreach (var parameter in attribute.NamedArguments)
        {
            var name = parameter.Key;
            var value = parameter.Value.Value;
            if (value is null) continue;

            switch (name)
            {
                case "ServiceType":
                    if (value is INamedTypeSymbol svc)
                        localServiceTypes.Add(svc.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));
                    break;
                case "ImplementationType":
                    if (value is INamedTypeSymbol impl)
                        implementationType = impl.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                    break;
                case "Registration":
                    registrationStrategy = SymbolHelpers.ResolveRegistrationStrategy(value);
                    break;
            }
        }

        implementationType ??= classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);

        if (registrationStrategy == null && localServiceTypes.Count == 0)
            registrationStrategy = KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;

        bool includeInterfaces = registrationStrategy is KnownTypes.RegistrationStrategyImplementedInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName
            or null;

        if (includeInterfaces)
        {
            foreach (var iface in classSymbol.AllInterfaces)
            {
                if (iface.ConstructedFrom.ToString() == "System.IEquatable<T>")
                    continue;

                var unbound = SymbolHelpers.ToUnboundGenericType(iface);
                localServiceTypes.Add(unbound.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat));
            }
        }

        bool includeSelf = registrationStrategy is KnownTypes.RegistrationStrategySelfShortName
            or KnownTypes.RegistrationStrategySelfWithInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName
            or null;

        if (includeSelf || localServiceTypes.Count == 0)
            localServiceTypes.Add(implementationType);

        lock (registeredServices)
        {
            foreach (var t in localServiceTypes)
                registeredServices.Add(t);
        }
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
            var location = classSymbol.Locations.Length > 0
                ? classSymbol.Locations[0]
                : Location.None;

            if (SymbolHelpers.IsDecoratorAttribute(attribute))
            {
                AnalyzeDecoratorAttribute(context, classSymbol, attribute, location);
                continue;
            }

            if (!SymbolHelpers.IsKnownAttribute(attribute, out _))
                continue;

            AnalyzeRegistrationAttribute(context, classSymbol, attribute, location);
        }
    }

    private static void AnalyzeDecoratorAttribute(
        SymbolAnalysisContext context,
        INamedTypeSymbol classSymbol,
        AttributeData attribute,
        Location location)
    {
        string? serviceTypeName = null;
        INamedTypeSymbol? serviceTypeSymbol = null;
        string? factory = null;
        bool hasServiceKey = false;
        bool isAnyKey = false;

        var attributeClass = attribute.AttributeClass;
        if (attributeClass is { IsGenericType: true } && attributeClass.TypeArguments.Length >= 1)
        {
            if (attributeClass.TypeArguments[0] is INamedTypeSymbol serviceArg)
            {
                serviceTypeSymbol = serviceArg;
                serviceTypeName = serviceArg.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
            }
        }

        foreach (var parameter in attribute.NamedArguments)
        {
            var name = parameter.Key;
            var value = parameter.Value.Value;

            switch (name)
            {
                case "ServiceType":
                    if (value is INamedTypeSymbol svc)
                    {
                        serviceTypeSymbol = svc;
                        serviceTypeName = svc.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                    }
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
        if (serviceTypeName is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DecoratorMissingServiceType,
                location,
                classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat)));
            return;
        }

        // INJ0010 — class does not implement service
        var classTypeName = classSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
        if (serviceTypeName != classTypeName)
        {
            var implementsService = false;

            foreach (var iface in classSymbol.AllInterfaces)
            {
                var ifaceName = iface.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                if (ifaceName == serviceTypeName)
                {
                    implementsService = true;
                    break;
                }

                var unboundIface = SymbolHelpers.ToUnboundGenericType(iface);
                if (!SymbolEqualityComparer.Default.Equals(unboundIface, iface))
                {
                    var unboundName = unboundIface.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                    if (unboundName == serviceTypeName)
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
                    if (baseName == serviceTypeName) { implementsService = true; break; }

                    var unboundBase = SymbolHelpers.ToUnboundGenericType(baseType);
                    if (!SymbolEqualityComparer.Default.Equals(unboundBase, baseType))
                    {
                        var unboundBaseName = unboundBase.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                        if (unboundBaseName == serviceTypeName) { implementsService = true; break; }
                    }

                    baseType = baseType.BaseType;
                }
            }

            if (!implementsService)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DecoratorDoesNotImplementService,
                    location,
                    classTypeName,
                    serviceTypeName));
            }
        }

        // INJ0012/13/14 — constructor or factory validation
        if (factory.HasValue())
        {
            ValidateDecoratorFactory(context, classSymbol, factory!, hasServiceKey || isAnyKey, location);
        }
        else
        {
            var hasCompatibleCtor = false;
            foreach (var ctor in classSymbol.InstanceConstructors)
            {
                if (ctor.DeclaredAccessibility == Accessibility.Private) continue;
                if (ctor.Parameters.Length == 0) continue;

                var firstParamType = ctor.Parameters[0].Type.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                if (firstParamType == serviceTypeName)
                {
                    hasCompatibleCtor = true;
                    break;
                }

                if (ctor.Parameters[0].Type is INamedTypeSymbol paramNamed)
                {
                    var unboundParam = SymbolHelpers.ToUnboundGenericType(paramNamed);
                    if (!SymbolEqualityComparer.Default.Equals(unboundParam, paramNamed))
                    {
                        var unboundName = unboundParam.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                        if (unboundName == serviceTypeName)
                        {
                            hasCompatibleCtor = true;
                            break;
                        }
                    }
                }

                // any parameter matches?
                foreach (var param in ctor.Parameters)
                {
                    var paramType = param.Type.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                    if (paramType == serviceTypeName)
                    {
                        hasCompatibleCtor = true;
                        break;
                    }
                }

                if (hasCompatibleCtor) break;
            }

            if (!hasCompatibleCtor && classSymbol.InstanceConstructors.Length > 0)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    DiagnosticDescriptors.DecoratorMissingInnerConstructor,
                    location,
                    classTypeName,
                    serviceTypeName));
            }
        }
    }

    private static void ValidateDecoratorFactory(
        SymbolAnalysisContext context,
        INamedTypeSymbol classSymbol,
        string factoryMethodName,
        bool isKeyed,
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
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.DecoratorFactoryNotFound,
                location,
                factoryMethodName,
                className));
            return;
        }

        var expectedParamCount = isKeyed ? 3 : 2;

        foreach (var method in factoryMethods)
        {
            if (!method.IsStatic) continue;
            if (method.Parameters.Length != expectedParamCount) continue;

            if (!SymbolHelpers.IsServiceProvider(method.Parameters[0])) continue;

            if (isKeyed)
            {
                if (method.Parameters[1].Type.SpecialType != SpecialType.System_Object) continue;
                // parameter[2] is the inner service — not strictly checked
            }

            return; // valid overload found
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.DecoratorFactoryInvalidSignature,
            location,
            factoryMethodName,
            className));
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

        // validate service type assignability (skip when a factory is specified, as the factory produces the service)
        if (implementationFactory.IsNullOrWhiteSpace())
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
