namespace EdfNet.Core.Binary;

internal static class BinaryEdfConfigSerializer
{
    public static EdfConfig? TryReadConfig(this BinBlock block)
    {
        if (block.Type != EdfBlockType.Config)
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
    public static void WriteConfig(this BinBlock block, EdfConfig config)
    {
        block.Reset();
        block.Type = EdfBlockType.Config;
        var buf = new SpanBufferWriter(block.ContentBuffer);
        buf.Append(config.VersMajor);
        buf.Append(config.VersMinor);
        buf.Append(config.Encoding);
        buf.Append(config.BlockSize);
        buf.Append((ushort)0);
        buf.Append(config.Flags);
        block.ContentLen = (ushort)buf.WrittedCount;
        ArgumentOutOfRangeException.ThrowIfNotEqual(block.ContentLen, 12);
    }
}
