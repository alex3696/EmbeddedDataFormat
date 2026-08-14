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
        return !IsSupportedPrimitive(type) && type is INamedTypeSymbol;
    }
    public static AttributeData? GetAttribute(this ISymbol symbol, string attribute)
    {
        return symbol.GetAttributes().FirstOrDefault(a =>
            Common.HasAttribute(a.AttributeClass?.Name, attribute)
            || Common.HasAttribute(a.AttributeClass?.ToDisplayString(), attribute));
    }
    public static bool IsSerializable(this ITypeSymbol ntype) => null != GetAttribute(ntype, Common.SerializeAttribute);
    public static AttributeData? GetArrayAttribute(this ISymbol symbol) => GetAttribute(symbol, Common.ArrayAttribute);
    public static AttributeData? GetCharArrayAttribute(this ISymbol symbol) => GetAttribute(symbol, Common.CharArrayAttribute);
    public static bool IsCompatibleType(this ITypeSymbol ntype)
    {
        if (ntype == null) return false;
        ntype = ntype.UnwrapNullable();
        bool hasAttribute = ntype.IsSerializable();
        return hasAttribute || IsSupportedPrimitive(ntype);
    }

    public static string ExtractArrayDimensions(this ISymbol symbol)
    {
        var arrayAttr = symbol.GetArrayAttribute();

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
        if (string.IsNullOrEmpty(s))
            return [];
        return s.Split([", "], StringSplitOptions.None);
    }
    public static byte ExtractCharArrayLength(this ISymbol symbol)
    {
        var attr = symbol.GetCharArrayAttribute();
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
    public static string GetShortTypeName(ITypeSymbol type, string currentNamespace)
    {
        // 1. Формируем имя типа с учетом всей цепочки вложенности (для вложенных классов)
        string typeNameWithNested = type.Name;
        INamedTypeSymbol containingType = type.ContainingType;
        while (containingType != null)
        {
            typeNameWithNested = containingType.Name + "." + typeNameWithNested;
            containingType = containingType.ContainingType;
        }
        // 2. Проверяем пространство имен
        if (type.ContainingNamespace == null || type.ContainingNamespace.IsGlobalNamespace)
            return typeNameWithNested;
        string typeNs = type.ContainingNamespace.ToDisplayString();
        if (typeNs == currentNamespace)
            return typeNameWithNested;
        // 3. Если пространства имен не совпадают, возвращаем полное имя
        return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        //if (type.ContainingNamespace.IsGlobalNamespace)
        //    return type.Name;
        //string typeNs = type.ContainingNamespace.ToDisplayString();
        //if (typeNs == currentNamespace)
        //    return type.Name;
        //return type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
    }
    public static string GetArrayTypeName(IArrayTypeSymbol arrayType, string currentNamespace)
    {
        string elemName = GetShortTypeName(arrayType.ElementType, currentNamespace);
        int rank = arrayType.Rank;
        if (rank == 1) return elemName + "[]";
        return elemName + "[" + new string(',', rank - 1) + "]";
    }
    public static bool IsCharArray(this ISymbol symbol)
    {
        // особый случай свойство или поле byte[] FieldName; с атрибутом [EdfCharArray(len: 54)]
        // - это аналог Си поля фиксированного размера char[54] FieldName в структуре
        if (null != symbol.GetCharArrayAttribute())
        {
            var type = symbol.GetMemberType();
            if (type is IArrayTypeSymbol arrayType && arrayType.ElementType.SpecialType == SpecialType.System_Byte)
                return true;
            throw new NotSupportedException("EdfCharArray attribute can only be applied to byte[] properties or fields.");
        }
        return false;
    }

    public static bool IsReadOnly(this ISymbol symbol)
    {
        return symbol switch
        {
            IPropertySymbol p => p.IsReadOnly,
            IFieldSymbol f => f.IsReadOnly,
            _ => false
        };
    }
    public static bool IsConst(this ISymbol symbol) => symbol is IFieldSymbol f && f.IsConst;
    public static bool IsImplicitlyDeclared(this ISymbol symbol) => symbol is IFieldSymbol f && f.IsImplicitlyDeclared;
    public static bool IsIgnored(this ISymbol symbol) => null != GetAttribute(symbol, Common.IgnoreAttribute);
    public static bool IsSupportedMemberType(this ISymbol symbol)
    {
        if (symbol.IsReadOnly()
            || symbol.IsConst()
            || symbol.IsImplicitlyDeclared()
            || symbol.IsIgnored())
            return false;
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
        if (symbol.IsCharArray())
        {
            return true;
        }
        // 4. Массив с [EdfArray]
        if (null != symbol.GetArrayAttribute())
        {
            var elementType = (type as IArrayTypeSymbol)?.ElementType;
            if (elementType == null)
            {
                var named = type as INamedTypeSymbol;
                elementType = named?.TypeArguments.FirstOrDefault();
            }
            if (elementType != null && (IsSupportedPrimitive(elementType) || elementType.IsSerializable()))
                return true;
            return false;
        }
        // 5. Вложенный сериализуемый тип
        if (type.IsSerializable())
            return true;

        return false;
    }
}
