namespace EdfNet.Gen;

public class RecursiveWriterState
{
    public readonly Stream DstStream;
    public readonly BinDataBlock Block;
    public int PrimOffset = 0;
    public int Skip = 0;
    public uint RecordId = 0;

    public RecursiveWriterState(Stream s, BinDataBlock b)
    {
        DstStream = s;
        Block = b;
    }
    public void Reset()
    {
        RecordId = 0;
        PrimOffset = 0;
    }
}

public ref struct RecursiveWriterBin<TEnumerator> : IPrimitiveIo
    where TEnumerator : struct, IEdfByteEnumerator
{
    #region Unused
    public void SepRecBegin() { }
    public void SepRecEnd() { }
    public void SepBeginStruct() { }
    public void SepEndStruct() { }
    public void SepBeginArray() { }
    public void SepEndArray() { }
    public void SepVarEnd() { }
    #endregion
    private readonly RecursiveWriterState _state;
    private readonly ref TEnumerator _enumerator;
    private readonly EdfType _rooEt;
    private bool _hasCurrent;

    public RecursiveWriterBin(RecursiveWriterState state
        , EdfType edfType, ref TEnumerator enm)
    {
        _state = state;
        _enumerator = ref enm;
        _rooEt = edfType;
        _hasCurrent = false;
    }

    public EdfErr DoWrite()
    {
        do
        {
            try
            {
                EdfTypeWalkerBinRef.Process(_rooEt, ref this);
            }
            catch (EdfWrongTypeException)
            {
                return EdfErr.WrongType;
            }
            catch (EdfSrcDataRequredException)
            {
                _state.Skip = _state.PrimOffset;
                return EdfErr.SrcDataRequred;
            }
            catch (EdfDstBufOverflowException)
            {
                return EdfErr.DstBufOverflow;
            }
            _state.RecordId++;
            _state.PrimOffset = 0;
            _state.Skip = 0;
            if (_enumerator.MoveNext(_rooEt))
            {
                _hasCurrent = true;
            }
            else
                return EdfErr.IsOk;
        }
        while (true);
    }

    public void Primitive(EdfType edfType)
    {
        if (0 < _state.Skip)
        {
            _state.Skip--;
            return;
        }
        ArgumentNullException.ThrowIfNull(edfType, nameof(edfType));

        if (!_hasCurrent)
        {
            if (!_enumerator.MoveNext(edfType))
                throw new EdfSrcDataRequredException();
            _hasCurrent = true;
        }

        int retry = 1;
        do
        {
            Span<byte> dst = _state.Block.GetEmptyBuffer();
            var len = _enumerator.Write(dst);
            if (0 > len)
            {
                _state.DstStream.Write(_state.Block);
                _state.Block.DataLen = 0;
                _state.Block.PrimOffset = (ushort)_state.PrimOffset;
                _state.Block.RecordId = _state.RecordId;
                continue;
            }
            _hasCurrent = false;
            _state.Block.DataLen += (ushort)len;
            _state.PrimOffset++;
            return;
        }
        while (0 < retry);
        throw new EdfDstBufOverflowException();
    }
}
