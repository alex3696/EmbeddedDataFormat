namespace EdfNet.Core;

public ref struct BufWriterBin : IBufWriter
{
    private readonly BufWriterState _state;
    private readonly EdfType? _rootType;

    public BufWriterBin(BufWriterState state, EdfType? rootType)
    {
        _state = state;
        _rootType = rootType;
    }
    #region Unused
    public readonly void BeginArray() { }
    public readonly void BeginStruct() { }
    public readonly void EndArray() { }
    public readonly void EndStruct() { }
    public readonly void RecBegin() { }
    public readonly void RecEnd() { }
    public readonly void VarEnd() { }
    #endregion
    public readonly int Write<T>(T val) where T : struct
    {
        var len = Marshal.SizeOf<T>();
        EnsureCapacity(len);
        MemoryMarshal.Write(_state.Blk.GetEmptyBuffer(), val);
        _state.Blk.DataLen += (ushort)len;
        return len;
    }
    public readonly int Write(string? str)
    {
        var len = string.IsNullOrEmpty(str) ? 1 : 1 + int.Min(EdfBinString.MaxLen, Encoding.UTF8.GetByteCount(str));
        EnsureCapacity(len);
        EdfBinString.WriteBin(str, _state.Blk.GetEmptyBuffer());
        _state.Blk.DataLen += (ushort)len;
        return len;
    }
    public EdfType? GetCurrentType()
    {
        return _rootType;
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
