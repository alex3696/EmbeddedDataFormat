namespace EdfNet.Core.Binary;

public readonly ref struct BufReaderBin : IBufReader
{
    public EdfType CurrentType => _state.Enum.CurrentType;
    private readonly BufStateBin _state;

    public BufReaderBin(BufStateBin state)
    {
        _state = state;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidatePrimitiveAndEnsureLen(EdfPrimitiveType got, int len)
    {
        WrongPrimitiveException.ThrowIfNotEqual(CurrentType.Type, got);
        EnsureData(len);
    }
    public byte ReadUInt8()
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.UInt8, 1);
        byte val = _state.ReadAvailableBuf[0];
        _state.Readed += 1;
        _state.Enum.MoveNext();
        return val;
    }
    public sbyte ReadInt8()
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.Int8, 1);
        sbyte val = unchecked((sbyte)_state.ReadAvailableBuf[0]);
        _state.Readed += 1;
        _state.Enum.MoveNext();
        return val;
    }
    public ushort ReadUInt16()
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.UInt16, 2);
        var val = Unsafe.As<byte, ushort>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 2;
        _state.Enum.MoveNext();
        return val;
    }
    public short ReadInt16()
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.Int16, 2);
        var val = Unsafe.As<byte, short>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 2;
        _state.Enum.MoveNext();
        return val;
    }
    public uint ReadUInt32()
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.UInt32, 4);
        var val = Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 4;
        _state.Enum.MoveNext();
        return val;
    }
    public int ReadInt32()
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.Int32, 4);
        var val = Unsafe.As<byte, int>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 4;
        _state.Enum.MoveNext();
        return val;
    }
    public ulong ReadUInt64()
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.UInt64, 8);
        var val = Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 8;
        _state.Enum.MoveNext();
        return val;
    }
    public long ReadInt64()
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.Int64, 8);
        var val = Unsafe.As<byte, long>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 8;
        _state.Enum.MoveNext();
        return val;
    }
    public float ReadSingle()
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.Single, 4);
        var val = Unsafe.As<byte, float>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 4;
        _state.Enum.MoveNext();
        return val;
    }
    public double ReadDouble()
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.Double, 8);
        var val = Unsafe.As<byte, double>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += 8;
        _state.Enum.MoveNext();
        return val;
    }

    public T Read<T>() where T : struct
    {
        IncompatiblePrimitiveAndValueException.ThrowIfNotCompatible(CurrentType.Type, typeof(T));
        var len = Unsafe.SizeOf<T>();
        EnsureData(len);
        T val = Unsafe.As<byte, T>(ref MemoryMarshal.GetReference(_state.ReadAvailableBuf));
        _state.Readed += len;
        _state.Enum.MoveNext();
        return val;
    }
    public string? ReadString()
    {
        WrongPrimitiveException.ThrowIfNotEqual(CurrentType.Type, EdfPrimitiveType.String);
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
        WrongPrimitiveException.ThrowIfNotEqual(CurrentType.Type, EdfPrimitiveType.Char);
        int len = (int)CurrentType.GetTotalElements();
        EnsureData(len);
        var result = new byte[len];
        _state.ReadAvailableBuf.Slice(0, len).CopyTo(result);
        _state.Readed += len;
        _state.Enum.MoveNext();
        return result;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureData(int len)
    {
        if (len > _state.ReadAvailableLen)
            DownloadBlock(len);
    }
    private void DownloadBlock(int len)
    {
        var available = _state.ReadAvailableLen;
        var currentSchema = _state.Blk.SchemaId;
        _state.Readed = 0;
        if (0 < available)
            throw new BinaryBlockHasTrashException(available);
        var read = _state.Stream.Read(_state.Blk);// ReadNextBlock
        if (read == 0)
            throw new EndOfStreamException();
        BinaryBlockSequenceException.ThrowIfBlockTypeNotEqual(EdfBlockType.Data, _state.Blk.Type);
        BinaryBlockSequenceException.ThrowIfNotEqualSchemaId(currentSchema, _state.Blk.SchemaId);
        BinaryBlockSequenceException.ThrowIfNotEqualRecordId(_state.Enum.RecordId, _state.Blk.RecordId);
        BinaryBlockSequenceException.ThrowIfNotEqualPrimOffset(_state.Enum.PrimOffset, _state.Blk.PrimOffset);
        BinaryBlockWrongLengthException.ThrowIfLess(len, _state.ReadAvailableLen);
    }
    public void ReadToSpan(Span<byte> dst, out EdfPrimitiveType pt, out int len)
    {
        pt = CurrentType.Type;
        switch (pt)
        {
            default: throw new PrimitiveNotSupportedException(pt);
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
