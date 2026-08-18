namespace EdfNet.Core;

public readonly ref struct BufReaderBin : IBufReader
{
    #region Unused
    public readonly bool ReadRecBegin() => true;
    public readonly bool ReadRecEnd() => true;
    public readonly bool ReadBeginStruct() => true;
    public readonly bool ReadEndStruct() => true;
    public readonly bool ReadBeginArray() => true;
    public readonly bool ReadEndArray() => true;
    public readonly bool ReadVarEnd() => true;
    #endregion
    private readonly BufStateBin _state;
    private readonly EdfType? _rootType;

    public BufReaderBin(BufStateBin state, EdfType? rootType)
    {
        _state = state;
        _rootType = rootType;
    }

    public T Read<T>() where T : struct
    {
        var len = Unsafe.SizeOf<T>();
        EnsureData(len);
        var val = MemoryMarshal.Read<T>(_state.DataBuf.Slice(0, len));
        _state.Readed += len;
        return val;
    }
    public string? ReadString()
    {
        EnsureData(1);
        var lenByte = _state.DataBuf[0];
        _state.Readed++;
        if (lenByte == 0) return null;
        EnsureData(lenByte);
        var str = Encoding.UTF8.GetString(_state.DataBuf.Slice(0, lenByte));
        _state.Readed += lenByte;
        return str;
    }
    public byte[] ReadCharArray(int len)
    {
        EnsureData(len);
        var result = new byte[len];
        _state.DataBuf.Slice(0, len).CopyTo(result);
        _state.Readed += len;
        return result;
    }
    public EdfType? GetCurrentType()
    {
        return _rootType;
    }
    public int Read(Span<byte> dst)
    {
        var len = dst.Length;
        EnsureData(len);
        _state.DataBuf.Slice(0, len).CopyTo(dst);
        _state.Readed += len;
        return len;
    }
    private void EnsureData(int len)
    {
        if (len > _state.DataLen)
        {
            var read = _state.Stream.Read(_state.Blk);// ReadNextBlock
            _state.Readed = 0;

            if (read == 0)
                throw new EndOfStreamException();
            if (_state.DataLen < len)
                throw new EndOfStreamException();
        }
    }
}
