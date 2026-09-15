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
    public static bool IsSame(this EdfPrimitiveType po, Type t)
    {
        switch (po)
        {
            default: throw new NetTypeNotSupportedException(t);
            case EdfPrimitiveType.UInt8: return TypeCode.Byte == Type.GetTypeCode(t);
            case EdfPrimitiveType.Int8: return TypeCode.SByte == Type.GetTypeCode(t);
            case EdfPrimitiveType.UInt16: return TypeCode.UInt16 == Type.GetTypeCode(t);
            case EdfPrimitiveType.Int16: return TypeCode.Int16 == Type.GetTypeCode(t);
            case EdfPrimitiveType.UInt32: return TypeCode.UInt32 == Type.GetTypeCode(t);
            case EdfPrimitiveType.Int32: return TypeCode.Int32 == Type.GetTypeCode(t);
            case EdfPrimitiveType.UInt64: return TypeCode.UInt64 == Type.GetTypeCode(t);
            case EdfPrimitiveType.Int64: return TypeCode.Int64 == Type.GetTypeCode(t);
            case EdfPrimitiveType.Single: return TypeCode.Single == Type.GetTypeCode(t);
            case EdfPrimitiveType.Double: return TypeCode.Double == Type.GetTypeCode(t);
            case EdfPrimitiveType.String: return TypeCode.String == Type.GetTypeCode(t);
            case EdfPrimitiveType.Char: return t.IsArray && t.GetElementType() == typeof(byte);
        }
    }
}
