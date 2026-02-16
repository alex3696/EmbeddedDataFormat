namespace NetEdf.src;

public class BinWriter : BaseWriter
{
    public ushort CurrentQty => _current.Qty;
    private readonly Stream _bw;
    private readonly BinBlock _current;

    private int _skip = 0;
    private object? _currObj = null;
    public static EdfErr WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int writed) => EdfErr.IsOk;
    public const byte[]? SepBeginStruct = null;
    public const byte[]? SepEndStruct = null;
    public const byte[]? SepBeginArray = null;
    public const byte[]? SepEndArray = null;
    public const byte[]? SepVarEnd = null;
    public const byte[]? SepRecBegin = null;
    public const byte[]? SepRecEnd = null;


    public BinWriter(Stream stream, Header? cfg = default)
        : base(cfg ?? Header.Default)
    {
        _bw = stream;
        _current = new BinBlock(0, new byte[Cfg.Blocksize], 0);
        Write(Cfg);
    }
    protected override void Dispose(bool disposing)
    {
        Flush();
        _bw.Flush();
        base.Dispose(disposing);
    }
    public override void Flush()
    {
        _current.Write(_bw);
        _current.Clear();
    }
    public override void Write(Header h)
    {
        Flush();
        _currDataType = null;
        _current._data.AsSpan(0, 16).Clear();
        _current.Type = BlockType.Header;
        _current.Qty += (ushort)Primitives.SrcToBin(_current.EmptySpan, PoType.UInt8, h.VersMajor);
        _current.Qty += (ushort)Primitives.SrcToBin(_current.EmptySpan, PoType.UInt8, h.VersMinor);
        _current.Qty += (ushort)Primitives.SrcToBin(_current.EmptySpan, PoType.UInt16, h.Encoding);
        _current.Qty += (ushort)Primitives.SrcToBin(_current.EmptySpan, PoType.UInt16, h.Blocksize);
        _current.Qty += (ushort)Primitives.SrcToBin(_current.EmptySpan, PoType.UInt32, h.Flags);
        _current.Qty = 16;
        Flush();
    }
    public override void Write(TypeRec t)
    {
        Flush();
        _current.Type = BlockType.VarInfo;
        using var ms = new MemoryStream(_current._data);
        Primitives.SrcToBin(ms, PoType.UInt32, t.Id);
        BinWriter.Write(ms, t.Inf);
        _current.Qty += (ushort)ms.Position;
        _current.Qty += (ushort)Primitives.SrcToBin(_current.EmptySpan, PoType.String, t.Name ?? string.Empty);
        _current.Qty += (ushort)Primitives.SrcToBin(_current.EmptySpan, PoType.String, t.Desc ?? string.Empty);
        _currDataType = t.Inf;
        Flush();
    }
    public override EdfErr Write(object obj)
    {
        ArgumentNullException.ThrowIfNull(_currDataType);
        IEnumerator<object> flatObj = new PrimitiveDecomposer(obj).GetEnumerator();
        _current.Type = BlockType.VarData;
        Span<byte> dst = _current._data.AsSpan(_current.Qty);
        EdfErr err = EdfErr.IsOk;
        do
        {
            int skip = _skip;
            int wqty = 0;
            int writed = 0;
            err = WriteObj(_currDataType, dst, flatObj, ref skip, ref wqty, ref writed);
            _current.Qty += (ushort)writed;
            dst = dst.Slice(writed);
            switch (err)
            {
                default:
                case EdfErr.WrongType: return err;
                case EdfErr.SrcDataRequred:
                    _skip += wqty;
                    break;
                case EdfErr.IsOk:
                    if (EdfErr.IsOk != (err = WriteSep(SepRecEnd, ref dst, ref writed)))
                        return err;
                    if (null == _currObj && !flatObj.MoveNext())
                    {
                        _skip = 0;
                        return (int)EdfErr.IsOk;
                    }
                    _currObj = flatObj.Current;
                    _skip += wqty;
                    dst = _current._data;
                    break;
                case EdfErr.DstBufOverflow:
                    Flush();
                    dst = _current._data;
                    _skip += wqty;
                    err = 0;
                    break;
            }
        }
        while (EdfErr.SrcDataRequred != err);
        return err;
    }
    private EdfErr WriteObj(TypeInf inf, Span<byte> dst, IEnumerator<object> flatObj, ref int skip, ref int wqty, ref int writed)
    {
        EdfErr err = EdfErr.IsOk;
        uint totalElement = inf.GetTotalElements();

        if (1 < totalElement)
            if (EdfErr.IsOk != (err = WriteSep(SepBeginArray, ref dst, ref writed)))
                return err;
        for (int i = 0; i < totalElement; i++)
        {
            var w = writed;
            if (EdfErr.IsOk != (err = WriteObjElement(inf, dst, flatObj, ref skip, ref wqty, ref writed)))
                return err;
            dst = dst.Slice(writed - w);
        }
        if (1 < totalElement)
            if (EdfErr.IsOk != (err = WriteSep(SepEndArray, ref dst, ref writed)))
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
                if (EdfErr.IsOk != (err = WriteSep(SepBeginStruct, ref dst, ref writed)))
                    return err;
                foreach (var childInf in inf.Childs)
                {
                    var w = writed;
                    err = WriteObj(childInf, dst, flatObj, ref skip, ref wqty, ref writed);
                    if (EdfErr.IsOk != err)
                        return err;
                    dst = dst.Slice(writed - w);
                }
                if (EdfErr.IsOk != (err = WriteSep(SepEndStruct, ref dst, ref writed)))
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
                if (EdfErr.IsOk != (err = Primitives.TrySrcToBin(inf.Type, _currObj, dst, out var w)))
                    return err;
                _currObj = null;
                writed += w;
                wqty++;
                dst = dst.Slice(w);
            }
            if (EdfErr.IsOk != (err = WriteSep(SepVarEnd, ref dst, ref writed)))
                return err;
        }
        return err;
    }
    private static long Write(Stream dst, TypeInf inf)
    {
        var begin = dst.Position;
        var bw = new BinaryWriter(dst);
        bw.Write((byte)inf.Type);
        if (null != inf.Dims && 0 < inf.Dims.Length)
        {
            bw.Write((byte)inf.Dims.Length);
            for (int i = 0; i < inf.Dims.Length; i++)
                bw.Write(inf.Dims[i]);
        }
        else
        {
            bw.Write((byte)0);
        }
        EdfBinString.WriteBin(inf.Name, dst);

        if (PoType.Struct == inf.Type && null != inf.Childs && 0 < inf.Childs.Length)
        {
            bw.Write((byte)inf.Childs.Length);
            for (int i = 0; i < inf.Childs.Length; i++)
            {
                Write(dst, inf.Childs[i]);
            }
        }
        return dst.Position - begin;
    }

}
