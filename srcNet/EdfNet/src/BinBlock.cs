namespace NetEdf.src;

public class BinBlock
{
    public BlockType Type;
    public UInt16 Qty;
    public readonly byte[] _data;

    public BinBlock(BlockType t, byte[] d, UInt16 qty, byte seq = 0)
    {
        Type = t;
        _data = d;
        Qty = qty;
    }

    public bool IsFull => Qty == _data.Length;

    public Span<byte> EmptySpan => _data.AsSpan(Qty, _data.Length - Qty);

    public ReadOnlySpan<byte> Data => _data.AsSpan(0, Qty);

    public int Remain => _data.Length - Qty;
    public void Clear() => Qty = 0;
    public void Add(ReadOnlySpan<byte> d)
    {
        d.CopyTo(_data.AsSpan(Qty));
        Qty += (ushort)d.Length;
    }


}
