namespace EdfNet.Ref;

public class RecursiveWriterBin : IPrimitiveIo
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
    public int PrimitivesWritted { get; private set; } = 0;
    public int BytesWritted { get; private set; } = 0;
    public int Skip { get; set; } = 0;
    public uint RecordId { get; private set; } = 0;

    public RecursiveWriterBin(BinDataBlock blk, Stream dstStream)
    {
        _blk = blk;
        _stream = dstStream;
        _decomposer = new();
    }

    public EdfErr DoWrite(EdfType edfType, object obj)
    {
        if (0 == Skip)
            _decomposer.Reset(edfType, obj);
        else
            _decomposer.ResetAdd(obj);
        _hasCurrent = false;
        do
        {
            try
            {
                EdfTypeWalkerBin.Process(edfType, this);
            }
            catch (EdfWrongTypeException)
            {
                return EdfErr.WrongType;
            }
            catch (EdfSrcDataRequredException)
            {
                Skip = PrimitivesWritted;
                return EdfErr.SrcDataRequred;
            }
            catch (EdfDstBufOverflowException)
            {
                return EdfErr.DstBufOverflow;
            }
            RecordId++;
            PrimitivesWritted = 0;
            Skip = 0;
            if (_decomposer.MoveNext(edfType))
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
        if (0 < Skip)
        {
            Skip--;
            return;
        }
        ArgumentNullException.ThrowIfNull(edfType, nameof(edfType));

        if (!_hasCurrent)
        {
            if (!_decomposer.MoveNext(edfType))
                throw new EdfSrcDataRequredException();
            _hasCurrent = true;
        }
        int retry = 1;
        do
        {
            Span<byte> dst = _blk.GetEmptyBuffer();
            _decomposer.DstType = edfType;
            var len = _decomposer.Write(dst);
            if (0 > len)
            {
                _stream.Write(_blk);
                _blk.DataLen = 0;
                _blk.PrimOffset = (ushort)PrimitivesWritted;
                _blk.RecordId = RecordId;
                continue;
            }
            _hasCurrent = false;
            _blk.DataLen += (ushort)len;
            BytesWritted += (ushort)len;
            PrimitivesWritted++;
            return;
        }
        while (0 < retry);
        throw new EdfDstBufOverflowException();
    }



    private readonly Stream _stream;
    private readonly BinDataBlock _blk;
    private readonly PrimitiveDecomposer _decomposer;// StackDecomposer
    private bool _hasCurrent;
}
