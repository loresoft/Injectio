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
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // find all classes and methods with attributes
        var registrations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: SyntacticPredicate,
                transform: SemanticTransform
            )
            .Where(static context =>
                context is not null
                && (context.ServiceRegistrations?.Count > 0 || context.ModuleRegistrations?.Count > 0)
            )
            .Collect()
            .WithTrackingName("Registrations");

        // include compilation options
        var assemblyName = context.CompilationProvider
            .Select(static (c, _) => c.AssemblyName)
            .WithTrackingName("AssemblyName");

        // include config options
        var methodName = context.AnalyzerConfigOptionsProvider
            .Select(static (c, _) =>
            {
                c.GlobalOptions.TryGetValue("build_property.InjectioName", out var methodName);
                c.GlobalOptions.TryGetValue("build_property.InjectioInternal", out var methodInternal);
                return new MethodOptions(methodName, methodInternal);
            })
            .WithTrackingName("Options");

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
        var isKnown = attributes.Any(SymbolHelpers.IsMethodAttribute);
        if (!isKnown)
            return null;

        var (isValid, hasTagCollection) = ValidateMethod(methodSymbol);
        if (!isValid)
            return null;

        var registration = new ModuleRegistration
        (
            ClassName: methodSymbol.ContainingType.ToDisplayString(SymbolHelpers.FullyQualifiedNullableFormat),
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

    private static ServiceRegistration? CreateServiceRegistration(INamedTypeSymbol classSymbol, AttributeData attribute)
    {
        // check for known attribute
        if (!SymbolHelpers.IsKnownAttribute(attribute, out var serviceLifetime))
            return null;

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
                if (SymbolHelpers.IsSystemEquatable(implementedInterface))
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
            ServiceTypes: serviceTypes.ToArray(),
            ServiceKey: serviceKey,
            Factory: implementationFactory,
            Duplicate: duplicateStrategy ?? KnownTypes.DuplicateStrategySkipShortName,
            Registration: registrationStrategy ?? KnownTypes.RegistrationStrategySelfWithInterfacesShortName,
            Tags: tags.ToArray(),
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
