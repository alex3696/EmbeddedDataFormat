using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EdfGenerator;

public static class Common
{
    public const string Attribute = "Attribute";
    public const string Namespace = "EdfNet";
    public const string SerializeAttribute = "EdfSerializableAttribute";
    public const string ArrayAttribute = "EdfArrayAttribute";

    public static bool IsAttribute(this string? str, string attribute)
    {
        return str != null && (str.Contains(attribute) || str.Contains(attribute.Replace(Common.Attribute, string.Empty)));
    }

    public static INamedTypeSymbol? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        //var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classDeclaration = (TypeDeclarationSyntax)context.Node;
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                // Быстрая текстовая проверка, чтобы зря не нагружать семантическую модель
                string attrName = attribute.Name.ToString();
                if (!IsAttribute(attrName, SerializeAttribute))
                    continue;

                if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol &&
                    attributeSymbol.ContainingType.ToDisplayString() == $"{Namespace}.{SerializeAttribute}")
                {
                    return context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
                }
            }
        }
        return null;
    }
}
