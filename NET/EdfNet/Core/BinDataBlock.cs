namespace EdfNet.Core;

public class BinDataBlock : BinBlock
{
    public const ushort ServiceLen = 8;
    public const ushort DataBefinIndex = HeaderLen + ServiceLen;
    public readonly int MaxDataLen;
    public ushort SchemaId
    {
        get => MemoryMarshal.Read<ushort>(_buffer.AsSpan(HeaderLen, 2));
        set => MemoryMarshal.Write(_buffer.AsSpan(HeaderLen, 2), value);
    }
    public ushort PrimOffset
    {
        get => MemoryMarshal.Read<ushort>(_buffer.AsSpan(HeaderLen + 2, 2));
        set => MemoryMarshal.Write(_buffer.AsSpan(HeaderLen + 2, 2), value);
    }
    public uint RecordId
    {
        get => MemoryMarshal.Read<uint>(_buffer.AsSpan(HeaderLen + 4, 4));
        set => MemoryMarshal.Write(_buffer.AsSpan(HeaderLen + 4, 4), value);
    }
    public ushort DataLen
    {
        get => (ushort)(ContentLen - ServiceLen);
        set => ContentLen = (ushort)(value + ServiceLen);
    }
    public Span<byte> DataBuffer => _buffer.AsSpan(DataBefinIndex, MaxDataLen);
    public ReadOnlySpan<byte> CurrentData => _buffer.AsSpan(DataBefinIndex, DataLen);

    public Span<byte> GetEmptyBuffer() => _buffer.AsSpan(DataBefinIndex + DataLen, MaxDataLen);
    public BinDataBlock(byte[] buf)
        : base(buf)
    {
        MaxDataLen = MaxPayloadLen - ServiceLen;
    }

    public new int Clear() => ContentLen = ServiceLen;

}

