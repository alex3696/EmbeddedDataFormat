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

public class BufStateTxt
{
    public readonly Stream Stream;
    public readonly byte[] _Buf;
    public readonly CircularEnumaratorEdfTypeTxt Enum = new();

    public Span<byte> Buf => _Buf;
    public int Readed;   // сколько байт из Buf уже разобрано (потреблено)
    public int Writed;   // сколько байт реально загружено в Buf из Stream

    public BufStateTxt(Stream stream, byte[] buf)
    {
        Stream = stream;
        _Buf = buf;
        Readed = 0;
        Writed = 0;
    }
}
