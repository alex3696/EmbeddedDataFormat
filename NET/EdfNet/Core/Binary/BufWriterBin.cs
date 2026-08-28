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
    public void Write<T>(T val) where T : struct
    {
        if (CurrentType.Type != typeof(T).GetPoType())
            throw new EdfWrongTypeException();
        var len = Unsafe.SizeOf<T>();
        EnsureCapacity(len);
        MemoryMarshal.Write(_state.Blk.GetEmptyBuffer(), val);
        _state.Blk.DataLen += (ushort)len;
        EnsureValueToken();
    }
    public void Write(string? str)
    {
        if (CurrentType.Type != EdfPrimitiveType.String)
            throw new EdfWrongTypeException();
        var len = string.IsNullOrEmpty(str) ? 1 : 1 + int.Min(EdfBinString.MaxLen, Encoding.UTF8.GetByteCount(str));
        EnsureCapacity(len);
        EdfBinString.WriteBin(str, _state.Blk.GetEmptyBuffer());
        _state.Blk.DataLen += (ushort)len;
        EnsureValueToken();
    }
    public void WriteCharArray(ReadOnlySpan<byte> charArray)
    {
        if (CurrentType.Type != EdfPrimitiveType.Char)
            throw new EdfWrongTypeException();
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
        switch (pt)
        {
            default: throw new EdfWrongTypeException();
            //case EdfPrimitiveType.UInt8: Write(MemoryMarshal.Read<byte>(src)); break;
            //case EdfPrimitiveType.Int8: Write(MemoryMarshal.Read<sbyte>(src)); break;
            //case EdfPrimitiveType.UInt16: Write(MemoryMarshal.Read<ushort>(src)); break;
            //case EdfPrimitiveType.Int16: Write(MemoryMarshal.Read<short>(src)); break;
            //case EdfPrimitiveType.UInt32: Write(MemoryMarshal.Read<uint>(src)); break;
            //case EdfPrimitiveType.Int32: Write(MemoryMarshal.Read<int>(src)); break;
            //case EdfPrimitiveType.UInt64: Write(MemoryMarshal.Read<ulong>(src)); break;
            //case EdfPrimitiveType.Int64: Write(MemoryMarshal.Read<long>(src)); break;
            //case EdfPrimitiveType.Single: Write(MemoryMarshal.Read<float>(src)); break;
            //case EdfPrimitiveType.Double: Write(MemoryMarshal.Read<double>(src)); break;
            //case EdfPrimitiveType.Char: WriteCharArray(src); break;
            case EdfPrimitiveType.String:
                var len = int.Min(EdfBinString.MaxLen, src.Length);
                EnsureCapacity(len + 1);
                var dst = _state.Blk.GetEmptyBuffer();
                dst[0] = (byte)len;
                src.Slice(0, len).CopyTo(dst.Slice(1, len));
                _state.Blk.DataLen += (ushort)(len + 1);
                EnsureValueToken();
                break;
        }
    }
}
