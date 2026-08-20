namespace EdfNet.Core;

public readonly ref struct BufWriterBin : IBufWriter
{
    private readonly BufWriterState _state;

    public BufWriterBin(BufWriterState state)
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
        if (_state.Enum.GetCurrentType()?.Type != typeof(T).GetPoType())
            throw new EdfWrongTypeException();
        var len = Unsafe.SizeOf<T>();
        EnsureCapacity(len);
        MemoryMarshal.Write(_state.Blk.GetEmptyBuffer(), val);
        _state.Blk.DataLen += (ushort)len;
        EnsureValueToken();
    }
    public void Write(string? str)
    {
        if (_state.Enum.GetCurrentType()?.Type != PoType.String)
            throw new EdfWrongTypeException();
        var len = string.IsNullOrEmpty(str) ? 1 : 1 + int.Min(EdfBinString.MaxLen, Encoding.UTF8.GetByteCount(str));
        EnsureCapacity(len);
        EdfBinString.WriteBin(str, _state.Blk.GetEmptyBuffer());
        _state.Blk.DataLen += (ushort)len;
        EnsureValueToken();
    }
    public EdfType? GetCurrentType()
    {
        return _state.Enum.GetCurrentType();
    }
    public void WriteCharArray(ReadOnlySpan<byte> charArray, int len)
    {
        if (_state.Enum.GetCurrentType()?.Type != PoType.Char)
            throw new EdfWrongTypeException();
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
}
