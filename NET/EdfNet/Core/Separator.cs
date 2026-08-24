namespace EdfNet.Core;

public static class Separator
{
    public static readonly byte[] EndLine = "\n"u8.ToArray();
    public static readonly byte[] ConfigBegin = "<~"u8.ToArray();
    public static readonly byte[] SchemaBegin = "<?"u8.ToArray();
    public static readonly byte[] RecBegin = "\n<= "u8.ToArray();
    public static readonly byte[] RecEnd = ">"u8.ToArray();
    public static readonly byte[] StructBegin = "{"u8.ToArray();
    public static readonly byte[] StructEnd = "}"u8.ToArray();
    public static readonly byte[] ArrayBegin = "["u8.ToArray();
    public static readonly byte[] ArrayEnd = "]"u8.ToArray();
    public static readonly byte[] VarEnd = ";"u8.ToArray();
}
