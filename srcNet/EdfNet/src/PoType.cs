namespace NetEdf.src;

/// <summary>
///  Plain object type
/// </summary>
public enum PoType : byte
{
    Struct = 0,
    Char,
    // integres
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
    public static bool IsPoType(this PoType p)
    {
        return Enum.IsDefined(p);
    }
    public static byte GetSizeOf(this PoType p)
    {
        return p switch
        {
            PoType.UInt8 or PoType.Int8 or PoType.Char => 1,
            PoType.UInt16 or PoType.Int16 or PoType.Half => 2,
            PoType.UInt32 or PoType.Int32 or PoType.Single => 4,
            PoType.UInt64 or PoType.Int64 or PoType.Double => 8,
            PoType.String => 1,// minimum string length
            _ => 0,
        };
    }

}
