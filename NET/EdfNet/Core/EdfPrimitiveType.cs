namespace EdfNet.Core;

/// <summary>
///  Plain object type
/// </summary>
public enum EdfPrimitiveType : byte
{
    Struct = 0,
    Char,
    // Integers
    Int8,
    UInt8,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    // float
    Half,
    Single,
    Double,
    // strings
    String,
}
public static class PoTypeExt
{
    public static bool IsPoType(this EdfPrimitiveType p)
    {
        return Enum.IsDefined(p);
    }
    public static byte GetSizeOf(this EdfPrimitiveType p)
    {
        return p switch
        {
            EdfPrimitiveType.UInt8 or EdfPrimitiveType.Int8 or EdfPrimitiveType.Char => 1,
            EdfPrimitiveType.UInt16 or EdfPrimitiveType.Int16 or EdfPrimitiveType.Half => 2,
            EdfPrimitiveType.UInt32 or EdfPrimitiveType.Int32 or EdfPrimitiveType.Single => 4,
            EdfPrimitiveType.UInt64 or EdfPrimitiveType.Int64 or EdfPrimitiveType.Double => 8,
            EdfPrimitiveType.String => 1,// minimum string length
            _ => 0,
        };
    }
    public static EdfPrimitiveType GetPoType(this Type t)
    {
        var typeCode = Type.GetTypeCode(t);
        return typeCode switch
        {
            TypeCode.Byte => EdfPrimitiveType.UInt8,
            TypeCode.SByte => EdfPrimitiveType.Int8,
            TypeCode.Int16 => EdfPrimitiveType.Int16,
            TypeCode.UInt16 => EdfPrimitiveType.UInt16,
            TypeCode.Int32 => EdfPrimitiveType.Int32,
            TypeCode.UInt32 => EdfPrimitiveType.UInt32,
            TypeCode.Int64 => EdfPrimitiveType.Int64,
            TypeCode.UInt64 => EdfPrimitiveType.UInt64,
            TypeCode.Single => EdfPrimitiveType.Single,
            TypeCode.Double => EdfPrimitiveType.Double,
            TypeCode.String => EdfPrimitiveType.String,
            _ => throw new ArgumentOutOfRangeException(nameof(t), t, "Unsupported type"),
        };
    }
}
