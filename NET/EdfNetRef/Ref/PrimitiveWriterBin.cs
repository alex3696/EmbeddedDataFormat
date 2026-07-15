namespace EdfNet.Ref;

public class PrimitiveWriterBin : IPrimitiveIo
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
    public PrimitiveWriterBin(BinDataBlock blk, Stream dstStream)
    {
        _blk = blk;
        _stream = dstStream;
    }

    public EdfErr DoWrite(EdfType edfType, object obj)
    {
        _decomposer = new PrimitiveDecomposer(obj);
        _decomposerEnum = _decomposer.GetEnumerator();
        try
        {
            _walker.Process(edfType, this);
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
        PrimitivesWritted = 0;
        Skip = 0;
        return EdfErr.IsOk;
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
        if (!_decomposerEnum.MoveNext())
            throw new EdfSrcDataRequredException();
        var obj = _decomposerEnum.Current;
        ArgumentNullException.ThrowIfNull(obj, nameof(obj));

        int retry = 1;
        do
        {
            Span<byte> dst = _blk.GetEmptyBuffer();
            var len = PrimitiveWritersBin.TryWriteCurrentPrimitive(dst, edfType, obj);
            if (0 > len)
            {
                _stream.Write(_blk);
                _blk.DataLen = 0;
                continue;
            }
            _blk.DataLen += (ushort)len;
            BytesWritted += (ushort)len;
            PrimitivesWritted++;
            return;
        }
        while (0 < retry);
        throw new EdfDstBufOverflowException();
    }


    private readonly EdfTypeWalker _walker = new();
    private readonly BinDataBlock _blk;

    private PrimitiveDecomposer? _decomposer;
    private IEnumerator<object>? _decomposerEnum;
    private readonly Stream _stream;
}
