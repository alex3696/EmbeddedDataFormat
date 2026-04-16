using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Text;
using System.Linq;
using System;

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

{namespaceName}";

        var methodSource = $@"
            public int GetSize() => {properties.Length};
            public object[] Decompose(object obj)
            {{
                var data = ({classSymbol.Name})obj;
                int size = GetSize();
                int index = 0;
                object[] flatObj = new object[size];
        {GenerateDecomposer(properties)}
                return flatObj;
            }}";
        if (!IsNested(classSymbol))
        {
            var classSource = $@"
            {source} 
            partial {strOrCls} {classSymbol.Name}
            {{
                {methodSource}
            }}";

            context.AddSource($"{classSymbol.Name}.g.cs", SourceText.From(classSource, Encoding.UTF8));
        }
        else
        {
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
        }


            //context.AddSource($"{classSymbol.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
        }

    private static string GenerateSizeCalc(ImmutableArray<IPropertySymbol> props)
    {
        var sb = new StringBuilder();
        int i = 0;
        foreach (var prop in props)
            i++;
        sb.Append($"{Tab(2)}size += {i};");
        return sb.ToString();
    }
    private static string GenerateDecomposer(ImmutableArray<IPropertySymbol> props)
    {
        var sb = new StringBuilder();
        foreach (var prop in props)
            GeneratePropertyWrite(prop, sb);
        return sb.ToString();
    }
    private static void GeneratePropertyWrite(IPropertySymbol prop, StringBuilder sb)
    {
        string pname = $"{prop.Name}";

        switch (prop.Type.SpecialType)
        {
            default: break;
            case SpecialType.System_Byte:
                //sb.Append($"{Tab(2)}dest[offset] = {pname};");
                sb.Append($"{Tab(2)}flatObj[index] = (byte)data.{pname};");
                sb.AppendLine($" index += 1;");
                return;
            case SpecialType.System_SByte:
                sb.Append($"{Tab(2)}flatObj[index] = (sbyte)data.{pname};");
                sb.AppendLine($" index += 1;");
                return;
            case SpecialType.System_UInt16:
                sb.Append($"{Tab(2)}flatObj[index] = (ushort)data.{pname};");
                sb.AppendLine($" index += 1;");
                return;
            case SpecialType.System_Int16:
                sb.Append($"{Tab(2)}flatObj[index] = (short)data.{pname};");
                sb.AppendLine($" index += 1;");
                return;
            case SpecialType.System_UInt32:
                sb.Append($"{Tab(2)}flatObj[index] = (uint)data.{pname};");
                sb.AppendLine($" index += 1;");
                return;
            case SpecialType.System_Int32:
                sb.Append($"{Tab(2)}flatObj[index] = (int)data.{pname};");
                sb.AppendLine($" index += 1;");
                return;
            case SpecialType.System_UInt64:
                sb.Append($"{Tab(2)}flatObj[index] = (ulong)data.{pname};");
                sb.AppendLine($" index += 1;");
                return;
            case SpecialType.System_Int64:
                sb.Append($"{Tab(2)}flatObj[index] = (long)data.{pname};");
                sb.AppendLine($" index += 1;");
                return;
            case SpecialType.System_Single:
                sb.Append($"{Tab(2)}flatObj[index] = (Single)data.{pname};");
                sb.AppendLine($" index += 1;");
                return;
            case SpecialType.System_Double:
                sb.Append($"{Tab(2)}flatObj[index] = (Double)data.{pname};");
                sb.AppendLine($" index += 1;");
                return;
            case SpecialType.System_String:
                sb.AppendLine($"{Tab(2)} flatObj[index] = data.{pname}; index += 1;");
                return;
            case SpecialType.System_Array:
                return;

        }
    }
    private static bool IsNested(INamedTypeSymbol classSymbol)
    {
        var nested = classSymbol.ContainingType;
        if(nested is not null)
            return true;
        return false;
    }
    //private static string GenerateForNested(INamedTypeSymbol classSymbol)
    //{
    //    StringBuilder sb = new();
    //    var nested = classSymbol.ContainingType;
    //    while (nested != null)
    //    {
    //        string strOrCls = nested.IsValueType ? "struct" : "class";
    //        sb.Append($"public partial {strOrCls} {nested.Name}");
    //    }
    //    
    //}
}
