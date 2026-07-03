using Microsoft.CodeAnalysis;

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
}
