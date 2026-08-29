namespace EdfNet.Core.Binary;

public readonly ref struct BufReaderBin : IBufReader
{
    public EdfType CurrentType => _state.Enum.CurrentType;
    private readonly BufStateBin _state;

    public BufReaderBin(BufStateBin state)
    {
        _state = state;
    }

    public byte ReadUInt8()
    {
        if (CurrentType.Type != EdfPrimitiveType.UInt8)
            throw new EdfWrongTypeException();
        EnsureData(1);
        byte val = _state.ReadAvailableBuf[0];
        _state.Readed += 1;
        _state.Enum.MoveNext();
        return val;
    }
    public sbyte ReadInt8()
    {
        if (CurrentType.Type != EdfPrimitiveType.Int8)
            throw new EdfWrongTypeException();
        EnsureData(1);
        sbyte val = unchecked((sbyte)_state.ReadAvailableBuf[0]);
        _state.Readed += 1;
        _state.Enum.MoveNext();
        return val;
    }
    public ushort ReadUInt16()
    {
        if (CurrentType.Type != EdfPrimitiveType.UInt16)
            throw new EdfWrongTypeException();
        EnsureData(2);
        var val = Unsafe.As<byte, ushort>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 2;
        _state.Enum.MoveNext();
        return val;
    }
    public short ReadInt16()
    {
        if (CurrentType.Type != EdfPrimitiveType.Int16)
            throw new EdfWrongTypeException();
        EnsureData(2);
        var val = Unsafe.As<byte, short>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 2;
        _state.Enum.MoveNext();
        return val;
    }
    public uint ReadUInt32()
    {
        if (CurrentType.Type != EdfPrimitiveType.UInt32)
            throw new EdfWrongTypeException();
        EnsureData(4);
        var val = Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 4;
        _state.Enum.MoveNext();
        return val;
    }
    public int ReadInt32()
    {
        if (CurrentType.Type != EdfPrimitiveType.Int32)
            throw new EdfWrongTypeException();
        EnsureData(4);
        var val = Unsafe.As<byte, int>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 4;
        _state.Enum.MoveNext();
        return val;
    }
    public ulong ReadUInt64()
    {
        if (CurrentType.Type != EdfPrimitiveType.UInt64)
            throw new EdfWrongTypeException();
        EnsureData(8);
        var val = Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 8;
        _state.Enum.MoveNext();
        return val;
    }
    public long ReadInt64()
    {
        if (CurrentType.Type != EdfPrimitiveType.Int64)
            throw new EdfWrongTypeException();
        EnsureData(8);
        var val = Unsafe.As<byte, long>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 8;
        _state.Enum.MoveNext();
        return val;
    }
    public float ReadSingle()
    {
        if (CurrentType.Type != EdfPrimitiveType.Single)
            throw new EdfWrongTypeException();
        EnsureData(4);
        var val = Unsafe.As<byte, float>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 4;
        _state.Enum.MoveNext();
        return val;
    }
    public double ReadDouble()
    {
        if (CurrentType.Type != EdfPrimitiveType.Double)
            throw new EdfWrongTypeException();
        EnsureData(8);
        var val = Unsafe.As<byte, double>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 8;
        _state.Enum.MoveNext();
        return val;
    }

    public T Read<T>() where T : struct
    {
        if (CurrentType.Type != typeof(T).GetPoType())
            throw new EdfWrongTypeException();
        var len = Unsafe.SizeOf<T>();
        EnsureData(len);
        T val = Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += len;
        _state.Enum.MoveNext();
        return val;
    }
    public string? ReadString()
    {
        if (CurrentType.Type != EdfPrimitiveType.String)
            throw new EdfWrongTypeException();
        EnsureData(1);
        var src = _state.ReadAvailableBuf;
        var lenByte = src[0];
        if (lenByte == 0)
        {
            _state.Readed++;
            return null;
        }
        EnsureData(lenByte);
        var str = Encoding.UTF8.GetString(src.Slice(1, lenByte));
        _state.Readed += 1 + lenByte;
        _state.Enum.MoveNext();
        return str;
    }
    public byte[] ReadCharArray()
    {
        if (CurrentType.Type != EdfPrimitiveType.Char)
            throw new EdfWrongTypeException();
        int len = (int)CurrentType.GetTotalElements();
        EnsureData(len);
        var result = new byte[len];
        _state.ReadAvailableBuf.Slice(0, len).CopyTo(result);
        _state.Readed += len;
        _state.Enum.MoveNext();
        return result;
    }
    private void EnsureData(int len)
    {
        if (len > _state.ReadAvailableLen)
        {
            var read = _state.Stream.Read(_state.Blk);// ReadNextBlock
            _state.Readed = 0;

            if (read == 0)
                throw new EndOfStreamException();
            if (_state.ReadAvailableLen < len)
                throw new EndOfStreamException();
        }
    }
    public void ReadToSpan(Span<byte> dst, out EdfPrimitiveType pt, out int len)
    {
        pt = CurrentType.Type;
        switch (pt)
        {
            default: throw new EdfWrongTypeException();
            case EdfPrimitiveType.UInt8:
            case EdfPrimitiveType.Int8: len = 1; break;
            case EdfPrimitiveType.UInt16:
            case EdfPrimitiveType.Int16: len = 2; break;
            case EdfPrimitiveType.UInt32:
            case EdfPrimitiveType.Int32:
            case EdfPrimitiveType.Single: len = 4; break;
            case EdfPrimitiveType.UInt64:
            case EdfPrimitiveType.Int64:
            case EdfPrimitiveType.Double: len = 8; break;
            case EdfPrimitiveType.Char:
                {
                    len = (int)CurrentType.GetTotalElements();
                    EnsureData(len);
                    _state.ReadAvailableBuf.Slice(0, len).CopyTo(dst);
                    _state.Readed += len;
                    _state.Enum.MoveNext();
                    return;
                }
            case EdfPrimitiveType.String:
                {
                    var src = _state.ReadAvailableBuf;
                    len = src[0];
                    src.Slice(1, len).CopyTo(dst);
                    _state.Readed += len + 1;
                    _state.Enum.MoveNext();
                    return;
                }
        }
        _state.ReadAvailableBuf.Slice(0, len).CopyTo(dst);
        _state.Readed += len;
        _state.Enum.MoveNext();
    }
}
