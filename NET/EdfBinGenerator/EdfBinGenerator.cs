using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace EdfBinGenerator;


[Generator]
public class FastBinaryGenerator : IIncrementalGenerator
{
    public const string SerializeAttribute = "EdfBinSerializableAttribute";
    public const string BinArrayAttribute = "EdfBinArrayAttribute";

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
        context.RegisterPostInitializationOutput(i => i.AddSource($"{SerializeAttribute}.g.cs",
$@"using System;
namespace EdfNet;
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal class {SerializeAttribute} : Attribute {{ }}

[AttributeUsage(AttributeTargets.Property)]
internal class {BinArrayAttribute} : Attribute 
{{ 
    public int[] Dimensions {{ get; set; }}
    public {BinArrayAttribute}(params int[] dimensions) {{ Dimensions = dimensions; }}
}}"));
    }

    private static INamedTypeSymbol? GetSemanticTargetForGeneration(GeneratorSyntaxContext context)
    {
        //var classDeclaration = (ClassDeclarationSyntax)context.Node;
        var classDeclaration = (TypeDeclarationSyntax)context.Node;
        foreach (var attributeList in classDeclaration.AttributeLists)
        {
            foreach (var attribute in attributeList.Attributes)
            {
                // Быстрая текстовая проверка, чтобы зря не нагружать семантическую модель
                string attrName = attribute.Name.ToString();
                if (attrName != "EdfBinSerializable" && attrName != SerializeAttribute)
                    continue;

                if (context.SemanticModel.GetSymbolInfo(attribute).Symbol is IMethodSymbol attributeSymbol &&
                    attributeSymbol.ContainingType.ToDisplayString() == $"EdfNet.{SerializeAttribute}")
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

        var source = $@"using System;
using System.Buffers.Binary;
using System.Text;
using EdfNet;
using EdfNet.Core;

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

    public static int DeserializeBin(ReadOnlySpan<byte> source, out {classSymbol.Name} ret)
    {{
        var obj = new {classSymbol.Name}();
        int offset = 0;
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
        if (prop.Type is IArrayTypeSymbol arrayType)
        {
            var dims = GetPropertyDimensions(prop);
            if (dims != null && dims.Length == arrayType.Rank)
            {
                int elementSize = TypeSymbolUtils.GetPrimitiveSize(arrayType.ElementType);
                int totalElements = dims.Aggregate(1, (acc, val) => acc * val);
                sb.AppendLine($"{Tab(2)}size += {totalElements * elementSize};");
            }
            return; // Любые другие массивы без атрибута просто пропускаем
        }
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
        if (prop.Type is IArrayTypeSymbol)
        {
            GenerateArrayPropertyWrite(sb, prop);
            return;
        }
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
                sb.AppendLine($"{Tab(2)}{{");
                sb.AppendLine($"{Tab(3)}var len=EdfBinString.WriteBin({pname}, dest.Slice(offset));");
                sb.AppendLine($"{Tab(3)}offset += 0 < len? len: 0;");
                sb.AppendLine($"{Tab(2)}}}");
                return;
        }
        if (IsSerializable(prop.Type))
            sb.AppendLine($"{Tab(2)}offset += {pname}.SerializeBin(dest.Slice(offset));");
    }
    private static void GenerateArrayPropertyWrite(StringBuilder sb, IPropertySymbol prop)
    {
        if (prop.Type is not IArrayTypeSymbol arrayWriteType)
            return;

        var dims = GetPropertyDimensions(prop);
        int rank = arrayWriteType.Rank;

        // Если атрибута [EdfBinArray] нет или размерность не совпадает с рангом массива — игнорируем
        if (dims == null || dims.Length != rank)
            return;

        string pname = $"{prop.Name}";
        var elementType = arrayWriteType.ElementType;
        int elementSize = TypeSymbolUtils.GetPrimitiveSize(elementType);

        int totalElements = dims.Aggregate(1, (acc, val) => acc * val);
        int totalBytes = totalElements * elementSize; // Строгий размер в байтах (например, 12)
        string dimsStr = string.Join(", ", dims);

        sb.AppendLine($"{Tab(2)}{{");
        // 1. Вырезаем целевой срез из буфера назначения
        sb.AppendLine($"{Tab(3)}var dstSlice = dest.Slice(offset, {totalBytes});");
        sb.AppendLine($"{Tab(3)}offset += {totalBytes};");

        sb.AppendLine($"{Tab(3)}if ({pname} == null)");
        sb.AppendLine($"{Tab(3)}{'{'}{Tab(3)}dstSlice.Clear();{'}'}"); // Если null — пишем чистые нули
        sb.AppendLine($"{Tab(3)}else");
        sb.AppendLine($"{Tab(3)}{{");

        // 2. Создаем временный массив идеального размера-шаблона (гарантирует нули в пустых ячейках)
        sb.AppendLine($"{Tab(4)}var tempArr = new {elementType.ToDisplayString()}[{dimsStr}];");

        // 3. Вычисляем безопасный лимит байт для копирования (чтобы не выйти за границы меньшего массива)
        sb.AppendLine($"{Tab(4)}int copyBytes = Math.Min({pname}.Length * {elementSize}, {totalBytes});");

        // 4. Получаем ref byte ссылки на источник и на наш временный буфер-шаблон
        sb.AppendLine($"{Tab(4)}ref byte srcRef = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference({pname});");
        sb.AppendLine($"{Tab(4)}ref byte tempRef = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(tempArr);");

        // 5. Создаем байтовые Span для копирования
        sb.AppendLine($"{Tab(4)}ReadOnlySpan<byte> srcSpan = System.Runtime.InteropServices.MemoryMarshal.CreateReadOnlySpan(ref srcRef, copyBytes);");
        sb.AppendLine($"{Tab(4)}Span<byte> tempSpan = System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref tempRef, {totalBytes});");

        // 6. Переносим данные в шаблон (лишнее обнулено при создании) и сбрасываем всё одной командой в целевой slice
        sb.AppendLine($"{Tab(4)}srcSpan.CopyTo(tempSpan);");
        sb.AppendLine($"{Tab(4)}tempSpan.CopyTo(dstSlice);");

        sb.AppendLine($"{Tab(3)}}}");
        sb.AppendLine($"{Tab(2)}}}");
    }



    private static int[]? GetPropertyDimensions(IPropertySymbol prop)
    {
        var attr = prop.GetAttributes().FirstOrDefault(a =>
            a.AttributeClass?.Name == BinArrayAttribute ||
            a.AttributeClass?.Name == "EdfBinArray");

        if (attr == null) return null;

        // Извлекаем массив из аргументов конструктора
        if (!attr.ConstructorArguments.IsEmpty)
        {
            var arg = attr.ConstructorArguments[0];
            if (arg.Kind == TypedConstantKind.Array)
            {
                return arg.Values.Select(v => (int)(v.Value ?? 0)).ToArray();
            }
        }
        return null;
    }
    private static bool IsSerializable(ITypeSymbol type) =>
        type.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == $"EdfNet.{SerializeAttribute}");

    private static string GenerateDeserial(ImmutableArray<IPropertySymbol> props)
    {
        var sb = new StringBuilder();
        foreach (var prop in props)
            GeneratePropertyRead(sb, prop);
        return sb.ToString();
    }
    private static void GeneratePropertyRead(StringBuilder sb, IPropertySymbol prop)
    {
        if (prop.Type is IArrayTypeSymbol)
        {
            GenerateArrayPropertyRead(sb, prop);
            return;
        }
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
                sb.AppendLine($"{Tab(2)}{{");
                sb.AppendLine($"{Tab(3)}var len=EdfBinString.ReadBin(source.Slice(offset), out var str);");
                sb.AppendLine($"{Tab(3)}if(0 < len)");
                sb.AppendLine($"{Tab(3)}{{");
                sb.AppendLine($"{Tab(4)}{pname} = str;");
                sb.AppendLine($"{Tab(4)}offset += len;");
                sb.AppendLine($"{Tab(3)}}}");
                sb.AppendLine($"{Tab(2)}}}");
                return;
        }
        if (IsSerializable(prop.Type))
        {
            // Используем FullName (ToDisplayString), чтобы избежать проблем с пространствами имен
            string typeName = prop.Type.ToDisplayString();
            sb.AppendLine($"{Tab(2)}len = {typeName}.DeserializeBin(source.Slice(offset), out var obj{prop.Name});");
            sb.AppendLine($"{Tab(2)}if(0 < len && obj{prop.Name} is not null)");
            sb.AppendLine($"{Tab(2)}{{");
            // Присваиваем напрямую, без .Value, так как метод возвращает сам объект или структуру
            sb.AppendLine($"{Tab(3)}{pname} = obj{prop.Name};");
            sb.AppendLine($"{Tab(3)}offset += len;");
            sb.AppendLine($"{Tab(2)}}}");
        }
    }
    private static void GenerateArrayPropertyRead(StringBuilder sb, IPropertySymbol prop)
    {
        if (prop.Type is not IArrayTypeSymbol arrayReadType)
            return;

        var dims = GetPropertyDimensions(prop);
        int rank = arrayReadType.Rank;

        // Если атрибута [EdfBinArray] нет или размерность не совпадает с рангом — игнорируем
        if (dims == null || dims.Length != rank)
            return;

        string pname = $"obj.{prop.Name}";
        var elementType = arrayReadType.ElementType;
        string elementTypeName = elementType.ToDisplayString();
        int elementSize = TypeSymbolUtils.GetPrimitiveSize(elementType);

        int totalElements = dims.Aggregate(1, (acc, val) => acc * val);
        int totalBytes = totalElements * elementSize; // Честный размер в байтах (например, 12)
        string dimsStr = string.Join(", ", dims);

        sb.AppendLine($"{Tab(2)}{{");
        // 1. Вырезаем срез байт из источника
        sb.AppendLine($"{Tab(3)}var srcSlice = source.Slice(offset, {totalBytes});");
        sb.AppendLine($"{Tab(3)}offset += {totalBytes};");

        // 2. Создаем массив нужных размеров (например, new short[2, 3])
        sb.AppendLine($"{Tab(3)}var arr = new {elementTypeName}[{dimsStr}];");

        // 3. Получаем ссылку на первый байт массива (всегда ref byte)
        sb.AppendLine($"{Tab(3)}ref byte dstRef = ref System.Runtime.InteropServices.MemoryMarshal.GetArrayDataReference(arr);");

        // 4. Создаем байтовый Span на основе размера в байтах
        sb.AppendLine($"{Tab(3)}Span<byte> dstSpan = System.Runtime.InteropServices.MemoryMarshal.CreateSpan(ref dstRef, {totalBytes});");

        // 5. Копируем память напрямую и записываем в объект
        sb.AppendLine($"{Tab(3)}srcSlice.CopyTo(dstSpan);");
        sb.AppendLine($"{Tab(3)}{pname} = arr;");
        sb.AppendLine($"{Tab(2)}}}");
    }



}
