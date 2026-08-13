using Microsoft.CodeAnalysis;
using System;
using System.Linq;

namespace EdfGenerator;

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
            _ => string.Empty
        };
    }

    public static ITypeSymbol? GetMemberType(this ISymbol symbol)
    {
        return symbol switch
        {
            IPropertySymbol p => p.Type,
            IFieldSymbol f => f.Type,
            _ => null
        };
    }

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
        return string.IsNullOrEmpty(type.MapToPoType()) && type is INamedTypeSymbol;
    }

    public static bool HasAttribute(this ITypeSymbol ntype, string attribute)
    {
        return ntype.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == $"{Common.Namespace}.{attribute}" ||
            a.AttributeClass?.Name == attribute);
    }

    public static bool HasAttribute(this ISymbol symbol, string attribute)
    {
        return symbol.GetAttributes().Any(a => Common.IsAttribute(a.AttributeClass?.Name, attribute));
    }

    public static bool IsCompatibleType(this ITypeSymbol ntype)
    {
        if (ntype == null) return false;
        ntype = ntype.UnwrapNullable();
        bool hasAttribute = ntype.HasAttribute(Common.SerializeAttribute)
            || ntype.HasAttribute(Common.ArrayAttribute);
        return hasAttribute || !string.IsNullOrWhiteSpace(TypeSymbolUtils.MapToPoType(ntype));
    }

    public static string ExtractArrayDimensions(this ISymbol symbol)
    {
        var arrayAttr = symbol.GetAttributes().FirstOrDefault(a =>
            Common.IsAttribute(a.AttributeClass?.Name, Common.ArrayAttribute));

        if (arrayAttr != null && arrayAttr.ConstructorArguments.Length > 0)
        {
            var arg = arrayAttr.ConstructorArguments[0];
            if (arg.Kind == TypedConstantKind.Array)
            {
                var values = arg.Values.Select(v => v.Value?.ToString()).ToArray();
                return string.Join(", ", values);
            }
        }
        return string.Empty;
    }

    public static string[] ExtractArrayDimensionValues(this ISymbol symbol)
    {
        var s = symbol.ExtractArrayDimensions();
        if (string.IsNullOrEmpty(s)) return Array.Empty<string>();
        return s.Split(new[] { ", " }, StringSplitOptions.None);
    }

    public static byte ExtractCharArrayLength(this ISymbol symbol)
    {
        var attr = symbol.GetAttributes().FirstOrDefault(a =>
            Common.IsAttribute(a.AttributeClass?.Name, "EdfCharArrayAttribute"));
        if (attr != null && attr.ConstructorArguments.Length > 0)
        {
            var val = attr.ConstructorArguments[0].Value;
            if (val is byte b) return b;
        }
        return 0;
    }

    public static bool IsSupportedPrimitive(ITypeSymbol type)
    {
        switch (type.SpecialType)
        {
            default:
                return false;
            case SpecialType.System_Byte:
            case SpecialType.System_SByte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
            case SpecialType.System_String:
                return true;
        }
    }

    public static void CheckSupportedPrimitive(ITypeSymbol type)
    {
        if (!IsSupportedPrimitive(type))
            throw new InvalidOperationException($"Type {type.ToDisplayString()} is not a supported primitive type.");
    }

    public static bool IsIgnored(this ISymbol symbol)
    {
        return symbol.GetAttributes().Any(a =>
            a.AttributeClass?.ToDisplayString() == $"{Common.Namespace}.EdfIgnoreAttribute" ||
            a.AttributeClass?.Name == "EdfIgnoreAttribute");
    }

    public static string GetShortTypeName(ITypeSymbol type, string currentNamespace)
    {
        if (type.ContainingNamespace.IsGlobalNamespace)
            return type.Name;
        string typeNs = type.ContainingNamespace.ToDisplayString();
        if (typeNs == currentNamespace)
            return type.Name;
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
    public static string GetArrayTypeName(IArrayTypeSymbol arrayType, string currentNamespace)
    {
        string elemName = TypeSymbolUtils.GetShortTypeName(arrayType.ElementType, currentNamespace);
        int rank = arrayType.Rank;
        if (rank == 1) return elemName + "[]";
        return elemName + "[" + new string(',', rank - 1) + "]";
    }

    public static bool IsByteArray(this ISymbol symbol)
    {
        // особый случай свойство или поле byte[] FieldName; с атрибутом [EdfCharArray(len: 54)]
        // - это аналог Си поля фиксированного размера char[54] FieldName в структуре
        if (symbol.HasAttribute(Common.CharArrayAttribute))
        {
            var type = symbol.GetMemberType();
            if (type is IArrayTypeSymbol arrayType && arrayType.ElementType.SpecialType == SpecialType.System_Byte)
                return true;
            throw new NotSupportedException("EdfCharArray attribute can only be applied to byte[] properties or fields.");
        }
        return false;
    }

    public static bool IsSupportedMemberType(this ISymbol symbol)
    {
        var type = symbol.GetMemberType();
        if (type == null) return false;

        // 1. Примитив
        if (IsSupportedPrimitive(type))
            return true;

        // 2. Nullable примитив
        if (type.IsNullableValueType())
        {
            var underlying = type.UnwrapNullable();
            if (IsSupportedPrimitive(underlying))
                return true;
        }

        // 3. byte[] с [EdfCharArray]
        if (symbol.HasAttribute(Common.CharArrayAttribute))
        {
            if (type is IArrayTypeSymbol arr && arr.ElementType.SpecialType == SpecialType.System_Byte)
                return true;
            return false;
        }

        // 4. Массив с [EdfArray]
        if (symbol.HasAttribute(Common.ArrayAttribute))
        {
            var elementType = (type as IArrayTypeSymbol)?.ElementType;
            if (elementType == null)
            {
                var named = type as INamedTypeSymbol;
                elementType = named?.TypeArguments.FirstOrDefault();
            }
            if (elementType != null && (IsSupportedPrimitive(elementType) || elementType.HasAttribute(Common.SerializeAttribute)))
                return true;
            return false;
        }

        // 5. Вложенный сериализуемый тип
        if (type.HasAttribute(Common.SerializeAttribute))
            return true;

        return false;
    }
}
