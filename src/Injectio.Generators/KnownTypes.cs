using System.Diagnostics.CodeAnalysis;

namespace Injectio.Generators;

[ExcludeFromCodeCoverage]
public static class KnownTypes
{
    public const string AbstractionNamespace = "Injectio.Attributes";

    public const string TransientAttributeShortName = "RegisterTransient";
    public const string TransientAttributeTypeName = $"{TransientAttributeShortName}Attribute";
    public const string TransientAttributeFullName = $"{AbstractionNamespace}.{TransientAttributeTypeName}";

    public const string SingletonAttributeShortName = "RegisterSingleton";
    public const string SingletonAttributeTypeName = $"{SingletonAttributeShortName}Attribute";
    public const string SingletonAttributeFullName = $"{AbstractionNamespace}.{SingletonAttributeTypeName}";

    public const string ScopedAttributeShortName = "RegisterScoped";
    public const string ScopedAttributeTypeName = $"{ScopedAttributeShortName}Attribute";
    public const string ScopedAttributeFullName = $"{AbstractionNamespace}.{ScopedAttributeTypeName}";

    public const string ModuleAttributeShortName = "RegisterServices";
    public const string ModuleAttributeTypeName = $"{ModuleAttributeShortName}Attribute";
    public const string ModuleAttributeFullName = $"{AbstractionNamespace}.{ModuleAttributeTypeName}";

    public const string StaticObjectAttributeShortName = "RegisterStaticObject";
    public const string StaticObjectAttributeTypeName = $"{StaticObjectAttributeShortName}Attribute";
    public const string StaticObjectAttributeFullName = $"{AbstractionNamespace}.{StaticObjectAttributeShortName}";

    public const string ServiceLifetimeSingletonShortName = "Singleton";
    public const string ServiceLifetimeSingletonTypeName = $"ServiceLifetime.{ServiceLifetimeSingletonShortName}";
    public const string ServiceLifetimeSingletonFullName = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton";

    public const string ServiceLifetimeScopedShortName = "Scoped";
    public const string ServiceLifetimeScopedTypeName = $"ServiceLifetime.{ServiceLifetimeScopedShortName}";
    public const string ServiceLifetimeScopedFullName = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped";

    public const string ServiceLifetimeTransientShortName = "Transient";
    public const string ServiceLifetimeTransientTypeName = $"ServiceLifetime.{ServiceLifetimeTransientShortName}";
    public const string ServiceLifetimeTransientFullName = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient";


    public const int DuplicateStrategySkipValue = 0;
    public const string DuplicateStrategySkipShortName = "Skip";
    public const string DuplicateStrategySkipTypeName = $"DuplicateStrategy.{DuplicateStrategySkipShortName}";

    public const int DuplicateStrategyReplaceValue = 1;
    public const string DuplicateStrategyReplaceShortName = "Replace";
    public const string DuplicateStrategyReplaceTypeName = $"DuplicateStrategy.{DuplicateStrategyReplaceShortName}";

    public const int DuplicateStrategyAppendValue = 2;
    public const string DuplicateStrategyAppendShortName = "Append";
    public const string DuplicateStrategyAppendTypeName = $"DuplicateStrategy.{DuplicateStrategyAppendShortName}";


    public const int RegistrationStrategySelfValue = 0;
    public const string RegistrationStrategySelfShortName = "Self";
    public const string RegistrationStrategySelfTypeName = $"RegistrationStrategy.{RegistrationStrategySelfShortName}";

    public const int RegistrationStrategyImplementedInterfacesValue = 1;
    public const string RegistrationStrategyImplementedInterfacesShortName = "ImplementedInterfaces";
    public const string RegistrationStrategyImplementedInterfacesTypeName = $"RegistrationStrategy.{RegistrationStrategyImplementedInterfacesShortName}";

    public const int RegistrationStrategySelfWithInterfacesValue = 2;
    public const string RegistrationStrategySelfWithInterfacesShortName = "SelfWithInterfaces";
    public const string RegistrationStrategySelfWithInterfacesTypeName = $"RegistrationStrategy.{RegistrationStrategySelfWithInterfacesShortName}";

    public const int RegistrationStrategySelfWithProxyFactoryValue = 3;
    public const string RegistrationStrategySelfWithProxyFactoryShortName = "SelfWithProxyFactory";
    public const string RegistrationStrategySelfWithProxyFactoryTypeName = $"RegistrationStrategy.{RegistrationStrategySelfWithProxyFactoryShortName}";

}
