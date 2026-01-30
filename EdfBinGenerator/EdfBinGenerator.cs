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
    public const string SerializeAttribute = "EdfBinSerializableAttribute";

    public static string Tab(byte n)
    {
        StringBuilder tabs = new(0xFF);
        for (int i = 0; i < n; i++)
            tabs.Append('\t');
        return tabs.ToString();
    }

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        // 1. Поиск классов с атрибутом [BinarySerializable]
        var classDeclarations = context.SyntaxProvider
            .CreateSyntaxProvider(
                //predicate: static (s, _) => s is ClassDeclarationSyntax { AttributeLists.Count: > 0 },
                predicate: static (s, _) => s is TypeDeclarationSyntax { AttributeLists.Count: > 0 },
                transform: static (ctx, _) => GetSemanticTargetForGeneration(ctx))
            .Where(static m => m is not null);

        // 2. Сборка данных для генерации (компиляция + список классов)
        IncrementalValuesProvider<INamedTypeSymbol> compilationAndClasses = classDeclarations.Select(static (s, _) => s!);

        // 3. Регистрация самого источника кода
        context.RegisterSourceOutput(compilationAndClasses, static (spc, source) => Execute(spc, source));

        // Добавляем сам атрибут в проект
        context.RegisterPostInitializationOutput(i => i.AddSource($"{SerializeAttribute}.g.cs", $@"
using System;
namespace NetEdf;
[AttributeUsage(AttributeTargets.Class|AttributeTargets.Struct)]
internal class {SerializeAttribute} : Attribute {{ }}"));
    }

    private static INamedTypeSymbol? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        //var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classDeclaration = (TypeDeclarationSyntax)context.Node;
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol &&
                    attributeSymbol.ContainingType.ToDisplayString() == $"NetEdf.{SerializeAttribute}")
                {
                    return context.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
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
using System.Buffers.Binary;
using System.Text;
using NetEdf;

{namespaceName}

partial {strOrCls} {classSymbol.Name}
{{
    public int GetSize()
    {{
        int size = 0;
{GenerateSizeCalc(properties)}
        return size;
    }}

    public int SerializeBin(Span<byte> dest)
    {{
        int offset = 0;
{GenerateSerial(properties)}
        return offset;
    }}

    public static int DeserializeBin(ReadOnlySpan<byte> source, out {classSymbol.Name}? ret)
    {{
        var obj = new {classSymbol.Name}();
        int offset = 0;
        int len = 0;
{GenerateDeserial(properties)}
        ret = obj;
        return offset;
    }}
}}";
        context.AddSource($"{classSymbol.Name}.g.cs", SourceText.From(source, Encoding.UTF8));
    }

    private static string GenerateSizeCalc(ImmutableArray<IPropertySymbol> props)
    {
        var sb = new StringBuilder();
        foreach (var prop in props)
            GeneratePropSizeOf(sb, prop);
        return sb.ToString();
    }
    private static void GeneratePropSizeOf(StringBuilder sb, IPropertySymbol prop)
    {
        switch (prop.Type.SpecialType)
        {
            default: break;
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
                sb.AppendLine($"{Tab(2)}size += 1;");
                return;
            case SpecialType.System_UInt16:
            case SpecialType.System_Int16:
                sb.AppendLine($"{Tab(2)}size += 2;");
                return;
            case SpecialType.System_UInt32:
            case SpecialType.System_Int32:
            case SpecialType.System_Single:
                sb.AppendLine($"{Tab(2)}size += 4;");
                return;
            case SpecialType.System_UInt64:
            case SpecialType.System_Int64:
            case SpecialType.System_Double:
                sb.AppendLine($"{Tab(2)}size += 8;");
                return;
            case SpecialType.System_String:
                sb.AppendLine($"{Tab(2)}size += EdfBinString.SizeOf({prop.Name});");
                return;
        }
        if (IsSerializable(prop.Type))
            sb.AppendLine($"{Tab(2)}size += {prop.Name}.GetSize();");

    }
    private static string GenerateSerial(ImmutableArray<IPropertySymbol> props)
    {
        var sb = new StringBuilder();
        foreach (var prop in props)
            GeneratePropertyWrite(sb, prop);
        return sb.ToString();
    }

    private static void GeneratePropertyWrite(StringBuilder sb, IPropertySymbol prop)
    {
        string pname = $"{prop.Name}";
        switch (prop.Type.SpecialType)
        {
            default: break;
            case SpecialType.System_Byte:
                sb.Append($"{Tab(2)}dest[offset] = {pname};");
                sb.AppendLine($" offset += 1;");
                return;
            case SpecialType.System_SByte:
                sb.Append($"{Tab(2)}dest[offset] = (byte){pname};");
                sb.AppendLine($" offset += 1;");
                return;
            case SpecialType.System_UInt16:
                sb.Append($"{Tab(2)}BinaryPrimitives.WriteUInt16LittleEndian(dest.Slice(offset), {pname});");
                sb.AppendLine($" offset += 2;");
                return;
            case SpecialType.System_Int16:
                sb.Append($"{Tab(2)}BinaryPrimitives.WriteInt16LittleEndian(dest.Slice(offset), {pname});");
                sb.AppendLine($" offset += 2;");
                return;
            case SpecialType.System_UInt32:
                sb.Append($"{Tab(2)}BinaryPrimitives.WriteUInt32LittleEndian(dest.Slice(offset), {pname});");
                sb.AppendLine($" offset += 4;");
                return;
            case SpecialType.System_Int32:
                sb.Append($"{Tab(2)}BinaryPrimitives.WriteInt32LittleEndian(dest.Slice(offset), {pname});");
                sb.AppendLine($" offset += 4;");
                return;
            case SpecialType.System_UInt64:
                sb.Append($"{Tab(2)}BinaryPrimitives.WriteUInt64LittleEndian(dest.Slice(offset), {pname});");
                sb.AppendLine($" offset += 8;");
                return;
            case SpecialType.System_Int64:
                sb.Append($"{Tab(2)}BinaryPrimitives.WriteInt64LittleEndian(dest.Slice(offset), {pname});");
                sb.AppendLine($" offset += 8;");
                return;
            case SpecialType.System_Single:
                sb.Append($"{Tab(2)}BinaryPrimitives.WriteSingleLittleEndian(dest.Slice(offset), {pname});");
                sb.AppendLine($" offset += 4;");
                return;
            case SpecialType.System_Double:
                sb.Append($"{Tab(2)}BinaryPrimitives.WriteDoubleLittleEndian(dest.Slice(offset), {pname});");
                sb.AppendLine($" offset += 8;");
                return;
            case SpecialType.System_String:
                sb.AppendLine($"{Tab(2)}var len=EdfBinString.WriteBin({pname}, dest.Slice(offset));");
                sb.AppendLine($"{Tab(2)}offset += 0 < len? len: 0;");
                return;
        }
        if (IsSerializable(prop.Type))
            sb.AppendLine($"{Tab(2)}offset += {pname}.SerializeBin(dest.Slice(offset));");
    }
    private static bool IsSerializable(ITypeSymbol type) =>
        type.GetAttributes().Any(a => a.AttributeClass?.Name == SerializeAttribute);

    private static string GenerateDeserial(ImmutableArray<IPropertySymbol> props)
    {
        var sb = new StringBuilder();
        foreach (var prop in props)
            GeneratePropertyRead(sb, prop);
        return sb.ToString();
    }
    private static void GeneratePropertyRead(StringBuilder sb, IPropertySymbol prop)
    {
        string pname = $"obj.{prop.Name}";
        switch (prop.Type.SpecialType)
        {
            default: break;
            case SpecialType.System_Byte:
                sb.Append($"{Tab(2)}{pname} = source[offset];");
                sb.AppendLine($" offset += 1;");
                return;
            case SpecialType.System_SByte:
                sb.Append($"{Tab(2)}{pname} = (sbyte)source[offset];");
                sb.AppendLine($" offset += 1;");
                return;
            case SpecialType.System_UInt16:
                sb.Append($"{Tab(2)}{pname} = BinaryPrimitives.ReadUInt16LittleEndian(source.Slice(offset));");
                sb.AppendLine($" offset += 2;");
                return;
            case SpecialType.System_Int16:
                sb.Append($"{Tab(2)}{pname} = BinaryPrimitives.ReadInt16LittleEndian(source.Slice(offset));");
                sb.AppendLine($" offset += 2;");
                return;
            case SpecialType.System_UInt32:
                sb.Append($"{Tab(2)}{pname} = BinaryPrimitives.ReadUInt32LittleEndian(source.Slice(offset));");
                sb.AppendLine($" offset += 4;");
                return;
            case SpecialType.System_Int32:
                sb.Append($"{Tab(2)}{pname} = BinaryPrimitives.ReadInt32LittleEndian(source.Slice(offset));");
                sb.AppendLine($" offset += 4;");
                return;
            case SpecialType.System_UInt64:
                sb.Append($"{Tab(2)}{pname} = BinaryPrimitives.ReadUInt64LittleEndian(source.Slice(offset));");
                sb.AppendLine($" offset += 8;");
                return;
            case SpecialType.System_Int64:
                sb.Append($"{Tab(2)}{pname} = BinaryPrimitives.ReadInt64LittleEndian(source.Slice(offset));");
                sb.AppendLine($" offset += 8;");
                return;
            case SpecialType.System_Single:
                sb.Append($"{Tab(2)}{pname} = BinaryPrimitives.ReadSingleLittleEndian(source.Slice(offset));");
                sb.AppendLine($" offset += 4;");
                return;
            case SpecialType.System_Double:
                sb.Append($"{Tab(2)}{pname} = BinaryPrimitives.ReadDoubleLittleEndian(source.Slice(offset));");
                sb.AppendLine($" offset += 8;");
                return;
            case SpecialType.System_String:
                sb.AppendLine($"{Tab(2)}len=EdfBinString.ReadBin(source.Slice(offset), out var str);");
                sb.AppendLine($"{Tab(2)}if(0 < len)");
                sb.AppendLine($"{Tab(2)}{{");
                sb.AppendLine($"{Tab(3)}{pname} = str;");
                sb.AppendLine($"{Tab(3)}offset += len;");
                sb.AppendLine($"{Tab(2)}}}");
                return;
        }
        if (IsSerializable(prop.Type))
        {
            sb.AppendLine($"{Tab(2)}len = {prop.Type.Name}.DeserializeBin(source.Slice(offset), out var obj{prop.Name});");
            sb.AppendLine($"{Tab(2)}if(0 < len && obj{prop.Name} is not null)");
            sb.AppendLine($"{Tab(2)}{{");
            sb.AppendLine($"{Tab(3)}{pname} = obj{prop.Name}.Value;");
            sb.AppendLine($"{Tab(3)}offset += len;");
            sb.AppendLine($"{Tab(2)}}}");
        }
    }
}
