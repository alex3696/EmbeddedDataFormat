using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;


namespace EdfBinGenerator;


[Generator]
public class FastBinaryGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Поиск классов с атрибутом [BinarySerializable]
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        // 2. Сборка данных для генерации (компиляция + список классов)
        IncrementalValuesProvider<INamedTypeSymbol> compilationAndClasses = classDeclarations.Select(static (s, _) => s!);

        // 3. Регистрация самого источника кода
        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(spc, source));

        // Добавляем сам атрибут в проект
        context.RegisterPostInitializationOutput(i => i.AddSource("BinarySerializableAttribute.g.cs", @"
using System;
namespace NetEdf;
[AttributeUsage(AttributeTargets.Class)]
internal class BinarySerializableAttribute : Attribute { }"));
    }

    private static INamedTypeSymbol? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        var classDeclaration = (ClassDeclarationSyntax)context.Node;
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol &&
                    attributeSymbol.ContainingType.ToDisplayString() == "NetEdf.BinarySerializableAttribute")
                {
                    return context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
                }
            }
        }
        return null;
    }

    private static void Execute(SourceProductionContext context, INamedTypeSymbol classSymbol)
    {
        var namespaceName = classSymbol.ContainingNamespace.IsGlobalNamespace ? "" : $"namespace {classSymbol.ContainingNamespace.ToDisplayString()};";
        var properties = classSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Where(p => !p.IsReadOnly && p.DeclaredAccessibility == Accessibility.Public)
            .ToImmutableArray();

        var source = $@"
using System;
using System.Buffers.Binary;
using System.Text;

{namespaceName}

partial class {classSymbol.Name}
{{
    public int GetSize()
    {{
        int size = 0;
        {GenerateSizeCalc(properties)}
        return size;
    }}

    public byte[] Serialize()
    {{
        var buffer = new byte[GetSize()];
        Serialize(buffer);
        return buffer;
    }}

    public void Serialize(Span<byte> dest)
    {{
        int offset = 0;
        {GenerateSerial(properties)}
    }}

    public static {classSymbol.Name} Deserialize(ReadOnlySpan<byte> source)
    {{
        var obj = new {classSymbol.Name}();
        int offset = 0;
        {GenerateDeserial(properties)}
        return obj;
    }}
}}";
        context.AddSource($"{classSymbol.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static string GenerateSizeCalc(ImmutableArray<IPropertySymbol> props) => string.Join("\n        ", props.Select(p => p.Type.SpecialType switch
    {
        SpecialType.System_Int32 => "size += 4;",
        SpecialType.System_String => $"size += 4 + Encoding.UTF8.GetByteCount(this.{p.Name});",
        _ => "size += 0;"
    }));

    private static string GenerateSerial(ImmutableArray<IPropertySymbol> props) => string.Join("\n        ", props.Select(p => p.Type.SpecialType switch
    {
        SpecialType.System_Int32 => $"BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(offset), this.{p.Name}); offset += 4;",
        SpecialType.System_String => $@"var bytes{p.Name} = Encoding.UTF8.GetBytes(this.{p.Name});" +
$"\n        BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(offset), bytes{p.Name}.Length); offset += 4;" +
$"\n        bytes{p.Name}.AsSpan().CopyTo(dest.Slice(offset)); offset += bytes{p.Name}.Length;",
        _ => ""
    }));

    private static string GenerateDeserial(ImmutableArray<IPropertySymbol> props) => string.Join("\n        ", props.Select(p => p.Type.SpecialType switch
    {
        SpecialType.System_Int32 => $"obj.{p.Name} = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(offset)); offset += 4;",
        SpecialType.System_String => $@"int len{p.Name} = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(offset)); offset += 4; obj.{p.Name} = Encoding.UTF8.GetString(source.Slice(offset, len{p.Name})); offset += len{p.Name};",
        _ => ""
    }));
}
