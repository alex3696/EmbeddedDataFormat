namespace EdfNet.Ref;

public class RecursiveReaderBin : IPrimitiveIo
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

    public RecursiveReaderBin(BinDataBlock blk, Stream stream)
    {
        _blk = blk;
        _stream = stream;
    }

    public EdfErr DoRead(EdfType edfType, out object obj, Type type)
    {
        obj = 1; return EdfErr.IsOk;
        /*
        _decomposer = new PrimitiveDecomposer(obj);
        _decomposerEnum = _decomposer.GetEnumerator();
        _hasCurrent = false;
        do
        {
            try
            {
                EdfTypeWalker.Process(edfType, this);
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
            if (_decomposerEnum.MoveNext())
            {
                _hasCurrent = true;
            }
            else
                return EdfErr.IsOk;
        }
        while (true);
        */
    }

    public void Primitive(EdfType edfType)
    {
        if (0 < Skip)
        {
            Skip--;
            return;
        }
        ArgumentNullException.ThrowIfNull(_decomposer, nameof(_decomposer));
        ArgumentNullException.ThrowIfNull(_decomposerEnum, nameof(_decomposerEnum));
        ArgumentNullException.ThrowIfNull(edfType, nameof(edfType));

        _decomposer.DstType = edfType;
        if (!_hasCurrent)
        {
            if (!_decomposerEnum.MoveNext())
                throw new EdfSrcDataRequredException();
            _hasCurrent = true;
        }
        var obj = _decomposerEnum.Current;
        ArgumentNullException.ThrowIfNull(obj, nameof(obj));

        int retry = 1;
        do
        {
            Span<byte> dst = _blk.GetEmptyBuffer();
            var len = PrimitiveWritersBin.TryWrite(dst, edfType, obj);
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
    private PrimitiveDecomposer? _decomposer;
    private IEnumerator<object>? _decomposerEnum;
    private bool _hasCurrent;
}
