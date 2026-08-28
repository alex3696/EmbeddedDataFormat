namespace EdfNet.Core.Binary;

public readonly ref struct BufReaderBin : IBufReader
{
    public EdfType CurrentType => _state.Enum.CurrentType;
    private readonly BufStateBin _state;

    public BufReaderBin(BufStateBin state)
    {
        _state = state;
    }

    public T Read<T>() where T : struct
    {
        if (CurrentType.Type != typeof(T).GetPoType())
            throw new EdfWrongTypeException();
        var len = Unsafe.SizeOf<T>();
        EnsureData(len);
        var val = MemoryMarshal.Read<T>(_state.ReadAvailableBuf.Slice(0, len));
        _state.Readed += len;
        _state.Enum.MoveNext();
        return val;
    }
    public string? ReadString()
    {
        if (CurrentType.Type != EdfPrimitiveType.String)
            throw new EdfWrongTypeException();
        EnsureData(1);
        var lenByte = _state.ReadAvailableBuf[0];
        _state.Readed++;
        if (lenByte == 0) return null;
        EnsureData(lenByte);
        var str = Encoding.UTF8.GetString(_state.ReadAvailableBuf.Slice(0, lenByte));
        _state.Readed += lenByte;
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
            case EdfPrimitiveType.String:
                len = _state.ReadAvailableBuf[0];
                _state.ReadAvailableBuf.Slice(1, len).CopyTo(dst);
                _state.Readed += len + 1;
                _state.Enum.MoveNext();
                return;

        }
        _state.ReadAvailableBuf.Slice(0, len).CopyTo(dst);
        _state.Readed += len;
        _state.Enum.MoveNext();
    }
}
