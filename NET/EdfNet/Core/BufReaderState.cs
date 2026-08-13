namespace EdfNet.Core;

public ref struct BufStateBin
{
    public readonly Stream Stream;
    public readonly BinDataBlock Blk;
    public int PrimOffset;
    public int RecordId;
    public int Readed;

    public readonly ReadOnlySpan<byte> DataBuf => Blk.DataBuffer.Slice(Readed);
    public readonly int DataLen => Blk.DataLen - Readed;

    public BufStateBin(Stream stream, BinDataBlock blk)
    {
        Stream = stream;
        Blk = blk;
        PrimOffset = 0;
        RecordId = 0;
    }
}

public ref struct BufStateTxt
{
    public readonly Stream Stream;
    public readonly Span<byte> Buf;
    public int Readed;   // сколько байт из Buf уже разобрано (потреблено)
    public int Writed;   // сколько байт реально загружено в Buf из Stream

    public BufStateTxt(Stream stream, Span<byte> buf)
    {
        Stream = stream;
        Buf = buf;
        Readed = 0;
        Writed = 0;
    }
}
