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

    public ReadOnlySpan<byte> ReadStringRawSpan()
    {
        if (CurrentType.Type != EdfPrimitiveType.String)
            throw new EdfWrongTypeException();
        EnsureData(1);
        var lenByte = _state.ReadAvailableBuf[0];
        _state.Readed++;
        if (lenByte == 0) return null;
        EnsureData(lenByte);
        ReadOnlySpan<byte> dirtyBuf = _state.ReadAvailableBuf.Slice(0, lenByte);
        _state.Readed += lenByte;
        _state.Enum.MoveNext();
        return dirtyBuf;
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
}
