namespace NetEdf.src;

public class BinBlock
{
    public BlockType Type;
    public byte Seq;
    public UInt16 Qty;
    public readonly byte[] _data;

    public BinBlock(BlockType t, byte[] d, UInt16 qty, byte seq = 0)
    {
        Type = t;
        Seq = seq;
        Qty = qty;
        _data = d;
    }
    public ReadOnlySpan<byte> Data => _data.AsSpan(0, Qty);


}
