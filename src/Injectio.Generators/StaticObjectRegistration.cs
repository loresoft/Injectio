namespace Injectio.Generators;

public record StaticObjectRegistration(
    string ClassName,
    string MemberName,
    EquatableArray<string> ServiceTypes,
    string? Duplicate,
    EquatableArray<string> Tags,
    string? ServiceKey,
    FileLine? FileLine
);
