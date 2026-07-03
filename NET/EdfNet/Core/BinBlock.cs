namespace EdfNet.Core;

public class BinBlock
{
    public const int HeaderLen = sizeof(BlockType) + sizeof(ushort);
    public const int CrcLen = sizeof(ushort);
    public const int OverheadLen = HeaderLen + CrcLen;
    public int MaxPayloadLen => _buffer.Length - OverheadLen;
    public BlockType Type
    {
        get => (BlockType)_buffer[0];
        set => _buffer[0] = (byte)value;
    }
    public ushort Len => (ushort)(HeaderLen + DataLen + CrcLen);
    public ushort FreeDataLen => (ushort)(MaxPayloadLen - DataLen);
    public ushort DataLen
    {
        get => MemoryMarshal.Read<ushort>(_buffer.AsSpan(1, 2));
        set => MemoryMarshal.Write(_buffer.AsSpan(1, 2), value);
    }
    public ushort Crc
    {
        get => MemoryMarshal.Read<ushort>(_buffer.AsSpan(HeaderLen + DataLen, CrcLen));
    }
    public ReadOnlySpan<byte> Buffer => _buffer.AsSpan();
    public ReadOnlySpan<byte> BinaryBlock => _buffer.AsSpan(0, OverheadLen + DataLen);
    public Span<byte> DataBuffer => _buffer.AsSpan(HeaderLen, MaxPayloadLen);
    public ReadOnlySpan<byte> CurrentData => _buffer.AsSpan(HeaderLen, DataLen);

    public BinBlock(byte[] buf)
    {
        _buffer = buf;
    }
    public BinBlock(int len)
        : this(new byte[len])
    {
    }
    public void Reset()
    {
        Type = 0;
        DataLen = 0;
    }
    public int Clear() => DataLen = 0;
    public int Append<T>(T val)
        where T : struct
    {
        //var valLen = Marshal.SizeOf<T>(); //return sizeof(T);
        var valLen = Unsafe.SizeOf<T>();
        MemoryMarshal.Write(_buffer.AsSpan(HeaderLen + DataLen, valLen), val);
        DataLen += (ushort)valLen;
        return valLen;
    }
    public int Append(string? str)
    {
        int writed = EdfBinString.WriteBin(str, _buffer.AsSpan(HeaderLen + DataLen));
        ArgumentOutOfRangeException.ThrowIfLessThan(writed, 1);
        DataLen += (ushort)writed;
        return writed;
    }
    public ushort UpdateCrc()
    {
        int blockLenWithoutCrc = HeaderLen + DataLen;
        var blkSpan = _buffer.AsSpan(0, blockLenWithoutCrc);
        var crcSpan = _buffer.AsSpan(blockLenWithoutCrc, CrcLen);
        ushort crc = ModbusCRC.Calc(blkSpan);
        MemoryMarshal.Write(crcSpan, crc);
        return crc;
    }
    public bool CheckCrc()
    {
        int blockLenWithoutCrc = HeaderLen + DataLen;
        var blkSpan = _buffer.AsSpan(0, blockLenWithoutCrc);
        return ModbusCRC.Calc(blkSpan) == Crc;
    }

    #region Privates
    private readonly byte[] _buffer;
    #endregion
}

public static class EdfBinBlockExt
{
    public static int Write(this Stream stream, BinBlock block)
    {
        if (!Enum.IsDefined(block.Type))
            throw new ArgumentException(nameof(block.Type));
        ArgumentOutOfRangeException.ThrowIfEqual(block.DataLen, 0);
        block.UpdateCrc();
        var bb = block.BinaryBlock;
        stream.Write(bb);
        return bb.Length;
    }
}
