namespace Microsoft.CodeAnalysis;

/// <summary>
/// A special attribute recognized by Roslyn, that marks a type as "embedded", meaning it won't ever be visible from other assemblies.
/// </summary>
[global::System.AttributeUsage(global::System.AttributeTargets.All)]
[global::System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
internal sealed partial class EmbeddedAttribute : global::System.Attribute;
