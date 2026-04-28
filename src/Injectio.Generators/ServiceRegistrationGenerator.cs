using System.Text;
using System.Text.RegularExpressions;

using Injectio.Generators.Extensions;
using Injectio.Generators.Infrastructure;
using Injectio.Generators.Models;

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
        // add embedded attributes and extensions as additional files to compilation
        context.RegisterPostInitializationOutput(static ctx =>
        {
            ctx.AddEmbeddedAttributeDefinition();

            ctx.AddSource(
                hintName: "Injectio.Attributes.g.cs",
                sourceText: SourceText.From(LoadEmbedded("Attributes.cs"), Encoding.UTF8)
            );
            ctx.AddSource(
                hintName: "Injectio.Extensions.g.cs",
                sourceText: SourceText.From(LoadEmbedded("Extensions.cs"), Encoding.UTF8)
            );
        });

        // separate pipeline per attribute, each returning its specific model
        var transientRegistrations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KnownTypes.TransientAttributeFullName,
                predicate: static (node, _) => IsNonAbstractNonStaticType(node),
                transform: static (ctx, _) => TransformServiceRegistration(ctx, KnownTypes.ServiceLifetimeTransientFullName))
            .WithTrackingName("TransientRegistrations");

        var transientRegistrationsT1 = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KnownTypes.TransientAttributeFullNameT1,
                predicate: static (node, _) => IsNonAbstractNonStaticType(node),
                transform: static (ctx, _) => TransformServiceRegistration(ctx, KnownTypes.ServiceLifetimeTransientFullName))
            .WithTrackingName("TransientRegistrationsT1");

        var transientRegistrationsT2 = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KnownTypes.TransientAttributeFullNameT2,
                predicate: static (node, _) => IsNonAbstractNonStaticType(node),
                transform: static (ctx, _) => TransformServiceRegistration(ctx, KnownTypes.ServiceLifetimeTransientFullName))
            .WithTrackingName("TransientRegistrationsT2");

        var scopedRegistrations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KnownTypes.ScopedAttributeFullName,
                predicate: static (node, _) => IsNonAbstractNonStaticType(node),
                transform: static (ctx, _) => TransformServiceRegistration(ctx, KnownTypes.ServiceLifetimeScopedFullName))
            .WithTrackingName("ScopedRegistrations");

        var scopedRegistrationsT1 = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KnownTypes.ScopedAttributeFullNameT1,
                predicate: static (node, _) => IsNonAbstractNonStaticType(node),
                transform: static (ctx, _) => TransformServiceRegistration(ctx, KnownTypes.ServiceLifetimeScopedFullName))
            .WithTrackingName("ScopedRegistrationsT1");

        var scopedRegistrationsT2 = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KnownTypes.ScopedAttributeFullNameT2,
                predicate: static (node, _) => IsNonAbstractNonStaticType(node),
                transform: static (ctx, _) => TransformServiceRegistration(ctx, KnownTypes.ServiceLifetimeScopedFullName))
            .WithTrackingName("ScopedRegistrationsT2");

        var singletonRegistrations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KnownTypes.SingletonAttributeFullName,
                predicate: static (node, _) => IsNonAbstractNonStaticType(node),
                transform: static (ctx, _) => TransformServiceRegistration(ctx, KnownTypes.ServiceLifetimeSingletonFullName))
            .WithTrackingName("SingletonRegistrations");

        var singletonRegistrationsT1 = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KnownTypes.SingletonAttributeFullNameT1,
                predicate: static (node, _) => IsNonAbstractNonStaticType(node),
                transform: static (ctx, _) => TransformServiceRegistration(ctx, KnownTypes.ServiceLifetimeSingletonFullName))
            .WithTrackingName("SingletonRegistrationsT1");

        var singletonRegistrationsT2 = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KnownTypes.SingletonAttributeFullNameT2,
                predicate: static (node, _) => IsNonAbstractNonStaticType(node),
                transform: static (ctx, _) => TransformServiceRegistration(ctx, KnownTypes.ServiceLifetimeSingletonFullName))
            .WithTrackingName("SingletonRegistrationsT2");

        var decoratorRegistrations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KnownTypes.DecoratorAttributeFullName,
                predicate: static (node, _) => IsNonAbstractNonStaticType(node),
                transform: static (ctx, _) => TransformDecoratorRegistration(ctx))
            .WithTrackingName("DecoratorRegistrations");

        var decoratorRegistrationsT1 = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KnownTypes.DecoratorAttributeFullNameT1,
                predicate: static (node, _) => IsNonAbstractNonStaticType(node),
                transform: static (ctx, _) => TransformDecoratorRegistration(ctx))
            .WithTrackingName("DecoratorRegistrationsT1");

        var decoratorRegistrationsT2 = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KnownTypes.DecoratorAttributeFullNameT2,
                predicate: static (node, _) => IsNonAbstractNonStaticType(node),
                transform: static (ctx, _) => TransformDecoratorRegistration(ctx))
            .WithTrackingName("DecoratorRegistrationsT2");

        var moduleRegistrations = context.SyntaxProvider
            .ForAttributeWithMetadataName(
                KnownTypes.ModuleAttributeFullName,
                predicate: static (node, _) => node is MethodDeclarationSyntax,
                transform: static (ctx, _) => TransformModuleRegistration(ctx))
            .WithTrackingName("ModuleRegistrations");

        // include compilation options
        var assemblyName = context.CompilationProvider
            .Select(static (c, _) => c.AssemblyName)
            .WithTrackingName("AssemblyName");

        // include config options
        var methodOptions = context.AnalyzerConfigOptionsProvider
            .Select(static (c, _) =>
            {
                c.GlobalOptions.TryGetValue("build_property.InjectioName", out var methodName);
                c.GlobalOptions.TryGetValue("build_property.InjectioInternal", out var methodInternal);
                return new MethodOptions(methodName, methodInternal);
            })
            .WithTrackingName("Options");

        // combine all service registration pipelines
        var allServiceRegistrations = transientRegistrations.Collect()
            .Combine(transientRegistrationsT1.Collect())
            .Combine(transientRegistrationsT2.Collect())
            .Combine(scopedRegistrations.Collect())
            .Combine(scopedRegistrationsT1.Collect())
            .Combine(scopedRegistrationsT2.Collect())
            .Combine(singletonRegistrations.Collect())
            .Combine(singletonRegistrationsT1.Collect())
            .Combine(singletonRegistrationsT2.Collect())
            .Select(static (combined, _) =>
            {
                // yuk, side effect of IncrementalValuesProvider combine
                var ((((((((t, t1), t2), s), s1), s2), g), g1), g2) = combined;
                return EquatableArray.Merge(t, t1, t2, s, s1, s2, g, g1, g2);
            })
            .WithTrackingName("AllServiceRegistrations");

        // combine all decorator registration pipelines
        var allDecoratorRegistrations = decoratorRegistrations.Collect()
            .Combine(decoratorRegistrationsT1.Collect())
            .Combine(decoratorRegistrationsT2.Collect())
            .Select(static (combined, _) =>
            {
                var ((d, d1), d2) = combined;
                return EquatableArray.Merge(d, d1, d2);
            })
            .WithTrackingName("AllDecoratorRegistrations");

        // combine all pipelines into a single generation context
        var generation = allServiceRegistrations
            .Combine(moduleRegistrations.Collect())
            .Combine(allDecoratorRegistrations)
            .Combine(assemblyName)
            .Combine(methodOptions)
            .Select(static (combined, _) =>
            {
                var ((((services, modules), decorators), assemblyName), options) = combined;

                var moduleArray = modules
                    .Where(static m => m is not null)
                    .Cast<ModuleRegistration>();

                return new RegistrationContext(
                    ServiceRegistrations: services,
                    ModuleRegistrations: [.. moduleArray],
                    DecoratorRegistrations: decorators,
                    AssemblyName: assemblyName,
                    MethodOptions: options
                );
            })
            .WithTrackingName("Generation");

        context.RegisterSourceOutput(generation, ExecuteGeneration);
    }

    private void ExecuteGeneration(SourceProductionContext sourceContext, RegistrationContext source)
    {
        var serviceRegistrations = source.ServiceRegistrations.AsArray();

        var moduleRegistrations = source.ModuleRegistrations.AsArray();

        var decoratorRegistrations = source.DecoratorRegistrations
            .OrderBy(m => m.ServiceType, StringComparer.Ordinal)
            .ThenBy(m => m.Order)
            .ThenBy(m => m.DecoratorType, StringComparer.Ordinal)
            .ToArray();

        // compute extension method name
        var methodName = source.MethodOptions?.Name;
        if (methodName.IsNullOrWhiteSpace())
            methodName = Regex.Replace(source.AssemblyName ?? string.Empty, "\\W", "");

        var methodInternal = source.MethodOptions?.Internal;

        // generate registration method
        var result = ServiceRegistrationWriter.GenerateExtensionClass(
            moduleRegistrations,
            serviceRegistrations,
            decoratorRegistrations,
            source.AssemblyName,
            methodName,
            methodInternal);

        // add source file
        sourceContext.AddSource("Injectio.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static string LoadEmbedded(string name)
    {
        var assembly = typeof(ServiceRegistrationGenerator).Assembly;
        var resourceName = "Injectio.Generators.Embedded." + name;

        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded resource not found: {resourceName}");

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static bool IsNonAbstractNonStaticType(SyntaxNode node)
    {
        return node is TypeDeclarationSyntax declaration
            and (ClassDeclarationSyntax or RecordDeclarationSyntax)
            && !declaration.Modifiers.Any(SyntaxKind.AbstractKeyword)
            && !declaration.Modifiers.Any(SyntaxKind.StaticKeyword);
    }

    private static EquatableArray<ServiceRegistration> TransformServiceRegistration(GeneratorAttributeSyntaxContext context, string serviceLifetime)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
            return EquatableArray<ServiceRegistration>.Empty;

        // find matching attributes for this specific lifetime
        var results = new List<ServiceRegistration>();
        foreach (var attribute in context.Attributes)
        {
            var registration = CreateServiceRegistration(classSymbol, attribute, serviceLifetime);
            if (registration is not null)
                results.Add(registration);
        }

        return new EquatableArray<ServiceRegistration>(results);
    }

    private static EquatableArray<DecoratorRegistration> TransformDecoratorRegistration(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not INamedTypeSymbol classSymbol)
            return EquatableArray<DecoratorRegistration>.Empty;

        var results = new List<DecoratorRegistration>();
        foreach (var attribute in context.Attributes)
        {
            var decorator = CreateDecoratorRegistration(classSymbol, attribute);
            if (decorator is not null)
                results.Add(decorator);
        }

        return new EquatableArray<DecoratorRegistration>(results);
    }

    private static ModuleRegistration? TransformModuleRegistration(GeneratorAttributeSyntaxContext context)
    {
        if (context.TargetSymbol is not IMethodSymbol methodSymbol)
            return null;

        var (isValid, hasTagCollection) = ValidateMethod(methodSymbol);
        if (!isValid)
            return null;

        return new ModuleRegistration(
            ClassName: methodSymbol.ContainingType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat),
            MethodName: methodSymbol.Name,
            IsStatic: methodSymbol.IsStatic,
            HasTagCollection: hasTagCollection
        );
    }

    private static DecoratorRegistration? CreateDecoratorRegistration(INamedTypeSymbol classSymbol, AttributeData attribute)
    {
        string? serviceType = null;
        string? implementationType = null;
        string? serviceKey = null;
        bool isAnyKey = false;
        string? factory = null;
        int order = 0;
        var tags = new HashSet<string>();
        bool isOpenGeneric = false;

        var attributeClass = attribute.AttributeClass;
        if (attributeClass is { IsGenericType: true } && attributeClass.TypeArguments.Length == attributeClass.TypeParameters.Length)
        {
            for (var index = 0; index < attributeClass.TypeParameters.Length; index++)
            {
                var typeParameter = attributeClass.TypeParameters[index];
                var typeArgument = attributeClass.TypeArguments[index];

                if (typeParameter.Name == "TService" || index == 0)
                {
                    isOpenGeneric = isOpenGeneric || IsOpenGeneric(typeArgument as INamedTypeSymbol);
                    serviceType = typeArgument.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                }
                else if (typeParameter.Name == "TImplementation" || index == 1)
                {
                    isOpenGeneric = isOpenGeneric || IsOpenGeneric(typeArgument as INamedTypeSymbol);
                    implementationType = typeArgument.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                }
            }
        }

        foreach (var parameter in attribute.NamedArguments)
        {
            var name = parameter.Key;
            var value = parameter.Value.Value;

            if (string.IsNullOrEmpty(name))
                continue;

            switch (name)
            {
                case "ServiceType":
                    if (value is INamedTypeSymbol serviceTypeSymbol)
                    {
                        isOpenGeneric = isOpenGeneric || IsOpenGeneric(serviceTypeSymbol);
                        serviceType = serviceTypeSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                    }
                    else if (value != null)
                    {
                        serviceType = value.ToString();
                    }
                    break;
                case "ImplementationType":
                    if (value is INamedTypeSymbol implSymbol)
                    {
                        isOpenGeneric = isOpenGeneric || IsOpenGeneric(implSymbol);
                        implementationType = implSymbol.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                    }
                    else if (value != null)
                    {
                        implementationType = value.ToString();
                    }
                    break;
                case "ServiceKey":
                    serviceKey = parameter.Value.ToCSharpString();
                    break;
                case "AnyKey":
                    if (value is bool anyKey)
                        isAnyKey = anyKey;
                    break;
                case "Factory":
                    factory = value?.ToString();
                    break;
                case "Order":
                    if (value is int orderValue)
                        order = orderValue;
                    break;
                case "Tags":
                    if (value is string tagsText)
                    {
                        foreach (var tag in tagsText.Split(',', ';'))
                        {
                            if (tag.HasValue())
                                tags.Add(tag.Trim());
                        }
                    }
                    break;
            }
        }

        if (implementationType.IsNullOrWhiteSpace())
        {
            var unboundType = SymbolHelpers.ToUnboundGenericType(classSymbol);
            isOpenGeneric = isOpenGeneric || IsOpenGeneric(unboundType);
            implementationType = unboundType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
        }

        if (serviceType.IsNullOrWhiteSpace())
            return null;

        return new DecoratorRegistration(
            DecoratorType: implementationType!,
            ServiceType: serviceType!,
            ServiceKey: serviceKey,
            IsAnyKey: isAnyKey,
            Factory: factory,
            Order: order,
            Tags: [.. tags],
            IsOpenGeneric: isOpenGeneric);
    }

    private static (bool isValid, bool hasTagCollection) ValidateMethod(IMethodSymbol methodSymbol)
    {
        var hasServiceCollection = false;
        var hasTagCollection = false;

        // validate first parameter should be service collection
        if (methodSymbol.Parameters.Length is 1 or 2)
        {
            var parameterSymbol = methodSymbol.Parameters[0];
            hasServiceCollection = SymbolHelpers.IsServiceCollection(parameterSymbol);
        }

        if (methodSymbol.Parameters.Length is 1)
            return (hasServiceCollection, false);

        // validate second parameter should be string collection
        if (methodSymbol.Parameters.Length is 2)
        {
            var parameterSymbol = methodSymbol.Parameters[1];
            hasTagCollection = SymbolHelpers.IsStringCollection(parameterSymbol);

            // to be valid, parameter 0 must be service collection and parameter 1 must be string collection,
            return (hasServiceCollection && hasTagCollection, hasTagCollection);
        }

        // invalid method
        return (false, false);
    }

    private static ServiceRegistration? CreateServiceRegistration(INamedTypeSymbol classSymbol, AttributeData attribute, string serviceLifetime)
    {
        // defaults
        var serviceTypes = new HashSet<string>();
        string implementationType = null!;
        string? implementationFactory = null;
        string? duplicateStrategy = null;
        string? registrationStrategy = null;
        var tags = new HashSet<string>();
        string? serviceKey = null;
        bool isOpenGeneric = false;

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
                    var service = typeArgument.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                    serviceTypes.Add(service);
                }
                else if (typeParameter.Name == "TImplementation" || index == 1)
                {
                    implementationType = typeArgument.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
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
                    var serviceTypeSymbol = value as INamedTypeSymbol;
                    isOpenGeneric = isOpenGeneric || IsOpenGeneric(serviceTypeSymbol);

                    var serviceType = serviceTypeSymbol?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat) ?? value.ToString();
                    serviceTypes.Add(serviceType);
                    break;
                case "ServiceKey":
                    serviceKey = parameter.Value.ToCSharpString();
                    break;
                case "ImplementationType":
                    var implementationTypeSymbol = value as INamedTypeSymbol;
                    isOpenGeneric = isOpenGeneric || IsOpenGeneric(implementationTypeSymbol);

                    implementationType = implementationTypeSymbol?.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat) ?? value.ToString();
                    break;
                case "Factory":
                    implementationFactory = value.ToString();
                    break;
                case "Duplicate":
                    duplicateStrategy = ResolveDuplicateStrategy(value);
                    break;
                case "Registration":
                    registrationStrategy = SymbolHelpers.ResolveRegistrationStrategy(value);
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
            registrationStrategy = KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;
        }

        // no implementation type set, use class attribute is on
        if (implementationType.IsNullOrWhiteSpace())
        {
            var unboundType = SymbolHelpers.ToUnboundGenericType(classSymbol);
            isOpenGeneric = isOpenGeneric || IsOpenGeneric(unboundType);
            implementationType = unboundType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
        }

        // add implemented interfaces
        bool includeInterfaces = registrationStrategy is KnownTypes.RegistrationStrategyImplementedInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;

        if (includeInterfaces)
        {
            foreach (var implementedInterface in classSymbol.AllInterfaces)
            {
                // This interface is typically not injected into services and, more specifically, record types auto-implement it.
                if (implementedInterface.ConstructedFrom.ToString() == "System.IEquatable<T>")
                    continue;

                var unboundInterface = SymbolHelpers.ToUnboundGenericType(implementedInterface);
                isOpenGeneric = isOpenGeneric || IsOpenGeneric(unboundInterface);

                var interfaceName = unboundInterface.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat);
                serviceTypes.Add(interfaceName);
            }
        }

        // add class attribute is on; default service type if not set
        bool includeSelf = registrationStrategy is KnownTypes.RegistrationStrategySelfShortName
            or KnownTypes.RegistrationStrategySelfWithInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;

        if (includeSelf || serviceTypes.Count == 0)
            serviceTypes.Add(implementationType!);

        if (registrationStrategy is null && serviceTypes.Contains(implementationType!))
            registrationStrategy = KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;

        if (registrationStrategy is KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName && isOpenGeneric)
            registrationStrategy = KnownTypes.RegistrationStrategySelfWithInterfacesShortName;

        return new ServiceRegistration(
            Lifetime: serviceLifetime,
            ImplementationType: implementationType!,
            ServiceTypes: [.. serviceTypes],
            ServiceKey: serviceKey,
            Factory: implementationFactory,
            Duplicate: duplicateStrategy ?? KnownTypes.DuplicateStrategySkipShortName,
            Registration: registrationStrategy ?? KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
            Tags: [.. tags],
            IsOpenGeneric: isOpenGeneric);
    }

    private static bool IsOpenGeneric(INamedTypeSymbol? typeSymbol)
    {
        if (typeSymbol is null)
            return false;

        if (!typeSymbol.IsGenericType)
            return false;

        return typeSymbol.IsUnboundGenericType;
    }

    private static string ResolveDuplicateStrategy(object? value)
    {
        return value switch
        {
            int v => v switch
            {
                KnownTypes.DuplicateStrategySkipValue => KnownTypes.DuplicateStrategySkipShortName,
                KnownTypes.DuplicateStrategyReplaceValue => KnownTypes.DuplicateStrategyReplaceShortName,
                KnownTypes.DuplicateStrategyAppendValue => KnownTypes.DuplicateStrategyAppendShortName,
                _ => KnownTypes.DuplicateStrategySkipShortName
            },
            string text => text,
            _ => KnownTypes.DuplicateStrategySkipShortName
        };
    }
}
