using Microsoft.CodeAnalysis;

namespace Injectio.Generators;

internal static class SymbolHelpers
{
    public static readonly SymbolDisplayFormat FullyQualifiedNullableFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.AddMiscellaneousOptions(
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier
        );

    public static bool IsMethodAttribute(AttributeData attribute)
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

    public static bool IsTransientAttribute(AttributeData attribute)
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

    public static bool IsSingletonAttribute(AttributeData attribute)
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

    public static bool IsScopedAttribute(AttributeData attribute)
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

    public static bool IsKnownAttribute(AttributeData attribute, out string serviceLifetime)
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

        serviceLifetime = KnownTypes.ServiceLifetimeTransientFullName;
        return false;
    }

    public static bool IsServiceCollection(IParameterSymbol parameterSymbol)
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

    public static bool IsStringCollection(IParameterSymbol parameterSymbol)
    {
        var type = parameterSymbol?.Type as INamedTypeSymbol;

        if (type is not
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
            })
        {
            return false;
        }

        // verify the generic argument is string
        return type.TypeArguments[0].SpecialType == SpecialType.System_String;
    }

    public static bool IsServiceProvider(IParameterSymbol parameterSymbol)
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

    public static bool IsSystemEquatable(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.OriginalDefinition is
        {
            MetadataName: "IEquatable`1",
            ContainingNamespace:
            {
                Name: "System",
                ContainingNamespace.IsGlobalNamespace: true
            }
        };
    }

    public static INamedTypeSymbol ToUnboundGenericType(INamedTypeSymbol typeSymbol)
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

    public static string ResolveRegistrationStrategy(object? value)
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
