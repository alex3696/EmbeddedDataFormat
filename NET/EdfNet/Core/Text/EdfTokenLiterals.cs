namespace EdfNet.Core.Text;

public static class EdfTokenLiterals
{
    public static ReadOnlySpan<byte> Zero => "\0"u8;
    public static ReadOnlySpan<byte> Quote => "\""u8;
    public static ReadOnlySpan<byte> Space => " "u8;
    public static ReadOnlySpan<byte> EndLine => "\n"u8;
    public static ReadOnlySpan<byte> ConfigBegin => "<~"u8;
    public static ReadOnlySpan<byte> SchemaBegin => "<?"u8;
    public static ReadOnlySpan<byte> RecBegin => "<="u8;
    public static ReadOnlySpan<byte> BlockEnd => ">"u8;
    public static ReadOnlySpan<byte> StructBegin => "{"u8;
    public static ReadOnlySpan<byte> StructEnd => "}"u8;
    public static ReadOnlySpan<byte> ArrayBegin => "["u8;
    public static ReadOnlySpan<byte> ArrayEnd => "]"u8;
    public static ReadOnlySpan<byte> VarEnd => ";"u8;
}
