using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Buffers;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace EdfBinGenerator;



[Generator]
public class DecomposeGenerator : IIncrementalGenerator
{
    public const string DecomposeAttribute = "DecomposeGenerator";

    public static string Tab(byte n)
    {
        StringBuilder tabs = new(0xFF);
        for (int i = 0; i < n; i++)
            tabs.Append('\t');
        return tabs.ToString();
    }
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var decompose = context.SyntaxProvider
            .CreateSyntaxProvider(
            predicate: static (node, _) => node is TypeDeclarationSyntax { AttributeLists.Count: > 0 },
            transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);
        IncrementalValuesProvider<INamedTypeSymbol> compilationAndClasses = decompose.Select(static (s, _) => s!);

        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(spc, source));

        context.RegisterPostInitializationOutput(i => i.AddSource($"{DecomposeAttribute}.g.cs",
            $@"using System;
            namespace NetEdf;
            [AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
            internal class {DecomposeAttribute} : Attribute {{ }}"));
    }

    private static INamedTypeSymbol? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        //var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var objectDeclaration = (TypeDeclarationSyntax)context.Node;
        foreach (var attributeList in objectDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol &&
                    attributeSymbol.ContainingType.ToDisplayString() == $"NetEdf.{DecomposeAttribute}")
                {
                    return context.SemanticModel.GetDeclaredSymbol(objectDeclaration) as INamedTypeSymbol;
                }
            }
        }
        return null;
    }


    private static void Execute(SourceProductionContext context, INamedTypeSymbol classSymbol)
    {
        string strOrCls = classSymbol.IsValueType ? "struct" : "class";
        var namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : $"namespace {classSymbol.ContainingNamespace.ToDisplayString()};";
        var properties = classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            //.OfType<IFieldSymbol>()
            .Where(p => !p.IsReadOnly && p.DeclaredAccessibility == Accessibility.Public)
            .ToImmutableArray();
        var source = $@"
using System;
using System.Text;
using System.Collections;
using NetEdf;

{namespaceName}

";

    var methodSource = $@"
    public int GetSize() => {properties.Length};

    public object[] Decompose(object obj)
    {{
        var data = ({classSymbol.Name})obj;
        int size = GetSize();
        int index = 0;
        object[] flatObj = new object[256];
    {GenerateDecomposer(properties)}
        return flatObj;
    }}";
            var usings = $@"{source}";
            var classSource = $@"
partial {strOrCls} {classSymbol.Name}
{{
    {methodSource}
}}";
            var nested = classSymbol.ContainingType;
            while (nested != null)
            {
                var typeNested = nested.IsValueType ? "struct" : "class";
                classSource = @$"
                partial {typeNested} {nested.Name}
                {{
                    {classSource}
                }}
                ";
                nested = nested.ContainingType;
            }

            var finalSourse = @$"{usings} {classSource}";
            context.AddSource($"{classSymbol.Name}.g.cs", SourceText.From(finalSourse, Encoding.UTF8));
    //    }


            //context.AddSource($"{classSymbol.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
        }

    private static string GenerateDecomposer(ImmutableArray<IPropertySymbol> props)
    {
        var sb = new StringBuilder();
        foreach (var prop in props)
            GeneratePropertyWrite(prop, sb);
        return sb.ToString();
    }
    private static bool GetTypeProp(SpecialType type)
    {
        switch (type)
        {
            default: break;
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt64:
            case SpecialType.System_Int64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_String:
            return true;
        }
        return false;
    }
    private static void GeneratePropertyWrite(IPropertySymbol prop, StringBuilder sb)
    {
        string pname = $"{prop.Name}";
        if (prop.Type is IArrayTypeSymbol array)
        {
            var elementProp = array.ElementType.GetMembers().OfType<IPropertySymbol>()
                .Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public); 
            sb.AppendLine($"{Tab(2)}foreach(var item in data.{pname})");
            sb.AppendLine($"{Tab(2)}{{");

            foreach (var pr in elementProp)
            {
                if (GetTypeProp(pr.Type.SpecialType))
                {
                    sb.AppendLine($"{Tab(3)}flatObj[index] = item.{pr.Name}; index++;");
                }
                else if(pr.Type is IArrayTypeSymbol nestedArr)
                {
                    sb.AppendLine($"{Tab(3)}foreach(var subitem in item.{pr.Name})");
                    sb.AppendLine($"{Tab(3)}{{");
                    sb.AppendLine($"{Tab(4)}flatObj[index] = subitem; index++;");
                    sb.AppendLine($"{Tab(3)}}}");
                }
                else
                {
                    var nestedProperties = pr.Type.GetMembers().OfType<IPropertySymbol>()
                        .Where(p => !p.IsStatic) ;
                    foreach (var nested in nestedProperties)
                    {
                        if (GetTypeProp(nested.Type.SpecialType))
                            sb.AppendLine($"{Tab(3)}flatObj[index] = item.{pr.Name}.{nested.Name}; index++;");
                    }
                }
            }
            sb.AppendLine($"{Tab(2)}}}");
        }
        else
        {
            if (GetTypeProp(prop.Type.SpecialType))
            {
                sb.AppendLine($"{Tab(2)}flatObj[index] = data.{pname}; index++;");
            }
            else
            {
                var properties = prop.Type.GetMembers().OfType<IPropertySymbol>().
                    Where(p => !p.IsStatic && p.DeclaredAccessibility == Accessibility.Public); ;
                foreach (var property in properties)
                {
                    sb.AppendLine($"flatObj[index] = data.{pname}.{property.Name}; index++;");
                }
            }
        }
    }
}
