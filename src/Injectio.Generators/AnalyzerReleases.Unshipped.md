; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
INJECT0001 | Injectio | Warning | RegisterServices method has invalid signature
INJECT0002 | Injectio | Warning | RegisterServices method has invalid second parameter
INJECT0003 | Injectio | Warning | RegisterServices method has too many parameters
INJECT0004 | Injectio | Warning | Factory method not found
INJECT0005 | Injectio | Warning | Factory method must be static
INJECT0006 | Injectio | Warning | Factory method has invalid signature
INJECT0007 | Injectio | Warning | Implementation does not implement service type
INJECT0008 | Injectio | Warning | Implementation type is abstract
INJECT0009 | Injectio | Warning | RegisterServices on non-static method in abstract class
