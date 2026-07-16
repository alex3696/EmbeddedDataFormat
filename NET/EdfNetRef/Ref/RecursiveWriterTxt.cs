namespace EdfNet.Ref;

public class RecursiveWriterTxt : IPrimitiveIo
{
    #region Separators
    private void WriteSep(ReadOnlySpan<byte> src)
    {
        if (0 < Skip)
        {
            Skip--;
            return;
        }
        if (0 < src.Length)
        {
            _stream.Write(src);
            BytesWritted += src.Length;
        }
        PrimitivesWritted++;
    }
    public void SepRecBegin() => WriteSep(Separator.RecBegin);
    public void SepRecEnd() => WriteSep(Separator.RecEnd);
    public void SepBeginStruct() => WriteSep(Separator.BeginStruct);
    public void SepEndStruct() => WriteSep(Separator.EndStruct);
    public void SepBeginArray() => WriteSep(Separator.BeginArray);
    public void SepEndArray() => WriteSep(Separator.EndArray);
    public void SepVarEnd() => WriteSep(Separator.VarEnd);
    #endregion
    public int PrimitivesWritted { get; private set; } = 0;
    public int BytesWritted { get; private set; } = 0;
    public int Skip { get; set; } = 0;
    public RecursiveWriterTxt(Stream dstStream)
    {
        _stream = dstStream;
    }

    public EdfErr DoWrite(EdfType edfType, object obj)
    {
        _decomposer = new PrimitiveDecomposer(obj);
        _decomposerEnum = _decomposer.GetEnumerator();
        _hasCurrent = false;
        do
        {
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
            if (_decomposerEnum.MoveNext())
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

        var len = PrimitiveWritersTxt.TryWrite(_stream, edfType, obj);
        if (0 > len)
            throw new EdfDstBufOverflowException();
        _hasCurrent = false;
        BytesWritted += (ushort)len;
        PrimitivesWritted++;
    }


    private readonly Stream _stream;
    private readonly EdfTypeWalker _walker = new();
    private PrimitiveDecomposer? _decomposer;
    private IEnumerator<object>? _decomposerEnum;
    private bool _hasCurrent;
}
