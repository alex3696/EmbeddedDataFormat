namespace EdfNet.Ref;

public class PrimitiveWriterTxt : IPrimitiveIo
{
    #region Separators
    private void WriteSep(ReadOnlySpan<byte> src)
    {
        if (0 < Skip)
        {
            Skip--;
            return;
        }
        if (0 == src.Length)
        {
            PrimitivesWritted++;
            return;
        }
        _stream.Write(src);
        PrimitivesWritted++;
        BytesWritted += src.Length;
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
    public PrimitiveWriterTxt(Stream dstStream)
    {
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

        var len = PrimitiveWritersTxtStream.TryWritePrimitive(_stream, edfType, obj);
        if (0 > len)
            throw new EdfDstBufOverflowException();
        BytesWritted += (ushort)len;
        PrimitivesWritted++;
    }


    private readonly EdfTypeWalker _walker = new();

    private PrimitiveDecomposer? _decomposer;
    private IEnumerator<object>? _decomposerEnum;
    private readonly Stream _stream;
}
