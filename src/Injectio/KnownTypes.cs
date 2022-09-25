namespace Injectio;

internal static class KnownTypes
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

    public const string ModuleAttributeShortName = "RegisterModule";
    public const string ModuleAttributeTypeName = $"{ModuleAttributeShortName}Attribute";
    public const string ModuleAttributeFullName = $"{AbstractionNamespace}.{ModuleAttributeTypeName}";


    public const string ServiceLifetimeSingletonShortName = "Singleton";
    public const string ServiceLifetimeSingletonTypeName = $"ServiceLifetime.{ServiceLifetimeSingletonShortName}";

    public const string ServiceLifetimeScopedShortName = "Scoped";
    public const string ServiceLifetimeScopedTypeName = $"ServiceLifetime.{ServiceLifetimeScopedShortName}";

    public const string ServiceLifetimeTransientShortName = "Transient";
    public const string ServiceLifetimeTransientTypeName = $"ServiceLifetime.{ServiceLifetimeTransientShortName}";


    public const string DuplicateStrategySkipShortName = "Skip";
    public const string DuplicateStrategySkipTypeName = $"DuplicateStrategy.{DuplicateStrategySkipShortName}";

    public const string DuplicateStrategyReplaceShortName = "Replace";
    public const string DuplicateStrategyReplaceTypeName = $"DuplicateStrategy.{DuplicateStrategyReplaceShortName}";

    public const string DuplicateStrategyAppendShortName = "Append";
    public const string DuplicateStrategyAppendTypeName = $"DuplicateStrategy.{DuplicateStrategyAppendShortName}";


    public const string RegistrationStrategySelfShortName = "Self";
    public const string RegistrationStrategySelfTypeName = $"RegistrationStrategy.{RegistrationStrategySelfShortName}";

    public const string RegistrationStrategyImplementedInterfacesShortName = "ImplementedInterfaces";
    public const string RegistrationStrategyImplementedInterfacesTypeName = $"RegistrationStrategy.{RegistrationStrategyImplementedInterfacesShortName}";

    public const string RegistrationStrategySelfWithInterfacesShortName = "SelfWithInterfaces";
    public const string RegistrationStrategySelfWithInterfacesTypeName = $"RegistrationStrategy.{RegistrationStrategySelfWithInterfacesShortName}";
}
