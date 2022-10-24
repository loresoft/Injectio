using System.Collections.Immutable;
using System.Text;
using System.Text.RegularExpressions;

using Injectio.Generators.Extensions;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace Injectio.Generators;

[Generator]
public class ServiceGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var classProviderTypes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => SyntaxCollector.IsClassAttributeCandidateType(node),
            transform: static (syntaxContext, _) => (ClassDeclarationSyntax)syntaxContext.Node
        );

        var methodProviderTypes = context.SyntaxProvider.CreateSyntaxProvider(
            predicate: static (node, _) => SyntaxCollector.IsMethodAttributeCanidate(node),
            transform: static (syntaxContext, _) => (MethodDeclarationSyntax)syntaxContext.Node
        );

        var options = context.AnalyzerConfigOptionsProvider;

        var classProviders = classProviderTypes.Collect();
        var methodProviders = methodProviderTypes.Collect();

        IncrementalValueProvider<(Compilation Compilation, AnalyzerConfigOptionsProvider AnalyzerOptions)> compilation
            = context.CompilationProvider.Combine(context.AnalyzerConfigOptionsProvider);

        IncrementalValueProvider<(ImmutableArray<ClassDeclarationSyntax> ClassDeclarations, ImmutableArray<MethodDeclarationSyntax> MethodDeclarations)> providers
            = classProviders.Combine(methodProviders);

        IncrementalValueProvider<(
            (Compilation Compilation, AnalyzerConfigOptionsProvider AnalyzerOptions) Left,
            (ImmutableArray<ClassDeclarationSyntax> ClassDeclarations, ImmutableArray<MethodDeclarationSyntax> MethodDeclarations) Right
        )> outputProvider = compilation.Combine(providers);

        context.RegisterSourceOutput(
            source: outputProvider,
            action: static (context, source) => Execute(
                compilation: source.Left.Compilation,
                analyzerOptions: source.Left.AnalyzerOptions,
                classDeclarations: source.Right.ClassDeclarations,
                methodDeclarations: source.Right.MethodDeclarations,
                sourceContext: context
            )
        );
    }

    private static void Execute(
        Compilation compilation,
        AnalyzerConfigOptionsProvider analyzerOptions,
        ImmutableArray<ClassDeclarationSyntax> classDeclarations,
        ImmutableArray<MethodDeclarationSyntax> methodDeclarations,
        SourceProductionContext sourceContext)
    {
        // extract registration details
        var moduleRegistrations = GetModuleRegistrations(compilation, methodDeclarations, sourceContext);
        var serviceRegistrations = GetServiceRegistrations(compilation, classDeclarations, sourceContext);

        // compute extension method name
        analyzerOptions.GlobalOptions.TryGetValue("build_property.injectioname", out var methodName);
        if (methodName.IsNullOrWhiteSpace())
            methodName = Regex.Replace(compilation.AssemblyName, "\\W", "");

        // generate registration method
        string result = CodeGenerator.GenerateExtensionClass(moduleRegistrations, serviceRegistrations, compilation.AssemblyName, methodName);

        // add source file
        sourceContext.AddSource("Injectio.g.cs", SourceText.From(result, Encoding.UTF8));
    }

    private static IReadOnlyList<ModuleRegistration> GetModuleRegistrations(
        Compilation compilation,
        IEnumerable<MethodDeclarationSyntax> methodDeclarations,
        SourceProductionContext sourceContext)
    {
        var moduleRegistrations = new List<ModuleRegistration>();

        foreach (var methodDeclaration in methodDeclarations)
        {
            var semanticModel = compilation.GetSemanticModel(methodDeclaration.SyntaxTree);
            var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);

            if (methodSymbol == null)
                continue;

            ValidateParameters(sourceContext, methodDeclaration, methodSymbol);

            var registration = new ModuleRegistration
            {
                ClassName = methodSymbol.ContainingType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat),
                MethodName = methodSymbol.Name,
                IsStatic = methodSymbol.IsStatic
            };

            moduleRegistrations.Add(registration);
        }


        return moduleRegistrations;
    }

    private static IReadOnlyList<ServiceRegistration> GetServiceRegistrations(
        Compilation compilation,
        IEnumerable<ClassDeclarationSyntax> classDeclarations,
        SourceProductionContext sourceContext)
    {
        var serviceRegistrations = new List<ServiceRegistration>();

        foreach (var classDeclaration in classDeclarations)
        {
            // need to convert to sematic model to get all interfaces implmented
            var semanticModel = compilation.GetSemanticModel(classDeclaration.SyntaxTree);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration);

            if (classSymbol == null)
                continue;

            var attributes = classSymbol.GetAttributes();

            // support multiple register attributes on a class
            foreach (var attribute in attributes)
            {
                var registration = ReadAttribute(attribute);
                if (registration == null)
                    continue; // skip unknown attributes

                if (registration.ImplementationType.IsNullOrWhiteSpace())
                    registration.ImplementationType = classSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

                // if registration is All Interfaces or null
                bool includeInterfaces = IncludeInterfaces(registration);
                if (includeInterfaces)
                    foreach (var implementedInterface in classSymbol.AllInterfaces)
                    {
                        var interfaceName = implementedInterface.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
                        registration.ServiceTypes.Add(interfaceName);
                    }

                // if registration is self
                bool includeSelf = IncludeSelf(registration);
                if (includeSelf)
                    registration.ServiceTypes.Add(registration.ImplementationType);

                serviceRegistrations.Add(registration);
            }
        }

        return serviceRegistrations;
    }

    private static bool IncludeSelf(ServiceRegistration registration)
    {
        return registration.Registration switch
        {
            // include self when nothing set only when no other service types set
            null => registration.ServiceTypes.Count == 0,
            int and (0 or 2) => true,
            string and (
                KnownTypes.RegistrationStrategySelfShortName
                or KnownTypes.RegistrationStrategySelfTypeName
                or KnownTypes.RegistrationStrategySelfWithInterfacesShortName
                or KnownTypes.RegistrationStrategySelfWithInterfacesTypeName
            ) => true,
            _ => false
        };
    }

    private static bool IncludeInterfaces(ServiceRegistration registration)
    {
        return registration.Registration switch
        {
            // include interfaces when nothing set only when no other service types set
            null => registration.ServiceTypes.Count == 0,
            int and (1 or 2) => true,
            string and (
                KnownTypes.RegistrationStrategyImplementedInterfacesShortName
                or KnownTypes.RegistrationStrategyImplementedInterfacesTypeName
                or KnownTypes.RegistrationStrategySelfWithInterfacesShortName
                or KnownTypes.RegistrationStrategySelfWithInterfacesTypeName
            ) => true,
            _ => false
        };
    }

    private static bool IsKnownAttribute(string attributeName, out string serviceLifetime)
    {
        serviceLifetime = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient";

        switch (attributeName)
        {
            case KnownTypes.TransientAttributeShortName:
            case KnownTypes.TransientAttributeTypeName:
            case KnownTypes.TransientAttributeFullName:
                serviceLifetime = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Transient";
                return true;
            case KnownTypes.SingletonAttributeShortName:
            case KnownTypes.SingletonAttributeTypeName:
            case KnownTypes.SingletonAttributeFullName:
                serviceLifetime = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton";
                return true;
            case KnownTypes.ScopedAttributeShortName:
            case KnownTypes.ScopedAttributeTypeName:
            case KnownTypes.ScopedAttributeFullName:
                serviceLifetime = "Microsoft.Extensions.DependencyInjection.ServiceLifetime.Scoped";
                return true;
        }

        return false;
    }

    private static bool IsServiceCollection(IParameterSymbol parameterSymbol)
    {
        return parameterSymbol.Type is
        {
            Name: "IServiceCollection" or "ServiceCollection",
            ContainingNamespace:
            {
                Name: "DependencyInjection",
                ContainingNamespace:
                {
                    Name: "Extensions",
                    ContainingNamespace:
                    {
                        Name: "Microsoft"
                    }
                }
            }
        };
    }

    private static void ValidateParameters(SourceProductionContext context, MethodDeclarationSyntax methodDeclaration, IMethodSymbol methodSymbol)
    {
        var hasServiceCollection = false;
        var methodName = methodSymbol.Name;

        foreach (var parameterSymbol in methodSymbol.Parameters)
        {
            bool isServiceCollection = IsServiceCollection(parameterSymbol);

            if (isServiceCollection)
            {
                hasServiceCollection = true;
                continue;
            }

            var parameterDiagnostic = Diagnostic.Create(
                new DiagnosticDescriptor(
                    id: "SD0010",
                    title: "Invalid Module Parameter",
                    messageFormat: "Invalid parameter {0} for registration method {1}.  Module registration will be skipped.",
                    category: "Usage",
                    defaultSeverity: DiagnosticSeverity.Warning,
                    isEnabledByDefault: true
                ),
                methodDeclaration.GetLocation(),
                parameterSymbol.Name,
                methodName
            );

            context.ReportDiagnostic(parameterDiagnostic);
        }

        if (hasServiceCollection)
            return;

        // no paramater for service collection
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor(
                id: "SD0011",
                title: "Invalid Module Parameter",
                messageFormat: "A parameter of type IServiceCollection was not found for method {0}. Module registration will be skipped.",
                category: "Usage",
                defaultSeverity: DiagnosticSeverity.Warning,
                isEnabledByDefault: true
            ),
            methodDeclaration.GetLocation(),
            methodName
        );

        context.ReportDiagnostic(diagnostic);
    }

    private static ServiceRegistration ReadAttribute(AttributeData attribute)
    {
        var attributeName = attribute.AttributeClass?.ToString();

        // check for known attribute
        if (!IsKnownAttribute(attributeName, out var serviceLifetime))
            return null;

        // defaults
        var registration = new ServiceRegistration
        {
            Lifetime = serviceLifetime
        };

        foreach (var parameter in attribute.NamedArguments)
        {
            // match name with service registration configuration
            var name = parameter.Key;
            var value = parameter.Value.Value;

            if (string.IsNullOrEmpty(name) || value == null)
                continue;

            switch (name)
            {
                case "ServiceType":
                    registration.ServiceTypes.Add(value?.ToString());
                    break;
                case "ImplementationType":
                    registration.ImplementationType = value?.ToString();
                    break;
                case "Factory":
                    registration.Factory = value?.ToString();
                    break;
                case "Duplicate":
                    registration.Duplicate = value;
                    break;
                case "Registration":
                    registration.Registration = value;
                    break;
            }
        }

        // set defaults
        registration.Duplicate ??= 0;

        if (registration.Registration == null
            && registration.ImplementationType == null
            && registration.ServiceTypes.Count == 0)
        {
            registration.Registration = 2; // SelfWithInterfaces
        }

        return registration;
    }

}
