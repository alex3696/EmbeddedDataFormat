namespace EdfNet.Core.Binary;

public class BinBlock
{
    public const int HeaderLen = sizeof(EdfBlockType) + sizeof(ushort);
    public const int CrcLen = sizeof(ushort);
    public const int OverheadLen = HeaderLen + CrcLen;
    public readonly int MaxPayloadLen;
    public EdfBlockType Type
    {
        get => (EdfBlockType)_buffer[0];
        set => _buffer[0] = (byte)value;
    }
    public ushort TotalLen => (ushort)(HeaderLen + ContentLen + CrcLen);
    //public ushort FreeDataLen => (ushort)(MaxPayloadLen - DataLen);
    public ushort ContentLen
    {
        get => Unsafe.ReadUnaligned<ushort>(ref _buffer[1]);
        set => Unsafe.WriteUnaligned(ref _buffer[1], value);
    }
    public const ushort ServiceLen = 8;
    public const ushort DataBeginIndex = HeaderLen + ServiceLen;
    public readonly int MaxDataLen;
    public ushort SchemaId
    {
        get => Unsafe.ReadUnaligned<ushort>(ref _buffer[HeaderLen]);
        set => Unsafe.WriteUnaligned(ref _buffer[HeaderLen], value);
    }
    public ushort PrimOffset
    {
        get => Unsafe.ReadUnaligned<ushort>(ref _buffer[HeaderLen + 2]);
        set => Unsafe.WriteUnaligned(ref _buffer[HeaderLen + 2], value);
    }
    public uint RecordId
    {
        get => Unsafe.ReadUnaligned<uint>(ref _buffer[HeaderLen + 4]);
        set => Unsafe.WriteUnaligned(ref _buffer[HeaderLen + 4], value);
    }
    public ushort DataLen
    {
        get => (ushort)(ContentLen - ServiceLen);
        set => ContentLen = (ushort)(value + ServiceLen);
    }

    public ushort Crc => MemoryMarshal.Read<ushort>(_buffer.AsSpan(HeaderLen + ContentLen, CrcLen));
    public Span<byte> Buffer => _buffer.AsSpan();
    public ReadOnlySpan<byte> BinaryBlock => new(_buffer, 0, OverheadLen + ContentLen);
    public Span<byte> ContentBuffer => new(_buffer, HeaderLen, MaxPayloadLen);
    public ReadOnlySpan<byte> CurrentContent => new(_buffer, HeaderLen, ContentLen);
    public ReadOnlySpan<byte> CurrentData => new(_buffer, DataBeginIndex, DataLen);
    public ReadOnlySpan<byte> ReadAvailable(int readed) => new(_buffer, DataBeginIndex + readed, DataLen - readed);
    public Span<byte> GetEmptyBuffer() => _buffer.AsSpan(DataBeginIndex + DataLen, MaxDataLen - DataLen);

    public BinBlock(byte[] buf)
    {
        _buffer = buf;
        MaxPayloadLen = _buffer.Length - OverheadLen;
        MaxDataLen = MaxPayloadLen - ServiceLen;
    }
    public int Clear() => ContentLen = 0;
    public void Reset()
    {
        Type = 0;
        Clear();
    }
    public ushort UpdateCrc()
    {
        int blockLenWithoutCrc = HeaderLen + ContentLen;
        var blkSpan = _buffer.AsSpan(0, blockLenWithoutCrc);
        var crcSpan = _buffer.AsSpan(blockLenWithoutCrc, CrcLen);
        ushort crc = ModbusCRC.Calc(blkSpan);
        MemoryMarshal.Write(crcSpan, crc);
        return crc;
    }
    public ushort CalcCrc()
    {
        ReadOnlySpan<byte> blkSpan = new(_buffer, 0, HeaderLen + ContentLen);
        return ModbusCRC.Calc(blkSpan);
    }

    #region Privates
    protected readonly byte[] _buffer;
    #endregion
}


