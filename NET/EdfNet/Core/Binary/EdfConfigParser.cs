namespace EdfNet.Core.Binary;

public static class EdfConfigParser
{
    public static EdfConfig? ReadConfig(this BinBlock block)
    {
        if (block.Type != BlockType.Config)
            return null;
        var b = block.CurrentContent;
        return new EdfConfig()
        {
            VersMajor = b[0],
            VersMinor = b[1],
            Encoding = MemoryMarshal.Read<ushort>(b[2..]),
            BlockSize = MemoryMarshal.Read<ushort>(b[4..]),
            Flags = MemoryMarshal.Read<EdfConfigOptions>(b[8..]),
        };
    }
}
