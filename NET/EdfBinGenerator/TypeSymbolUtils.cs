using Microsoft.CodeAnalysis;
using System.Linq;

namespace EdfBinGenerator;

public static class TypeSymbolUtils
{
    public static int GetPrimitiveSize(this ITypeSymbol type)
    {
        return type.SpecialType switch
        {
            SpecialType.System_Byte => 1,
            SpecialType.System_SByte => 1,
            SpecialType.System_Int16 => 2,
            SpecialType.System_UInt16 => 2,
            SpecialType.System_Int32 => 4,
            SpecialType.System_UInt32 => 4,
            SpecialType.System_Single => 4,
            SpecialType.System_Int64 => 8,
            SpecialType.System_UInt64 => 8,
            SpecialType.System_Double => 8,
            _ => 0
        };
    }
    public static string MapToPoType(this ITypeSymbol typeSymbol)
    {
        return typeSymbol.SpecialType switch
        {
            SpecialType.System_Byte => "UInt8",
            SpecialType.System_SByte => "Int8",
            SpecialType.System_Int16 => "Int16",
            SpecialType.System_UInt16 => "UInt16",
            SpecialType.System_Int32 => "Int32",
            SpecialType.System_UInt32 => "UInt32",
            SpecialType.System_Double => "Double",
            SpecialType.System_Single => "Single",
            SpecialType.System_Int64 => "Int64",
            SpecialType.System_UInt64 => "UInt64",
            SpecialType.System_String => "String",
            _ => string.Empty // Сюда можно добавить обработку других типов (Char, Int8 и т.д.)
        };
    }
    // --- Вспомогательные методы-утилиты для проверки типов в EdfEnumeratorGenerator ---

    public static ITypeSymbol UnwrapNullable(this ITypeSymbol type)
    {
        if (type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T && type is INamedTypeSymbol namedType && namedType.TypeArguments.Length > 0)
            return namedType.TypeArguments[0];
        return type;
    }

    public static bool IsNullableValueType(this ITypeSymbol type)
    {
        return type.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T;
    }

    public static bool IsNestedSerializable(this ITypeSymbol type)
    {
        // Используем ваше новое расширение TypeSymbolUtils.MapToPoType()
        // Если это не примитив, но у типа есть имя — значит, это вложенный объект (класс или структура)
        return string.IsNullOrEmpty(type.MapToPoType()) && type is INamedTypeSymbol;
    }


    public const string Attribute = "Attribute";
    public const string Namespace = "EdfNet";

    public static bool IsAttribute(this string? str, string attribute)
    {
        return str != null && (str.Contains(attribute) || str.Contains(attribute.Replace(Attribute, string.Empty)));
    }
    public static bool HasAttribute(this ITypeSymbol ntype, string attribute)
    {
        return ntype.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == $"{Namespace}.{attribute}");
    }

    /// <summary>
    /// Вспомогательный метод для извлечения размерностей массива из атрибута [EdfArray]
    /// </summary>
    public static string ExtractArrayDimensions(this IPropertySymbol property)
    {
        var arrayAttr = property.GetAttributes().FirstOrDefault(a =>
            IsAttribute(a.AttributeClass?.Name, EdfEnumeratorGenerator.ArrayAttribute));

        if (arrayAttr != null && arrayAttr.ConstructorArguments.Length > 0)
        {
            var arg = arrayAttr.ConstructorArguments[0];
            if (arg.Kind == TypedConstantKind.Array)
            {
                var values = arg.Values.Select(v => v.Value?.ToString()).ToArray();
                return string.Join(", ", values); // Вернет строку "3, 2, 1"
            }
        }
        return string.Empty;
    }
}
