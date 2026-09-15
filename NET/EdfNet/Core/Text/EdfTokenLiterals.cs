namespace EdfNet.Core.Text;

public static class EdfTokenLiterals
{
    public const byte Zero = 0;
    public const byte Quote = (byte)'"';
    public const byte Space = (byte)' ';
    public const byte EndLine = (byte)'\n';
    public static ReadOnlySpan<byte> ConfigBegin => "<~"u8;
    public static ReadOnlySpan<byte> SchemaBegin => "<?"u8;
    public static ReadOnlySpan<byte> RecBegin => "<="u8;
    public const byte BlockEnd = (byte)'>';
    public const byte StructBegin = (byte)'{';
    public const byte StructEnd = (byte)'}';
    public const byte ArrayBegin = (byte)'[';
    public const byte ArrayEnd = (byte)']';
    public const byte VarEnd = (byte)';';
}
