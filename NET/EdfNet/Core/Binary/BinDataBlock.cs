namespace EdfNet.Core.Binary;

public class BinDataBlock : BinBlock
{
    public const ushort ServiceLen = 8;
    public const ushort DataBeпinIndex = HeaderLen + ServiceLen;
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
    public Span<byte> DataBuffer => _buffer.AsSpan(DataBeпinIndex, MaxDataLen);
    public ReadOnlySpan<byte> CurrentData => new(_buffer, DataBeпinIndex, DataLen);
    public ReadOnlySpan<byte> ReadAvailable(int readed) => new(_buffer, DataBeпinIndex + readed, DataLen - readed);

    public Span<byte> GetEmptyBuffer() => _buffer.AsSpan(DataBeпinIndex + DataLen, MaxDataLen - DataLen);
    public BinDataBlock(byte[] buf)
        : base(buf)
    {
        MaxDataLen = MaxPayloadLen - ServiceLen;
    }

    public new int Clear() => ContentLen = ServiceLen;

}

