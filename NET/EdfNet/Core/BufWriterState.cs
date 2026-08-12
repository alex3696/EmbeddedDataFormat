namespace EdfNet.Core;

public class BufWriterState
{
    public readonly Stream Stream;
    public readonly BinDataBlock Blk;
    public int PrimOffset = 0;
    public int Skip = 0;
    public uint RecordId = 0;
    public BufWriterState(Stream stream, BinDataBlock blk, int primOffset = 0, int skip = 0, uint recordId = 0)
    {
        Stream = stream;
        Blk = blk;
        PrimOffset = primOffset;
        Skip = skip;
        RecordId = recordId;
    }
}
