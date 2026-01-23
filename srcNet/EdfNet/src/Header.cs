namespace NetEdf.src;

//[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Size = 16, Pack = 1)]

// Code Page Identifiers
// https://learn.microsoft.com/en-us/windows/win32/intl/code-page-identifiers

[Flags]
public enum Options : UInt32
{
    Default = 0,
    UseCrc = 1,

    MaskUseCrc = 0xFE,
};

public class Header : IEquatable<Header>
{
    public byte VersMajor = 0x01;
    public byte VersMinor = 0x02;
    public byte VersPatch = 0x03;
    public UInt16 Encoding = 65001;
    public UInt16 Blocksize = 256;
    public Options Flags = Options.Default;
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
                VersPatch = b[2],
                Encoding = BinaryPrimitives.ReadUInt16LittleEndian(b.Slice(3, sizeof(UInt16))),
                Blocksize = BinaryPrimitives.ReadUInt16LittleEndian(b.Slice(5, sizeof(UInt16))),
                Flags = (Options)BinaryPrimitives.ReadUInt32LittleEndian(b.Slice(7, sizeof(UInt32))),
            };
        }
        throw new ArgumentException($"array is not Header");
    }

    public byte[] ToBytes()
    {
        var b = new byte[16];
        b[0] = VersMajor;
        b[1] = VersMinor;
        b[2] = VersPatch;
        BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(3, sizeof(UInt16)), Encoding);
        BinaryPrimitives.WriteUInt16LittleEndian(b.AsSpan(5, sizeof(UInt16)), Blocksize);
        BinaryPrimitives.WriteUInt32LittleEndian(b.AsSpan(7, sizeof(UInt32)), (UInt32)Flags);
        return b;
    }

    public bool Equals(Header? other)
    {
        if (null == other)
            return false;
        byte[] x = this.ToBytes();
        byte[] y = other.ToBytes();
        return Enumerable.SequenceEqual(x, y);
    }
    public override bool Equals(object? obj) => obj is Header header && Equals(header);
    public override int GetHashCode() => this.ToBytes().GetHashCode();

}
