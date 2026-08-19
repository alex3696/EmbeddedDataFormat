namespace EdfNet.Core;

public class BufWriterState
{
    public readonly Stream Stream;
    public readonly BinDataBlock Blk;
    public readonly CircularEnumaratorEdfType Enum = new();

    public BufWriterState(Stream stream, BinDataBlock blk)
    {
        Stream = stream;
        Blk = blk;
    }
}
