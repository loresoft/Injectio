; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|-------
INJ0001 | Usage    | Warning  | RegisterServices method has invalid signature
INJ0002 | Usage    | Warning  | RegisterServices method has invalid second parameter
INJ0003 | Usage    | Warning  | RegisterServices method has too many parameters
INJ0004 | Usage    | Warning  | Factory method not found
INJ0005 | Usage    | Warning  | Factory method must be static
INJ0006 | Usage    | Warning  | Factory method has invalid signature
INJ0007 | Usage    | Warning  | Implementation does not implement service type
INJ0008 | Usage    | Warning  | Implementation type is abstract
INJ0009 | Usage    | Warning  | RegisterServices on non-static method in abstract class
INJ0010 | Usage    | Warning  | Decorator does not implement service type
INJ0011 | Usage    | Warning  | Decorator is missing service type
INJ0012 | Usage    | Warning  | Decorator has no constructor accepting the inner service
INJ0013 | Usage    | Warning  | Decorator factory method not found
INJ0014 | Usage    | Warning  | Decorator factory method has invalid signature
