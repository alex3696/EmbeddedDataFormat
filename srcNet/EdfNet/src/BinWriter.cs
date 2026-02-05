namespace NetEdf.src;

public class BinWriter : BaseWriter
{
    public ushort CurrentQty => _current.Qty;
    private readonly Stream _bw;
    private readonly BinBlock _current;

    private int _skip = 0;
    private int _wqty = 0;
    public static int WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int writed) => 0;
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
        _current = new BinBlock(0, new byte[_cfg.Blocksize], 0);
        Write(_cfg);
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
        var wr = WriteObj(t, _current._data, obj, ref _skip, ref _wqty, out var writed);
        if (0 < wr)
        {
            Flush();
        }
        _current.Qty += (ushort)writed;


        return wr;
    }
    private static int WriteObj(TypeInf inf, Span<byte> dst, object? obj, ref int skip, ref int wqty, out int writed)
    {
        writed = 0;
        if (null == obj)
            return 0;

        uint totalElement = 1;
        for (int i = 0; i < inf.Dims?.Length; i++)
            totalElement *= inf.Dims[i];

        if (1 < totalElement)
        {
            if (0 != WriteSep(SepBeginArray, ref dst, ref writed))
                return 1;
            if (obj is not Array arr)
            {
                var ret = WriteObjElement(inf, dst, obj, ref skip, ref wqty, out var w);
                if (0 != ret)
                    return ret;
                writed += w;
                return -1;
            }
            for (int i = 0; i < totalElement; i++)
            {
                var ret = WriteObjElement(inf, dst, arr.GetValue(i), ref skip, ref wqty, out var w);
                if (0 != ret)
                    return ret;
                writed += w;
                dst = dst.Slice(w);
            }
            if (0 != WriteSep(SepEndArray, ref dst, ref writed))
                return 1;
        }
        else
        {
            var ret = WriteObjElement(inf, dst, obj, ref skip, ref wqty, out var w);
            if (0 != ret)
                return ret;
            writed += w;
            dst = dst.Slice(w);
        }
        return 0;
    }
    private static int WriteObjElement(TypeInf inf, Span<byte> dst, object? obj, ref int skip, ref int wqty, out int writed)
    {
        writed = 0;
        if (null == obj)
            return 0;
        if (PoType.Struct == inf.Type)
        {
            if (inf.Items != null && 0 != inf.Items.Length)
            {
                if (0 != WriteSep(SepBeginStruct, ref dst, ref writed))
                    return 1;
                var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [];
                for (int childIndex = 0; childIndex < inf.Items.Length; childIndex++)
                {
                    var wr = WriteObj(inf.Items[childIndex], dst, props[childIndex].GetValue(obj), ref skip, ref wqty, out var w);
                    writed += w;
                    if (0 != wr)
                        return wr;
                    dst = dst.Slice(w);
                }
                if (0 != WriteSep(SepEndStruct, ref dst, ref writed))
                    return 1;
            }
        }
        else
        {
            if (0 < skip)
                skip--;
            else
            {
                int w = 0;
                int wr = Primitives.TrySrcToBin(inf.Type, obj, dst, ref w);
                if (0 != wr)
                    return wr;
                wqty++;
                writed += w;
                dst = dst.Slice(w);
            }
            if (0 != WriteSep(SepVarEnd, ref dst, ref writed))
                return 1;
        }
        return 0;
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
