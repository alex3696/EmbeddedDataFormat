namespace EdfNet.Core;

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
        if (CurrentType.Type != PoType.String)
            throw new EdfWrongTypeException();
        var len = string.IsNullOrEmpty(str) ? 1 : 1 + int.Min(EdfBinString.MaxLen, Encoding.UTF8.GetByteCount(str));
        EnsureCapacity(len);
        EdfBinString.WriteBin(str, _state.Blk.GetEmptyBuffer());
        _state.Blk.DataLen += (ushort)len;
        EnsureValueToken();
    }
    public void WriteCharArray(ReadOnlySpan<byte> charArray)
    {
        if (CurrentType.Type != PoType.Char)
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
}
