namespace NetEdf.src;

//[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Size = 16, Pack = 1)]

// Code Page Identifiers
// https://learn.microsoft.com/en-us/windows/win32/intl/code-page-identifiers

[Flags]
public enum Options : uint
{
    Default = 0,
    UseCrc = 1,
    MaskUseCrc = 0xFE,
};

public class Header : IEquatable<Header>
{
    public byte VersMajor = 0x01;
    public byte VersMinor = 0x00;
    public UInt16 Encoding = 65001;
    public UInt16 Blocksize = 256;
    public Options Flags = Options.Default | Options.UseCrc;
    public byte[]? Reserved;

    public Header()
    {
        //Name = "bdf ".ToCharArray();
    }

    public static readonly Header Default = new();

    public static Header Parse(ReadOnlySpan<byte> b)
    {
        if (16 <= b.Length)
        {
            return new Header()
            {
                VersMajor = b[0],
                VersMinor = b[1],
                Encoding = BinaryPrimitives.ReadUInt16LittleEndian(b.Slice(2, sizeof(UInt16))),
                Blocksize = BinaryPrimitives.ReadUInt16LittleEndian(b.Slice(4, sizeof(UInt16))),
                Flags = (Options)BinaryPrimitives.ReadUInt32LittleEndian(b.Slice(6, sizeof(UInt32))),
            };
        }
        throw new ArgumentException($"array is not Header");
    }

    public bool Equals(Header? other)
    {
        if (null == other)
            return false;
        if (VersMajor != other.VersMajor) return false;
        if (VersMinor != other.VersMinor) return false;
        if (Encoding != other.Encoding) return false;
        if (Blocksize != other.Blocksize) return false;
        if (Flags != other.Flags) return false;
        return true;
    }
    public override bool Equals(object? obj) => obj is Header header && Equals(header);
    public override int GetHashCode() => throw new NotImplementedException();
}
