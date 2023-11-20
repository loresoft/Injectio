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


    public const string ServiceLifetimeSingletonShortName = "Singleton";
    public const string ServiceLifetimeSingletonTypeName = $"ServiceLifetime.{ServiceLifetimeSingletonShortName}";

    public const string ServiceLifetimeScopedShortName = "Scoped";
    public const string ServiceLifetimeScopedTypeName = $"ServiceLifetime.{ServiceLifetimeScopedShortName}";

    public const string ServiceLifetimeTransientShortName = "Transient";
    public const string ServiceLifetimeTransientTypeName = $"ServiceLifetime.{ServiceLifetimeTransientShortName}";


    public static readonly int DuplicateStrategySkipValue = 0;
    public const string DuplicateStrategySkipShortName = "Skip";
    public const string DuplicateStrategySkipTypeName = $"DuplicateStrategy.{DuplicateStrategySkipShortName}";

    public static readonly int DuplicateStrategyReplaceValue = 1;
    public const string DuplicateStrategyReplaceShortName = "Replace";
    public const string DuplicateStrategyReplaceTypeName = $"DuplicateStrategy.{DuplicateStrategyReplaceShortName}";

    public static readonly int DuplicateStrategyAppendValue = 2;
    public const string DuplicateStrategyAppendShortName = "Append";
    public const string DuplicateStrategyAppendTypeName = $"DuplicateStrategy.{DuplicateStrategyAppendShortName}";


    public static readonly int RegistrationStrategySelfValue = 0;
    public const string RegistrationStrategySelfShortName = "Self";
    public const string RegistrationStrategySelfTypeName = $"RegistrationStrategy.{RegistrationStrategySelfShortName}";

    public static readonly int RegistrationStrategyImplementedInterfacesValue = 1;
    public const string RegistrationStrategyImplementedInterfacesShortName = "ImplementedInterfaces";
    public const string RegistrationStrategyImplementedInterfacesTypeName = $"RegistrationStrategy.{RegistrationStrategyImplementedInterfacesShortName}";

    public static readonly int RegistrationStrategySelfWithInterfacesValue = 2;
    public const string RegistrationStrategySelfWithInterfacesShortName = "SelfWithInterfaces";
    public const string RegistrationStrategySelfWithInterfacesTypeName = $"RegistrationStrategy.{RegistrationStrategySelfWithInterfacesShortName}";


}
