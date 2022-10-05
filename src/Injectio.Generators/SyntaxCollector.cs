using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Injectio.Generators;

internal static class SyntaxCollector
{
    public static bool IsMethodAttributeCanidate(SyntaxNode syntaxNode)
    {
        if (syntaxNode is not MethodDeclarationSyntax methodDeclarationSyntax)
            return false;


        foreach (var attributeList in methodDeclarationSyntax.AttributeLists)
            foreach (var attribute in attributeList.Attributes)
                if (IsKnownMethodAttribute(attribute))
                    return true;

        return false;
    }

    public static bool IsClassAttributeCandidateType(SyntaxNode syntax)
    {
        if (syntax is not ClassDeclarationSyntax typeDeclarationSyntax)
            return false;

        foreach (var attributeList in typeDeclarationSyntax.AttributeLists)
            foreach (var attribute in attributeList.Attributes)
                if (IsKnownClassAttribute(attribute))
                    return true;

        return false;
    }

    public static bool IsKnownClassAttribute(SyntaxNode syntaxNode)
    {
        if (syntaxNode is AttributeSyntax
            {
                Name: SimpleNameSyntax
                {
                    Identifier: { } identifier
                },
                Parent: AttributeListSyntax
                {
                    Parent: ClassDeclarationSyntax
                }
            })
            switch (identifier.Text)
            {
                case KnownTypes.TransientAttributeShortName:
                case KnownTypes.TransientAttributeTypeName:

                case KnownTypes.SingletonAttributeShortName:
                case KnownTypes.SingletonAttributeTypeName:

                case KnownTypes.ScopedAttributeShortName:
                case KnownTypes.ScopedAttributeTypeName:
                    return true;
            }

        return false;
    }

    public static bool IsKnownMethodAttribute(SyntaxNode syntaxNode)
    {
        if (syntaxNode is AttributeSyntax
            {
                Name: SimpleNameSyntax
                {
                    Identifier: { } identifier
                },
                Parent: AttributeListSyntax
                {
                    Parent: MethodDeclarationSyntax
                }
            })
            switch (identifier.Text)
            {
                case KnownTypes.ModuleAttributeShortName:
                case KnownTypes.ModuleAttributeTypeName:
                    return true;
            }

        return false;
    }

}
