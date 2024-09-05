using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;

using Microsoft.CodeAnalysis;

namespace Injectio.Generators;

[ExcludeFromCodeCoverage]
internal static class DiagnosticDescriptors
{
    public static DiagnosticDescriptor InvalidModuleParameter => new(
        id: "SD0010",
        title: "Invalid Module Parameter",
        messageFormat: "Invalid parameter {0} for registration method {1}.  Module registration will be skipped.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );

    public static DiagnosticDescriptor InvalidServiceCollectionParameter => new(
        id: "SD0011",
        title: "Invalid Module Parameter",
        messageFormat:"A parameter of type IServiceCollection was not found for method {0}. Module registration will be skipped.",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true
    );
}
