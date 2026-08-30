namespace EdfNet.Core.Binary;

public sealed class BufStateBin : IDisposable
{
    public readonly Stream Stream;
    public readonly BinDataBlock Blk;
    public BinaryCircularEdfTypeEnumerator Enum { get; private set; } = new();
    public int Readed;

    public ReadOnlySpan<byte> ReadAvailableBuf => Blk.ReadAvailable(Readed);
    public int ReadAvailableLen => Blk.DataLen - Readed;

    public BufStateBin(Stream stream, BinDataBlock blk)
    {
        Stream = stream;
        Blk = blk;
    }
    public void Dispose()
    {
        Enum?.Dispose(); Enum = null!;
    }
}
