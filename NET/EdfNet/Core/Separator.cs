namespace EdfNet.Core;

public static class Separator
{
    public static readonly byte[] BeginStruct = "{"u8.ToArray();
    public static readonly byte[] EndStruct = "}"u8.ToArray();
    public static readonly byte[] BeginArray = "["u8.ToArray();
    public static readonly byte[] EndArray = "]"u8.ToArray();
    public static readonly byte[] VarEnd = ";"u8.ToArray();
    public static readonly byte[] RecBegin = "\n<= "u8.ToArray();
    public static readonly byte[] RecEnd = ">"u8.ToArray();
}
