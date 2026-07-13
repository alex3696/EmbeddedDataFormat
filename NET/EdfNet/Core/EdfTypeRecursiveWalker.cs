namespace EdfNet.Core;

public class EdfTypeRecursiveWalker
    
{
    protected byte[]? SepBeginStruct = null;
    protected byte[]? SepEndStruct = null;
    protected byte[]? SepBeginArray = null;
    protected byte[]? SepEndArray = null;
    protected byte[]? SepVarEnd = null;
    protected byte[]? SepRecBegin = null;
    protected byte[]? SepRecEnd = null;

    public delegate EdfErr WriteSepDelegate(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int skip, ref int wqty, ref int writed);
    public delegate void FlushDelegate();
    public delegate void SetWritedDelegate(int writed);
    public delegate Span<byte> GetBufDelegate();

    public required WriteSepDelegate WriteSep;
    public required FlushDelegate Flush;
    public required SetWritedDelegate AddWrited;
    public required GetBufDelegate GetBuf;

    public int PrimitiveOffset { get; set; }
    protected int? _currObj = null;

    public EdfErr Walk<TEnumerator>(EdfType et, ref TEnumerator flatObj)
        where TEnumerator : struct, IEdfByteEnumerator
    {
        Span<byte> dst = GetBuf();
        EdfErr err;
        do
        {
            int skip = PrimitiveOffset;
            int wqty = 0;
            int writed = 0;
            err = WriteSingleValue(et, ref dst, ref flatObj, ref skip, ref wqty, ref writed);
            AddWrited(writed);
            switch (err)
            {
                default:
                case EdfErr.WrongType: return err;
                case EdfErr.SrcDataRequred:
                    PrimitiveOffset += wqty;
                    break;
                case EdfErr.IsOk:
                    PrimitiveOffset = 0;
                    if (null == _currObj && !flatObj.MoveNext(et))
                    {
                        return (int)EdfErr.IsOk;
                    }
                    _currObj = flatObj.CurrentIndex;
                    break;
                case EdfErr.DstBufOverflow:
                    Flush();
                    dst = GetBuf();
                    PrimitiveOffset += wqty;
                    err = EdfErr.IsOk;
                    break;
            }
        }
        while (EdfErr.SrcDataRequred != err);
        return err;
    }
    private EdfErr WriteSingleValue<TEnumerator>(EdfType inf, ref Span<byte> dst, ref TEnumerator flatObj, ref int skip, ref int wqty, ref int writed)
        where TEnumerator : struct, IEdfByteEnumerator
    {
        EdfErr err;
        if (EdfErr.IsOk != (err = WriteSep(SepRecBegin, ref dst, ref skip, ref wqty, ref writed)))
            return err;
        if (EdfErr.IsOk != (err = WriteObj(inf, ref dst, ref flatObj, ref skip, ref wqty, ref writed)))
            return err;
        if (EdfErr.IsOk != (err = WriteSep(SepRecEnd, ref dst, ref skip, ref wqty, ref writed)))
            return err;
        return err;
    }
    private EdfErr WriteObj<TEnumerator>(EdfType inf, ref Span<byte> dst, ref TEnumerator flatObj, ref int skip, ref int wqty, ref int writed)
        where TEnumerator : struct, IEdfByteEnumerator
    {
        EdfErr err = EdfErr.IsOk;
        if (PoType.Char == inf.Type)
        {
            if (EdfErr.IsOk != (err = WritePrimitive(inf, ref dst, ref flatObj, ref skip, ref wqty, ref writed)))
                return err;
            if (EdfErr.IsOk != (err = WriteSep(SepVarEnd, ref dst, ref skip, ref wqty, ref writed)))
                return err;
            return EdfErr.IsOk;
        }
        uint totalElement = inf.GetTotalElements();
        if (1 < totalElement)
            if (EdfErr.IsOk != (err = WriteSep(SepBeginArray, ref dst, ref skip, ref wqty, ref writed)))
                return err;
        for (int i = 0; i < totalElement; i++)
        {
            if (EdfErr.IsOk != (err = WriteObjElement(inf, ref dst, ref flatObj, ref skip, ref wqty, ref writed)))
                return err;
        }
        if (1 < totalElement)
            if (EdfErr.IsOk != (err = WriteSep(SepEndArray, ref dst, ref skip, ref wqty, ref writed)))
                return err;
        return err;
    }


    protected static EdfErr TrySrcToX<TEnumerator>(EdfType et, ref TEnumerator flatObj, Span<byte> dst, out int w)
        where TEnumerator : struct, IEdfByteEnumerator
    {
        w = flatObj.Write(dst);
        if (0 > w)
            return EdfErr.DstBufOverflow;
        return EdfErr.IsOk;
    }

    private EdfErr WritePrimitive<TEnumerator>(EdfType inf, ref Span<byte> dst, ref TEnumerator flatObj, ref int skip, ref int wqty, ref int writed)
        where TEnumerator : struct, IEdfByteEnumerator
    {
        EdfErr err = EdfErr.IsOk;
        if (0 < skip)
            skip--;
        else
        {
            if (null == _currObj)
            {
                if (!flatObj.MoveNext(inf))
                    return EdfErr.SrcDataRequred;
                _currObj = flatObj.CurrentIndex;
            }
            if (EdfErr.IsOk != (err = TrySrcToX(inf, ref flatObj, dst, out var w)))
            {
                if (EdfErr.DstBufOverflow != err)
                    return err;
                AddWrited(writed);
                Flush();
                writed = 0;
                dst = GetBuf();
                if (EdfErr.IsOk != (err = TrySrcToX(inf, ref flatObj, dst, out w)))
                    return err;
            }
            _currObj = null;
            writed += w;
            wqty++;
            dst = dst.Slice(w);
        }
        return err;
    }
    private EdfErr WriteObjElement<TEnumerator>(EdfType inf, ref Span<byte> dst, ref TEnumerator flatObj, ref int skip, ref int wqty, ref int writed)
        where TEnumerator : struct, IEdfByteEnumerator
    {
        EdfErr err = EdfErr.IsOk;
        if (PoType.Struct == inf.Type)
        {
            if (inf.Childs != null && 0 != inf.Childs.Length)
            {
                if (EdfErr.IsOk != (err = WriteSep(SepBeginStruct, ref dst, ref skip, ref wqty, ref writed)))
                    return err;
                for (int childIndex = 0; childIndex < inf.Childs.Length; childIndex++)
                {
                    err = WriteObj(inf.Childs[childIndex], ref dst, ref flatObj, ref skip, ref wqty, ref writed);
                    if (EdfErr.IsOk != err)
                        return err;
                }
                if (EdfErr.IsOk != (err = WriteSep(SepEndStruct, ref dst, ref skip, ref wqty, ref writed)))
                    return err;
            }
        }
        else
        {
            if (EdfErr.IsOk != (err = WritePrimitive(inf, ref dst, ref flatObj, ref skip, ref wqty, ref writed)))
                return err;
            if (EdfErr.IsOk != (err = WriteSep(SepVarEnd, ref dst, ref skip, ref wqty, ref writed)))
                return err;
        }
        return err;
    }
}

