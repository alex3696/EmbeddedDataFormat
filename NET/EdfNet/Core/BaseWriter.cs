namespace EdfNet.Core;

public abstract class BaseWriter : BaseDisposable, IWriter
{
    public readonly Config Cfg;
    public Schema? CurrentSchema;

    protected abstract ushort _DataLen { get; set; }
    protected abstract Span<byte> _DataBuffer { get; }


    public BaseWriter(Config header)
    {
        Cfg = header;
    }
    //protected override void Dispose(bool disposing) => base.Dispose(disposing);

    protected abstract EdfErr TrySrcToX<TEnumerator>(PoType t, ref TEnumerator flatObj, Span<byte> dst, out int w)
        where TEnumerator : struct, IEdfByteEnumerator;

    protected abstract EdfErr WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int skip, ref int wqty, ref int writed);
    protected byte[]? SepBeginStruct = null;
    protected byte[]? SepEndStruct = null;
    protected byte[]? SepBeginArray = null;
    protected byte[]? SepEndArray = null;
    protected byte[]? SepVarEnd = null;
    protected byte[]? SepRecBegin = null;
    protected byte[]? SepRecEnd = null;

    protected int _skip = 0;
    protected int? _currObj = null;

    public abstract void Write(Config cfg);
    public abstract void Write(Schema sch);
    public abstract void Flush();

    public abstract EdfErr Write(object obj);
    public EdfErr WriteEnumerator<TEnumerator>(ref TEnumerator flatObj)
        where TEnumerator : struct, IEdfByteEnumerator
    {
        ArgumentNullException.ThrowIfNull(CurrentSchema);
        Span<byte> dst = _DataBuffer[_DataLen..];
        EdfErr err;
        do
        {
            int skip = _skip;
            int wqty = 0;
            int writed = 0;
            err = WriteSingleValue(CurrentSchema.Type, ref dst, ref flatObj, ref skip, ref wqty, ref writed);
            _DataLen += (ushort)writed;
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
                    _currObj = flatObj.CurrentIndex;
                    break;
                case EdfErr.DstBufOverflow:
                    Flush();
                    dst = _DataBuffer;
                    _skip += wqty;
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
        uint totalElement = inf.GetTotalElements();

        if (PoType.Char == inf.Type)
        {
            if (0 < skip)
            {
                skip--;
                return EdfErr.IsOk;
            }
            if (null == _currObj)
            {
                if (!flatObj.MoveNext())
                    return EdfErr.WrongType;
                _currObj = flatObj.CurrentIndex;
            }
            if (EdfErr.IsOk != (err = TrySrcToX(inf.Type, ref flatObj, dst, out var w)))
            {
                if (EdfErr.DstBufOverflow != err)
                    return err;
                _DataLen += (ushort)writed;
                Flush();
                _DataLen = 0;
                writed = 0;
                dst = _DataBuffer;
                if (EdfErr.IsOk != (err = TrySrcToX(inf.Type, ref flatObj, dst, out w)))
                    return err;
            }
            _currObj = null;
            writed += w;
            wqty++;
            dst = dst.Slice(w);

            if (EdfErr.IsOk != (err = WriteSep(SepVarEnd, ref dst, ref skip, ref wqty, ref writed)))
                return err;
            return err;
        }

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
                if (!flatObj.MoveNext())
                    return EdfErr.SrcDataRequred;
                _currObj = flatObj.CurrentIndex;
            }
            if (EdfErr.IsOk != (err = TrySrcToX(inf.Type, ref flatObj, dst, out var w)))
            {
                if (EdfErr.DstBufOverflow != err)
                    return err;
                _DataLen += (ushort)writed;
                Flush();
                _DataLen = 0;
                writed = 0;
                dst = _DataBuffer;
                if (EdfErr.IsOk != (err = TrySrcToX(inf.Type, ref flatObj, dst, out w)))
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

public static class BaseWriterExt
{
    public static EdfErr WriteInfData(this BaseWriter dw, ushort id, PoType pt, string name, object d)
    {
        dw.Write(new Schema() { Id = id, Type = new(pt), Name = name, });
        ArgumentNullException.ThrowIfNull(dw.CurrentSchema);
        return dw.Write(d);
    }
}


