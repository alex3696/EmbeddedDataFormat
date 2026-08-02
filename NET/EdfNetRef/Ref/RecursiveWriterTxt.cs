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
        _decomposer = new PrimitiveDecomposer();
    }

    public EdfErr DoWrite(EdfType edfType, object obj)
    {
        _decomposer.Reset(obj);
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

        _decomposer.DstType = edfType;
        var len = _decomposer.WriteTxt(_stream);
        if (0 > len)
            throw new EdfDstBufOverflowException();
        _hasCurrent = false;
        BytesWritted += (ushort)len;
        PrimitivesWritted++;
    }


    private readonly Stream _stream;
    private readonly PrimitiveDecomposer _decomposer;
    private bool _hasCurrent;
}
