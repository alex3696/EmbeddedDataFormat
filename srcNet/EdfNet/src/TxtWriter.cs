namespace NetEdf.src;

public class TxtWriter : BaseWriter
{
    readonly Stream _st;
    readonly TextWriter _stream;

    public TxtWriter(Stream stream, Header? cfg = null)
        : base(cfg ?? Header.Default)
    {
        _st = stream;
        _stream = new StreamWriter(stream);
        SepBeginStruct = "{"u8.ToArray();
        SepEndStruct = "}"u8.ToArray();
        SepBeginArray = "["u8.ToArray();
        SepEndArray = "]"u8.ToArray();
        SepVarEnd = ";"u8.ToArray();
        SepRecBegin = "\n<= "u8.ToArray();
        SepRecEnd = ">"u8.ToArray();
        Write(Cfg);
    }
    protected override void Dispose(bool disposing)
    {
        Flush();
        base.Dispose(disposing);
    }
    public override void Flush()
    {
        BufFlush();
        _stream.Flush();
        //_tw.Clear();
    }
    protected void Write(string? str) => _stream.Write(str);
    protected void Write(PoType p) => _stream.Write(p.ToString());
    protected static string GetOffset(int noffset)
    {
        string offset = "";
        for (int i = 0; i < noffset; i++)
            offset += "  ";
        return offset;
    }
    public override void Write(Header h)
    {
        Write($"<~ {{version={h.VersMajor}.{h.VersMinor}; bs={h.Blocksize}; encoding={h.Encoding}; flags={(uint)h.Flags}; }} >\n");
        //Write($"// ? - struct @ - data // - comment");
        _currDataType = null;
        _blkQty = 0;
    }
    public override void Write(TypeRec t)
    {
        Write($"\n\n<? {{");
        Write($"{t.Id};\"{t.Name}\"");
        if (!string.IsNullOrEmpty(t.Desc))
            Write($";\"{t.Desc}\"");
        Write($"}} ");
        ToString(t.Inf);
        Write($">");
        _currDataType = t.Inf;
        _blkQty = 0;
    }
    protected void ToString(TypeInf t, int noffset = 0)
    {
        string offset = GetOffset(noffset);
        Write(offset);
        Write(t.Type);
        if (null != t.Dims)
        {
            foreach (var d in t.Dims)
                Write($"[{d}]");
        }
        Write($" \"{t.Name}\"");
        if (PoType.Struct==t.Type && null != t.Items && 0 < t.Items.Length)
        {
            Write($"\n{offset}{{");
            foreach (var it in t.Items)
                ToString(it, noffset + 1);
            Write($"\n{offset}}}");
        }
        Write(";");
    }


    BlockType _blkType;
    ushort _blkQty;
    readonly byte[] _blkData = new byte[256];

    private int _skip = 0;
    private object? _currObj = null;
    public static EdfErr WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int skip, ref int wqty, ref int writed)
    {
        if (0 < skip)
        {
            skip--;
            return EdfErr.IsOk;
        }
        if (0 == src.Length)
        {
            wqty++;
            return EdfErr.IsOk; 
        }
        if (src.Length > dst.Length)
            return EdfErr.DstBufOverflow;
        src.CopyTo(dst);
        wqty++;
        writed += src.Length;
        dst = dst.Slice(src.Length);
        return EdfErr.IsOk;
    }
    private void BufFlush()
    {
        _stream.Flush();
        _st.Write(_blkData.AsSpan(0, _blkQty));
        _blkQty = 0;
        //_tw.Clear();
    }
    public readonly byte[]? SepBeginStruct = null;
    public readonly byte[]? SepEndStruct = null;
    public readonly byte[]? SepBeginArray = null;
    public readonly byte[]? SepEndArray = null;
    public readonly byte[]? SepVarEnd = null;
    public readonly byte[]? SepRecBegin = null;
    public readonly byte[]? SepRecEnd = null;

    public override int Write(object obj)
    {
        ArgumentNullException.ThrowIfNull(_currDataType);
        IEnumerator<object> flatObj = new PrimitiveDecomposer(obj).GetEnumerator();
        _blkType = BlockType.VarData;
        Span<byte> dst = _blkData.AsSpan(_blkQty);
        EdfErr err = EdfErr.IsOk;
        do
        {
            int skip = _skip;
            int wqty = 0;
            int writed = 0;
            err = WriteObj(_currDataType, dst, flatObj, ref skip, ref wqty, ref writed);
            _blkQty += (ushort)writed;
            dst = dst.Slice(writed);
            switch (err)
            {
                default:
                case EdfErr.WrongType: return (int)err;
                case EdfErr.SrcDataRequred:
                    _skip += wqty;
                    break;
                case EdfErr.IsOk:
                    throw new NotImplementedException();
                    if (null == _currObj || !flatObj.MoveNext())
                    {
                        _skip = 0;
                        return (int)EdfErr.IsOk;
                    }
                    _currObj = flatObj.Current;
                    _skip += wqty;
                    dst = _blkData;
                    break;
                case EdfErr.DstBufOverflow:
                    BufFlush();
                    dst = _blkData;
                    _skip += wqty;
                    err = EdfErr.IsOk;
                    break;
            }
        }
        while (EdfErr.SrcDataRequred != err);
        return (int)err;
    }
    private EdfErr WriteObj(TypeInf inf, Span<byte> dst, IEnumerator<object> flatObj, ref int skip, ref int wqty, ref int writed)
    {
        EdfErr err = EdfErr.IsOk;
        if (EdfErr.IsOk != (err = WriteSep(SepRecBegin, ref dst, ref skip, ref wqty, ref writed)))
            return err;
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

        if (EdfErr.IsOk != (err = WriteSep(SepRecEnd, ref dst, ref skip, ref wqty, ref writed)))
            return err;
        return err;
    }
    private EdfErr WriteObjElement(TypeInf inf, Span<byte> dst, IEnumerator<object> flatObj, ref int skip, ref int wqty, ref int writed)
    {
        EdfErr err = EdfErr.IsOk;
        if (PoType.Struct == inf.Type)
        {
            if (inf.Items != null && 0 != inf.Items.Length)
            {
                if (EdfErr.IsOk != (err = WriteSep(SepBeginStruct, ref dst, ref skip, ref wqty, ref writed)))
                    return err;
                for (int childIndex = 0; childIndex < inf.Items.Length; childIndex++)
                {
                    var w = writed;
                    err = WriteObj(inf.Items[childIndex], dst, flatObj, ref skip, ref wqty, ref writed);
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
                if (EdfErr.IsOk != (err = Primitives.TrySrcToTxt(inf.Type, _currObj, dst, out var w)))
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
