namespace EdfNet.Core;

public class BufStateBin
{
    public readonly Stream Stream;
    public readonly BinDataBlock Blk;
    public readonly CircularEnumaratorEdfType Enum = new();
    public int Readed;

    public ReadOnlySpan<byte> ReadAvailableBuf => Blk.DataBuffer.Slice(Readed);
    public int ReadAvailableLen => Blk.DataLen - Readed;

    public BufStateBin(Stream stream, BinDataBlock blk)
    {
        Stream = stream;
        Blk = blk;
    }
}
