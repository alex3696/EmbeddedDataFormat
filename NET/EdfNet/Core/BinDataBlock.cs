namespace EdfNet.Core;

public class BinDataBlock : BinBlock
{
    public const ushort ServiceLen = 8;
    public ushort SchemaId
    {
        get => MemoryMarshal.Read<ushort>(_buffer.AsSpan(HeaderLen, 2));
        set => MemoryMarshal.Write(_buffer.AsSpan(HeaderLen, 2), value);
    }
    public uint RecordId
    {
        get => MemoryMarshal.Read<uint>(_buffer.AsSpan(HeaderLen + 2, 4));
        set => MemoryMarshal.Write(_buffer.AsSpan(HeaderLen + 2, 4), value);
    }
    public ushort PrimOffset
    {
        get => MemoryMarshal.Read<ushort>(_buffer.AsSpan(HeaderLen + 6, 2));
        set => MemoryMarshal.Write(_buffer.AsSpan(HeaderLen + 6, 2), value);
    }
    public ushort DataLen
    {
        get => (ushort)(ContentLen - ServiceLen);
        set => ContentLen = (ushort)(value + ServiceLen);
    }
    public Span<byte> DataBuffer => _buffer.AsSpan(HeaderLen + ServiceLen, MaxPayloadLen - ServiceLen);
    public ReadOnlySpan<byte> CurrentData => _buffer.AsSpan(HeaderLen + ServiceLen, DataLen);

    public Span<byte> GetEmptyBuffer() => _buffer.AsSpan(HeaderLen + ServiceLen + DataLen, MaxPayloadLen - ServiceLen - DataLen);
    public BinDataBlock(byte[] buf)
        : base(buf)
    {
    }

    public new int Clear() => ContentLen = ServiceLen;

}

