using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Injectio.Generators;

/// <summary>
/// Serializable diagnostic information for pipeline caching.
/// Captures all data needed to report a diagnostic without holding Roslyn symbols.
/// </summary>
public record DiagnosticInfo(
    string Id,
    string FilePath,
    TextSpan TextSpan,
    LinePositionSpan LineSpan,
    EquatableArray<string> MessageArguments
);
