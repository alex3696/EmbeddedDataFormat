using System.Collections;

namespace NetEdf.src;

public class BinWriter : BaseWriter
{
    public ushort CurrentQty => _current.Qty;
    private readonly Stream _bw;
    private readonly BinBlock _current;


    private int _skip = 0;
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
        //_current.Clear();
        _current.Type = BlockType.Header;
        var sp = _current._data.AsSpan(0, 16);
        sp.Clear();
        using var ms = new MemoryStream(_current._data);
        using var bs = new BinaryWriter(ms);
        bs.Write(h.VersMajor);
        bs.Write(h.VersMinor);
        bs.Write(h.Encoding);
        bs.Write(h.Blocksize);
        bs.Write((UInt32)h.Flags);
        _current.Qty = (ushort)sp.Length;
        Flush();
    }
    public override void Write(TypeRec t)
    {
        Flush();
        _current.Type = BlockType.VarInfo;
        using var ms = new MemoryStream(_current._data);
        _current.Qty += (ushort)Primitives.SrcToBin(PoType.UInt32, t.Id, ms);
        _current.Qty += (ushort)Write(t.Inf, ms);
        _current.Qty += (ushort)Primitives.SrcToBin(PoType.String, t.Name ?? string.Empty, ms);
        _current.Qty += (ushort)Primitives.SrcToBin(PoType.String, t.Desc ?? string.Empty, ms);
        _currDataType = t.Inf;
        Flush();
    }
    public override int Write(TypeInf t, object obj)
    {
        _current.Type = BlockType.VarData;
        Span<byte> dst = _current._data.AsSpan(_current.Qty);
        EdfErr err = EdfErr.IsOk;
        do
        {
            int skip = _skip;
            int wqty = 0;
            int writed = 0;
            err = WriteObj(t, dst, obj, ref skip, ref wqty, ref writed);
            _current.Qty += (ushort)writed;
            dst.Slice(writed);
            switch (err)
            {
                default:
                case EdfErr.WrongType: return (int)err;
                case EdfErr.SrcDataRequred:
                    _skip += wqty;
                    break;
                case EdfErr.IsOk:
                    if (EdfErr.IsOk != (err = WriteSep(SepRecEnd, ref dst, ref writed)))
                        return (int)err;
                    _skip = 0;
                    return (int)EdfErr.IsOk;
                case EdfErr.DstBufOverflow:
                    Flush();
                    dst = _current._data;
                    _skip += wqty;
                    err = 0;
                    break;
            }
        }
        while (EdfErr.SrcDataRequred != err);
        return (int)err;
    }
    private static EdfErr WriteObj(TypeInf inf, Span<byte> dst, object? obj, ref int skip, ref int wqty, ref int writed)
    {
        EdfErr err = EdfErr.IsOk;
        if (null == obj)
            return EdfErr.SrcDataRequred;
        uint totalElement = 1;
        for (int i = 0; i < inf.Dims?.Length; i++)
            totalElement *= inf.Dims[i];

        if (1 < totalElement)
        {
            if (EdfErr.IsOk != (err = WriteSep(SepBeginArray, ref dst, ref writed)))
                return err;
            if (obj is not Array arr)
            {
                if (EdfErr.IsOk != (err = WriteObjElement(inf, dst, obj, ref skip, ref wqty, ref writed)))
                    return err;
                return EdfErr.SrcDataRequred;
            }
            for (int i = 0; i < totalElement && i < arr.Length; i++)
            {
                var w = writed;
                if (EdfErr.IsOk != (err = WriteObjElement(inf, dst, arr.GetValue(i), ref skip, ref wqty, ref writed)))
                    return err;
                dst = dst.Slice(writed - w);
            }
            if (arr.Length < totalElement)
                return EdfErr.SrcDataRequred;
            if (EdfErr.IsOk != (err = WriteSep(SepEndArray, ref dst, ref writed)))
                return err;
        }
        else
        {
            var w = writed;
            if (EdfErr.IsOk != (err = WriteObjElement(inf, dst, obj, ref skip, ref wqty, ref writed)))
                return err;
            //dst = dst.Slice(writed - w);
        }
        return err;
    }
    private static EdfErr WriteObjElement(TypeInf inf, Span<byte> dst, object? obj, ref int skip, ref int wqty, ref int writed)
    {
        EdfErr err = EdfErr.IsOk;
        if (null == obj)
            return EdfErr.SrcDataRequred;
        if (PoType.Struct == inf.Type)
        {
            if (inf.Items != null && 0 != inf.Items.Length)
            {
                if (EdfErr.IsOk != (err = WriteSep(SepBeginStruct, ref dst, ref writed)))
                    return err;
                var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [];
                for (int childIndex = 0; childIndex < inf.Items.Length; childIndex++)
                {
                    var w = writed;
                    err = WriteObj(inf.Items[childIndex], dst, props[childIndex].GetValue(obj), ref skip, ref wqty, ref writed);
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
                if (EdfErr.IsOk != (err = Primitives.TrySrcToBin(inf.Type, obj, dst, out var w)))
                    return err;
                writed += w;
                wqty++;
                dst = dst.Slice(w);
            }
            if (EdfErr.IsOk != (err = WriteSep(SepVarEnd, ref dst, ref writed)))
                return err;
        }
        return err;
    }
    private static long Write(TypeInf inf, Stream dst)
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

        if (PoType.Struct == inf.Type && null != inf.Items && 0 < inf.Items.Length)
        {
            bw.Write((byte)inf.Items.Length);
            for (int i = 0; i < inf.Items.Length; i++)
            {
                Write(inf.Items[i], dst);
            }
        }
        return dst.Position - begin;
    }

}
