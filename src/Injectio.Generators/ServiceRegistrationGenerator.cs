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
        // find all classes and methods with attributes
        var registrations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: SyntacticPredicate,
                transform: SemanticTransform
            )
            .Where(static context =>
                context is not null
                && (context.ServiceRegistrations?.Count > 0 || context.ModuleRegistrations?.Count > 0 || context.StaticObjectRegistrations?.Count > 0)
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

        var staticObjectRegistrations = source.Registrations
            .SelectMany(m => m?.StaticObjectRegistrations ?? Array.Empty<StaticObjectRegistration>())
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
            staticObjectRegistrations,
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
            ClassDeclarationSyntax {AttributeLists.Count: > 0} declaration =>
                !declaration.Modifiers.Any(SyntaxKind.AbstractKeyword)
                && !declaration.Modifiers.Any(SyntaxKind.StaticKeyword),

            RecordDeclarationSyntax {AttributeLists.Count: > 0} declaration =>
                !declaration.Modifiers.Any(SyntaxKind.AbstractKeyword)
                && !declaration.Modifiers.Any(SyntaxKind.StaticKeyword),

            MethodDeclarationSyntax {AttributeLists.Count: > 0} declaration =>
                !declaration.Modifiers.Any(SyntaxKind.AbstractKeyword),

            FieldDeclarationSyntax {AttributeLists.Count: > 0} declaration =>
                ValidateFieldIsRegistable(declaration),

            _ => false
        };
    }

    private static ServiceRegistrationContext? SemanticTransform(GeneratorSyntaxContext context, CancellationToken cancellationToken)
    {
        return context.Node switch
        {
            ClassDeclarationSyntax => SemanticTransformClass(context),
            RecordDeclarationSyntax => SemanticTransformClass(context),
            MethodDeclarationSyntax => SemanticTransformMethod(context),
            FieldDeclarationSyntax => SemanticTransformField(context),
            _ => null
        };
    }

    private static ServiceRegistrationContext? SemanticTransformField(GeneratorSyntaxContext context)
    {
        if (context.Node is not FieldDeclarationSyntax fieldDeclaration)
            return null;

        var fieldSymbol = fieldDeclaration.Declaration.Variables
            .Select(x=>context.SemanticModel.GetDeclaredSymbol(x))
            .OfType<IFieldSymbol>()
            .FirstOrDefault();
        if (fieldSymbol is null)
            return null;

        var attributes = fieldSymbol.GetAttributes();

        // support multiple register attributes on a class
        var registrations = new List<StaticObjectRegistration>();
        foreach (var attribute in attributes)
        {
            var registration = CreateStaticObjectRegistration(fieldSymbol, attribute);
            if (registration is not null)
                registrations.Add(registration);
        }

        if (registrations.Count == 0)
            return null;


        return new ServiceRegistrationContext(StaticObjectRegistrations: registrations.ToArray());
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

        var (isValid, hasTagCollection) = ValidateMethod(methodSymbol);
        if (!isValid)
            return null;

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

    private static bool ValidateFieldIsRegistable(FieldDeclarationSyntax fieldDeclarationSyntax)
    {
        //check field is static and public, and that its declaring class is also public.
        //so we could reference field and its value safely
        var isFieldPublic = fieldDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword);
        var isFieldStatic = fieldDeclarationSyntax.Modifiers.Any(SyntaxKind.StaticKeyword);

        if (!(isFieldPublic && isFieldStatic))
            return false;

        for (var node = fieldDeclarationSyntax.Parent; node != null; node = node.Parent)
        {
            if (node is ClassDeclarationSyntax classDeclarationSyntax)
            {
                if (!classDeclarationSyntax.Modifiers.Any(SyntaxKind.PublicKeyword))
                    return false;
            }
            else if (node is BaseNamespaceDeclarationSyntax)
            {
                //skip remain
                break;
            }
        }

        //we only accept one variable in once declaration.
        if (fieldDeclarationSyntax.Declaration.Variables.Count != 1)
            return false;

        //field must have a initializer or it nothing makes scene
        var containInitializer = fieldDeclarationSyntax.Declaration.Variables.Any(x=>x.Initializer != null);
        if (!containInitializer)
            return false;

        return true;
    }

    private static (bool isValid, bool hasTagCollection) ValidateMethod(IMethodSymbol methodSymbol)
    {
        var hasServiceCollection = false;
        var hasTagCollection = false;

        // validate first parameter should be service collection
        if (methodSymbol.Parameters.Length is 1 or 2)
        {
            var parameterSymbol = methodSymbol.Parameters[0];
            hasServiceCollection = IsServiceCollection(parameterSymbol);
        }

        if (methodSymbol.Parameters.Length is 1)
            return (hasServiceCollection, false);

        // validate second parameter should be string collection
        if (methodSymbol.Parameters.Length is 2)
        {
            var parameterSymbol = methodSymbol.Parameters[1];
            hasTagCollection = IsStringCollection(parameterSymbol);

            // to be valid, parameter 0 must be service collection and parameter 1 must be string collection,
            return (hasServiceCollection && hasTagCollection, hasTagCollection);
        }

        // invalid method
        return (false, false);
    }

    private static StaticObjectRegistration? CreateStaticObjectRegistration(IFieldSymbol fieldSymbol, AttributeData attribute)
    {
        // check for known attribute
        if (!IsKnownAttribute(attribute, out var serviceLifetime))
            return null;


        // defaults
        var serviceTypes = new HashSet<string>();
        string? duplicateStrategy = null;
        string? registrationStrategy = null;
        var tags = new HashSet<string>();
        string? serviceKey = null;

        var attributeClass = attribute.AttributeClass;
        if (attributeClass is {IsGenericType: true} &&
            attributeClass.TypeArguments.Length == attributeClass.TypeParameters.Length)
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

                    var serviceType = serviceTypeSymbol?.ToDisplayString(_fullyQualifiedNullableFormat) ??
                                      value.ToString();
                    serviceTypes.Add(serviceType);
                    break;
                case "ServiceKey":
                    serviceKey = parameter.Value.ToCSharpString();
                    break;
                case "ImplementationType":
                    //not support
                    break;
                case "Factory":
                    //not support
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
            && serviceTypes.Count == 0)
            registrationStrategy = KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;

        // add implemented interfaces
        var includeInterfaces = registrationStrategy is KnownTypes.RegistrationStrategyImplementedInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;
        if (includeInterfaces)
            foreach (var implementedInterface in fieldSymbol.Type.AllInterfaces)
            {
                // This interface is typically not injected into services and, more specifically, record types auto-implement it.
                if (implementedInterface.ConstructedFrom.ToString() == "System.IEquatable<T>")
                    continue;

                var unboundInterface = ToUnboundGenericType(implementedInterface);

                var interfaceName = unboundInterface.ToDisplayString(_fullyQualifiedNullableFormat);
                serviceTypes.Add(interfaceName);
            }

        // add class attribute is on; default service type if not set
        var includeSelf = registrationStrategy is KnownTypes.RegistrationStrategySelfShortName
            or KnownTypes.RegistrationStrategySelfWithInterfacesShortName
            or KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;
        if (includeSelf || serviceTypes.Count == 0)
            serviceTypes.Add(fieldSymbol.Type.ToDisplayString(_fullyQualifiedNullableFormat));

        return new StaticObjectRegistration(
            ClassName: fieldSymbol.ContainingType.ToDisplayString(_fullyQualifiedNullableFormat),
            MemberName: fieldSymbol.Name,
            ServiceTypes: serviceTypes.ToArray(),
            Duplicate: duplicateStrategy,
            Tags: tags.ToArray(),
            FileLine: GetFileLine(attribute),
            ServiceKey: serviceKey);
    }

    private static FileLine? GetFileLine(AttributeData attribute)
    {
        var attrSyntaxRef = attribute.ApplicationSyntaxReference;
        if (attrSyntaxRef != null)
        {
            var location = attrSyntaxRef.SyntaxTree.GetLineSpan(attrSyntaxRef.Span);
            var filePath = location.Path;
            var line = location.StartLinePosition.Line + 1;
            return new FileLine(filePath, line);
        }

        //maybe attrbute/field is from others like metadata.
        return null;
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
                    var serviceTypeSymbol = value as INamedTypeSymbol;
                    isOpenGeneric = isOpenGeneric || IsOpenGeneric(serviceTypeSymbol);

                    var serviceType = serviceTypeSymbol?.ToDisplayString(_fullyQualifiedNullableFormat) ?? value.ToString();
                    serviceTypes.Add(serviceType);
                    break;
                case "ServiceKey":
                    serviceKey = parameter.Value.ToCSharpString();
                    break;
                case "ImplementationType":
                    var implementationTypeSymbol = value as INamedTypeSymbol;
                    isOpenGeneric = isOpenGeneric || IsOpenGeneric(implementationTypeSymbol);

                    implementationType = implementationTypeSymbol?.ToDisplayString(_fullyQualifiedNullableFormat) ?? value.ToString();
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
            registrationStrategy = KnownTypes.RegistrationStrategySelfWithProxyFactoryShortName;
        }

        // no implementation type set, use class attribute is on
        if (implementationType.IsNullOrWhiteSpace())
        {
            var unboundType = ToUnboundGenericType(classSymbol);
            isOpenGeneric = isOpenGeneric || IsOpenGeneric(unboundType);
            implementationType = unboundType.ToDisplayString(_fullyQualifiedNullableFormat);
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

                var unboundInterface = ToUnboundGenericType(implementedInterface);
                isOpenGeneric = isOpenGeneric || IsOpenGeneric(unboundInterface);

                var interfaceName = unboundInterface.ToDisplayString(_fullyQualifiedNullableFormat);
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
            IsOpenGeneric: isOpenGeneric,
            FileLine: GetFileLine(attribute));
    }

    private static INamedTypeSymbol ToUnboundGenericType(INamedTypeSymbol typeSymbol)
    {
        if (!typeSymbol.IsGenericType || typeSymbol.IsUnboundGenericType)
            return typeSymbol;

        foreach (var typeArgument in typeSymbol.TypeArguments)
        {
            // If TypeKind is TypeParameter, it's actually the name of a locally declared type-parameter -> placeholder
            if (typeArgument.TypeKind != TypeKind.TypeParameter)
                return typeSymbol;
        }

        return typeSymbol.ConstructUnboundGenericType();
    }

    private static bool IsKnownAttribute(AttributeData attribute, out string serviceLifetime)
    {
        if (IsSingletonAttribute(attribute))
        {
            serviceLifetime = KnownTypes.ServiceLifetimeSingletonFullName;
            return true;
        }

        if (IsScopedAttribute(attribute))
        {
            serviceLifetime = KnownTypes.ServiceLifetimeScopedFullName;
            return true;
        }

        if (IsTransientAttribute(attribute))
        {
            serviceLifetime = KnownTypes.ServiceLifetimeTransientFullName;
            return true;
        }

        if (IsStaticObjectAttribute(attribute))
        {
            serviceLifetime = KnownTypes.ServiceLifetimeSingletonFullName;
            return true;
        }


        serviceLifetime = KnownTypes.ServiceLifetimeTransientFullName;
        return false;
    }

    private static bool IsStaticObjectAttribute(AttributeData attribute)
    {
        return attribute?.AttributeClass is
        {
            Name: KnownTypes.StaticObjectAttributeShortName or KnownTypes.StaticObjectAttributeTypeName,
            ContainingNamespace:
            {
                Name: "Attributes",
                ContainingNamespace.Name: "Injectio"
            }
        };
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
