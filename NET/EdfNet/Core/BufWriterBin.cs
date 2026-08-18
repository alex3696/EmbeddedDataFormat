namespace EdfNet.Core;

public readonly ref struct BufWriterBin : IBufWriter
{
    private readonly BufWriterState _state;
    private readonly EdfTypeEnumeratorStack _enm;

    public BufWriterBin(BufWriterState state, ref EdfTypeEnumeratorStack enm)
    {
        _state = state;
        _enm = enm;
        _enm.MoveNext();
    }
    #region Unused
    public void BeginArray() { }
    public void BeginStruct() { }
    public void EndArray() { }
    public void EndStruct() { }
    public void RecBegin() { _state.PrimOffset = 0; }
    public void RecEnd() { _state.RecordId++; }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void VarEnd()
    {
        _state.PrimOffset++;
        _enm.MoveNext();
    }
    #endregion
    public int Write<T>(T val) where T : struct
    {
        var len = Unsafe.SizeOf<T>();
        EnsureCapacity(len);
        MemoryMarshal.Write(_state.Blk.GetEmptyBuffer(), val);
        _state.Blk.DataLen += (ushort)len;
        VarEnd();
        return len;
    }
    public int Write(string? str)
    {
        var len = string.IsNullOrEmpty(str) ? 1 : 1 + int.Min(EdfBinString.MaxLen, Encoding.UTF8.GetByteCount(str));
        EnsureCapacity(len);
        EdfBinString.WriteBin(str, _state.Blk.GetEmptyBuffer());
        _state.Blk.DataLen += (ushort)len;
        VarEnd();
        return len;
    }
    public EdfType? GetCurrentType()
    {
        return _enm.Current;
    }
    public int Write(ReadOnlySpan<byte> val)
    {
        var len = val.Length;
        EnsureCapacity(len);
        val.CopyTo(_state.Blk.GetEmptyBuffer());
        _state.Blk.DataLen += (ushort)len;
        return len;
    }
    public int WriteCharArray(ReadOnlySpan<byte> charArray, int len)
    {
        EnsureCapacity(len);
        var datalen = int.Min(len, charArray.Length);
        var dst = _state.Blk.GetEmptyBuffer();
        charArray.Slice(0, datalen).CopyTo(dst);
        if (datalen < len)
            dst.Slice(datalen, len - datalen).Clear();
        _state.Blk.DataLen += (ushort)len;
        VarEnd();
        return datalen;
    }
    private readonly void EnsureCapacity(int len)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(len, _state.Blk.MaxDataLen);
        var emptyLen = _state.Blk.MaxDataLen - _state.Blk.DataLen;
        if (len > emptyLen)
        {
            _state.Stream.Write(_state.Blk);
            _state.Blk.DataLen = 0;
            _state.Blk.PrimOffset = (ushort)_state.PrimOffset;
            _state.Blk.RecordId = _state.RecordId;
        }
    }
}
