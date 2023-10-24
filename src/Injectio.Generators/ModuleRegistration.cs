namespace Injectio.Generators;

public sealed class ModuleRegistration : IEquatable<ModuleRegistration>
{
    public ModuleRegistration(
        string className,
        string methodName,
        bool isStatic,
        bool hasTagCollection)
    {
        ClassName = className;
        MethodName = methodName;
        IsStatic = isStatic;
        HasTagCollection = hasTagCollection;
    }

    public string ClassName { get; }

    public string MethodName { get; }

    public bool IsStatic { get; }

    public bool HasTagCollection { get; }

    public bool Equals(ModuleRegistration other)
    {
        if (ReferenceEquals(null, other))
            return false;
        if (ReferenceEquals(this, other))
            return true;

        return ClassName == other.ClassName
               && MethodName == other.MethodName
               && IsStatic == other.IsStatic;
    }

    public override bool Equals(object obj)
    {
        return obj is ModuleRegistration moduleRegistration
               && Equals(moduleRegistration);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(ClassName, MethodName, IsStatic);
    }

    public static bool operator ==(ModuleRegistration left, ModuleRegistration right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ModuleRegistration left, ModuleRegistration right)
    {
        return !Equals(left, right);
    }
}
