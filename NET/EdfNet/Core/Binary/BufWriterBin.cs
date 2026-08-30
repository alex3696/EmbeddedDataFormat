namespace EdfNet.Core.Binary;

public readonly ref struct BufWriterBin : IBufWriter
{
    private readonly BufStateBin _state;
    public EdfType CurrentType => _state.Enum.CurrentType;

    public BufWriterBin(BufStateBin state)
    {
        _state = state;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureValueToken()
    {
        _state.Enum.MoveNext();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidatePrimitiveAndEnsureLen(EdfPrimitiveType got, int len)
    {
        WrongPrimitiveException.ThrowIfNotEqual(CurrentType.Type, got);
        EnsureCapacity(len);
    }
    public void Write(byte val)
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.UInt8, 1);
        //Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(_state.Blk.GetEmptyBuffer()), val);//MemoryMarshal.Write(_state.Blk.GetEmptyBuffer(), val);
        _state.Blk.GetEmptyBuffer()[0] = val;
        _state.Blk.DataLen += 1;
        EnsureValueToken();
    }
    public void Write(sbyte val)
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.Int8, 1);
        //Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(_state.Blk.GetEmptyBuffer()), val);
        _state.Blk.GetEmptyBuffer()[0] = unchecked((byte)val);
        _state.Blk.DataLen += 1;
        EnsureValueToken();
    }
    public void Write(ushort val)
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.UInt16, 2);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(_state.Blk.GetEmptyBuffer()), val);
        _state.Blk.DataLen += 2;
        EnsureValueToken();
    }
    public void Write(short val)
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.Int16, 2);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(_state.Blk.GetEmptyBuffer()), val);
        _state.Blk.DataLen += 2;
        EnsureValueToken();
    }
    public void Write(uint val)
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.UInt32, 4);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(_state.Blk.GetEmptyBuffer()), val);
        _state.Blk.DataLen += 4;
        EnsureValueToken();
    }
    public void Write(int val)
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.Int32, 4);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(_state.Blk.GetEmptyBuffer()), val);
        _state.Blk.DataLen += 4;
        EnsureValueToken();
    }
    public void Write(ulong val)
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.UInt64, 8);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(_state.Blk.GetEmptyBuffer()), val);
        _state.Blk.DataLen += 8;
        EnsureValueToken();
    }
    public void Write(long val)
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.Int64, 8);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(_state.Blk.GetEmptyBuffer()), val);
        _state.Blk.DataLen += 8;
        EnsureValueToken();
    }
    public void Write(float val)
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.Single, 4);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(_state.Blk.GetEmptyBuffer()), val);
        _state.Blk.DataLen += 4;
        EnsureValueToken();
    }
    public void Write(double val)
    {
        ValidatePrimitiveAndEnsureLen(EdfPrimitiveType.Double, 8);
        Unsafe.WriteUnaligned(ref MemoryMarshal.GetReference(_state.Blk.GetEmptyBuffer()), val);
        _state.Blk.DataLen += 8;
        EnsureValueToken();
    }

    public void Write<T>(T val) where T : struct, IBinaryNumber<T>
    {
        IncomatiblePrimitiveAndValueException.ThrowIfNotComatible(CurrentType.Type, typeof(T));
        var len = Unsafe.SizeOf<T>();
        EnsureCapacity(len);
        MemoryMarshal.Write(_state.Blk.GetEmptyBuffer(), val);
        _state.Blk.DataLen += (ushort)len;
        EnsureValueToken();
    }
    public void Write(string? str)
    {
        WrongPrimitiveException.ThrowIfNotEqual(CurrentType.Type, EdfPrimitiveType.String);
        var len = string.IsNullOrEmpty(str) ? 1 : 1 + int.Min(EdfBinString.MaxLen, Encoding.UTF8.GetByteCount(str));
        EnsureCapacity(len);
        EdfBinString.WriteBin(str, _state.Blk.GetEmptyBuffer());
        _state.Blk.DataLen += (ushort)len;
        EnsureValueToken();
    }
    public void WriteCharArray(ReadOnlySpan<byte> charArray)
    {
        WrongPrimitiveException.ThrowIfNotEqual(CurrentType.Type, EdfPrimitiveType.Char);
        int len = (int)CurrentType.GetTotalElements();
        EnsureCapacity(len);
        var datalen = int.Min(len, charArray.Length);
        var dst = _state.Blk.GetEmptyBuffer();
        charArray.Slice(0, datalen).CopyTo(dst);
        if (datalen < len)
            dst.Slice(datalen, len - datalen).Clear();
        _state.Blk.DataLen += (ushort)len;
        EnsureValueToken();
    }
    private readonly void EnsureCapacity(int len)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(len, _state.Blk.MaxDataLen);
        var emptyLen = _state.Blk.MaxDataLen - _state.Blk.DataLen;
        if (len > emptyLen)
        {
            _state.Stream.Write(_state.Blk);
            _state.Blk.DataLen = 0;
            _state.Blk.PrimOffset = (ushort)_state.Enum.PrimOffset;
            _state.Blk.RecordId = _state.Enum.RecordId;
        }
    }

    public void WriteSpan(ReadOnlySpan<byte> src, EdfPrimitiveType pt)
    {
        if (CurrentType.Type != pt)
            throw new EdfWrongTypeException();
        ushort len;
        switch (pt)
        {
            default: throw new PrimitiveNotSupportedException(pt);
            case EdfPrimitiveType.UInt8:
            case EdfPrimitiveType.Int8: len = 1; break;
            case EdfPrimitiveType.UInt16:
            case EdfPrimitiveType.Int16: len = 2; break;
            case EdfPrimitiveType.Single:
            case EdfPrimitiveType.UInt32:
            case EdfPrimitiveType.Int32: len = 4; break;
            case EdfPrimitiveType.Double:
            case EdfPrimitiveType.UInt64:
            case EdfPrimitiveType.Int64: len = 8; break;
            case EdfPrimitiveType.Char: WriteCharArray(src); return;
            case EdfPrimitiveType.String:
                {
                    len = (ushort)int.Min(EdfBinString.MaxLen, src.Length);
                    EnsureCapacity(len + 1);
                    var dst = _state.Blk.GetEmptyBuffer();
                    dst[0] = (byte)len;
                    src.Slice(0, len).CopyTo(dst.Slice(1, len));
                    _state.Blk.DataLen += (ushort)(len + 1);
                    EnsureValueToken();
                    return;
                }
        }
        // write number
        {
            EnsureCapacity(len);
            var dst = _state.Blk.GetEmptyBuffer();
            src.Slice(0, len).CopyTo(dst);
            _state.Blk.DataLen += len;
            EnsureValueToken();
        }
    }
}
