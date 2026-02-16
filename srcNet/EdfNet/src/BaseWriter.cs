using NetEdf.Base;

namespace NetEdf.src;

public abstract class BaseWriter : BaseDisposable
{
    public readonly Header Cfg;
    protected TypeInf? _currDataType;

    public TypeInf? CurrDataType => _currDataType;

    public BaseWriter(Header header)
    {
        Cfg = header;
        _blkData = new byte[Cfg.Blocksize];
    }

    //protected override void Dispose(bool disposing) => base.Dispose(disposing);


    protected abstract EdfErr TrySrcToX(PoType t, object obj, Span<byte> dst, out int w);
    protected abstract EdfErr WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int skip, ref int wqty, ref int writed);
    protected byte[]? SepBeginStruct = null;
    protected byte[]? SepEndStruct = null;
    protected byte[]? SepBeginArray = null;
    protected byte[]? SepEndArray = null;
    protected byte[]? SepVarEnd = null;
    protected byte[]? SepRecBegin = null;
    protected byte[]? SepRecEnd = null;

    protected ushort _blkQty;
    protected readonly byte[] _blkData;
    protected Span<byte> _EmptySpan => _blkData.AsSpan(_blkQty);

    protected private int _skip = 0;
    protected private object? _currObj = null;


    public abstract void Write(Header v);
    public abstract void Write(TypeRec t);
    public abstract void Flush();

    public virtual EdfErr Write(object obj)
    {
        ArgumentNullException.ThrowIfNull(_currDataType);
        IEnumerator<object> flatObj = new PrimitiveDecomposer(obj).GetEnumerator();
        Span<byte> dst = _blkData.AsSpan(_blkQty);
        EdfErr err = EdfErr.IsOk;
        do
        {
            int skip = _skip;
            int wqty = 0;
            int writed = 0;
            err = WriteSingleValue(_currDataType, dst, flatObj, ref skip, ref wqty, ref writed);
            _blkQty += (ushort)writed;
            dst = dst.Slice(writed);
            switch (err)
            {
                default:
                case EdfErr.WrongType: return err;
                case EdfErr.SrcDataRequred:
                    _skip += wqty;
                    break;
                case EdfErr.IsOk:
                    _skip = 0;
                    if (null == _currObj && !flatObj.MoveNext())
                    {
                        return (int)EdfErr.IsOk;
                    }
                    _currObj = flatObj.Current;
                    break;
                case EdfErr.DstBufOverflow:
                    Flush();
                    dst = _blkData;
                    _skip += wqty;
                    err = EdfErr.IsOk;
                    break;
            }
        }
        while (EdfErr.SrcDataRequred != err);
        return err;
    }
    private EdfErr WriteSingleValue(TypeInf inf, Span<byte> dst, IEnumerator<object> flatObj, ref int skip, ref int wqty, ref int writed)
    {
        EdfErr err;
        if (EdfErr.IsOk != (err = WriteSep(SepRecBegin, ref dst, ref skip, ref wqty, ref writed)))
            return err;
        var w = writed;
        if (EdfErr.IsOk != (err = WriteObj(inf, dst, flatObj, ref skip, ref wqty, ref writed)))
            return err;
        dst = dst.Slice(writed - w);
        if (EdfErr.IsOk != (err = WriteSep(SepRecEnd, ref dst, ref skip, ref wqty, ref writed)))
            return err;
        return err;
    }
    private EdfErr WriteObj(TypeInf inf, Span<byte> dst, IEnumerator<object> flatObj, ref int skip, ref int wqty, ref int writed)
    {
        EdfErr err = EdfErr.IsOk;
        uint totalElement = inf.GetTotalElements();
        if (1 < totalElement)
            if (EdfErr.IsOk != (err = WriteSep(SepBeginArray, ref dst, ref skip, ref wqty, ref writed)))
                return err;
        for (int i = 0; i < totalElement; i++)
        {
            var w = writed;
            if (EdfErr.IsOk != (err = WriteObjElement(inf, dst, flatObj, ref skip, ref wqty, ref writed)))
                return err;
            dst = dst.Slice(writed - w);
        }
        if (1 < totalElement)
            if (EdfErr.IsOk != (err = WriteSep(SepEndArray, ref dst, ref skip, ref wqty, ref writed)))
                return err;
        return err;
    }
    private EdfErr WriteObjElement(TypeInf inf, Span<byte> dst, IEnumerator<object> flatObj, ref int skip, ref int wqty, ref int writed)
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
                    var w = writed;
                    err = WriteObj(inf.Childs[childIndex], dst, flatObj, ref skip, ref wqty, ref writed);
                    if (EdfErr.IsOk != err)
                        return err;
                    dst = dst.Slice(writed - w);
                }
                if (EdfErr.IsOk != (err = WriteSep(SepEndStruct, ref dst, ref skip, ref wqty, ref writed)))
                    return err;
            }
        }
        else
        {
            if (0 < skip)
                skip--;
            else
            {
                if (null == _currObj)
                {
                    if (!flatObj.MoveNext())
                        return EdfErr.SrcDataRequred;
                    _currObj = flatObj.Current;
                }
                if (EdfErr.IsOk != (err = TrySrcToX(inf.Type, _currObj, dst, out var w)))
                    return err;
                _currObj = null;
                writed += w;
                wqty++;
                dst = dst.Slice(w);
            }
            if (EdfErr.IsOk != (err = WriteSep(SepVarEnd, ref dst, ref skip, ref wqty, ref writed)))
                return err;
        }
        return err;
    }
}

public static class BaseWriterExt
{
    public static void WriteInfData(this BaseWriter dw, UInt32 id, PoType pt, string name, object d)
    {
        dw.Write(new TypeRec() { Id = id, Inf = new(pt), Name = name, });
        ArgumentNullException.ThrowIfNull(dw.CurrDataType);
        dw.Write(d);
    }
}


