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
    public ushort TotalLen => (ushort)(HeaderLen + ContentLen + CrcLen);
    //public ushort FreeDataLen => (ushort)(MaxPayloadLen - DataLen);
    public ushort ContentLen
    {
        get => MemoryMarshal.Read<ushort>(_buffer.AsSpan(1, 2));
        set => MemoryMarshal.Write(_buffer.AsSpan(1, 2), value);
    }
    public ushort Crc => MemoryMarshal.Read<ushort>(_buffer.AsSpan(HeaderLen + ContentLen, CrcLen));
    public ReadOnlySpan<byte> Buffer => _buffer.AsSpan();
    public ReadOnlySpan<byte> BinaryBlock => _buffer.AsSpan(0, OverheadLen + ContentLen);
    public Span<byte> ContentBuffer => _buffer.AsSpan(HeaderLen, MaxPayloadLen);
    public ReadOnlySpan<byte> CurrentContent => _buffer.AsSpan(HeaderLen, ContentLen);

    public BinBlock(byte[] buf)
    {
        _buffer = buf;
    }
    public BinBlock(int len)
        : this(new byte[len])
    {
    }
    public int Clear() => ContentLen = 0;
    public void Reset()
    {
        Type = 0;
        Clear();
    }
    
    public int Append<T>(T val)
        where T : struct
    {
        //var valLen = Marshal.SizeOf<T>(); //return sizeof(T);
        var valLen = Unsafe.SizeOf<T>();
        MemoryMarshal.Write(_buffer.AsSpan(HeaderLen + ContentLen, valLen), val);
        ContentLen += (ushort)valLen;
        return valLen;
    }
    public int Append(string? str)
    {
        int writed = EdfBinString.WriteBin(str, _buffer.AsSpan(HeaderLen + ContentLen));
        ArgumentOutOfRangeException.ThrowIfLessThan(writed, 1);
        ContentLen += (ushort)writed;
        return writed;
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
    public bool CheckCrc()
    {
        int blockLenWithoutCrc = HeaderLen + ContentLen;
        var blkSpan = _buffer.AsSpan(0, blockLenWithoutCrc);
        return ModbusCRC.Calc(blkSpan) == Crc;
    }

    #region Privates
    protected readonly byte[] _buffer;
    #endregion
}


public static class EdfBinBlockExt
{
    public static int Write(this Stream stream, BinBlock block)
    {
        if (!Enum.IsDefined(block.Type))
            throw new ArgumentException(nameof(block.Type));
        ArgumentOutOfRangeException.ThrowIfEqual(block.ContentLen, 0);
        block.UpdateCrc();
        var bb = block.BinaryBlock;
        stream.Write(bb);
        return bb.Length;
    }
}
