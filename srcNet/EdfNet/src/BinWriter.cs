namespace NetEdf.src;

public class BinWriter : BaseBlockWriter
{
    public ushort CurrentQty => _current.Qty;


    readonly Stream _bw;
    BinBlock _current;


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
        _current.Add(h.ToBytes());
        //_current.Write(_bw);
        Flush();
    }
    public void WriteInfo(TypeRec t)
    {
        Flush();
        _current.Type = BlockType.VarInfo;
        var ms = new MemoryStream(_current._data);
        _current.Qty += (ushort)Primitives.SrcToBin(PoType.UInt32, t.Id, ms);
        _current.Qty += (ushort)Write(t.Inf, ms);
        _current.Qty += (ushort)Primitives.SrcToBin(PoType.String, t.Name ?? string.Empty, ms);
        _current.Qty += (ushort)Primitives.SrcToBin(PoType.String, t.Desc ?? string.Empty, ms);
        _currDataType = t.Inf;
        Flush();
    }


    int _skip = 0;
    int _wqty = 0;
    public int Write(TypeInf t, object obj)
    {
        _current.Type = BlockType.VarData;
        int writed = 0;
        var wr = WriteObj(t, _current._data, obj, ref _skip, ref _wqty, ref writed);
        if(0 < wr)
        {
            Flush();
        }
        _current.Qty += (ushort)writed;


        return wr;
    }
    static int WriteObj(TypeInf inf, Span<byte> dst, object? obj, ref int skip, ref int wqty, ref int writed)
    {
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
                var ret = WriteObjElement(inf, dst, obj, ref skip, ref wqty, ref writed);
                if (0 != ret)
                    return ret;
                return -1;
            }
            for (int i = 0; i < totalElement; i++)
            {
                var ret = WriteObjElement(inf, dst, arr.GetValue(i), ref skip, ref wqty, ref writed);
                if (0 != ret)
                    return ret;
            }
            if (0 != WriteSep(SepEndArray, ref dst, ref writed))
                return 1;
        }
        else
        {
            var ret = WriteObjElement(inf, dst, obj, ref skip, ref wqty, ref writed);
            if (0 != ret)
                return ret;
        }
        return 0;
    }

    static int WriteObjElement(TypeInf inf, Span<byte> dst, object? obj, ref int skip, ref int wqty, ref int writed)
    {
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
                    int w = 0;
                    var wr = WriteObj(inf.Items[childIndex], dst, props[childIndex].GetValue(obj), ref skip, ref wqty, ref w);
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
                int wr = Primitives.SrcToBin(inf.Type, obj, dst, ref w);
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





    public static int WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int writed) => 0;
    public static byte[]? SepBeginStruct = null;
    public static byte[]? SepEndStruct = null;
    public static byte[]? SepBeginArray = null;
    public static byte[]? SepEndArray = null;
    public static byte[]? SepVarEnd = null;
    public static byte[]? SepRecBegin = null;
    public static byte[]? SepRecEnd = null;


    public static long Write(Header h, Stream dst)
    {
        Span<byte> b = stackalloc byte[16];
        b[0] = h.VersMajor;
        b[1] = h.VersMinor;
        BinaryPrimitives.WriteUInt16LittleEndian(b.Slice(2, sizeof(UInt16)), h.Encoding);
        BinaryPrimitives.WriteUInt16LittleEndian(b.Slice(4, sizeof(UInt16)), h.Blocksize);
        BinaryPrimitives.WriteUInt32LittleEndian(b.Slice(6, sizeof(UInt32)), (UInt32)h.Flags);
        return b.Length;
    }
    public static long Write(TypeInf inf, Stream dst)
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


    public override void WriteVarInfo(TypeInf t)
    {
        Flush();
        _currDataType = t;
        _current.Type = BlockType.VarInfo;
        _current.Add(t.ToBytes());
        Flush();
    }

    public override void WriteVarData(ReadOnlySpan<byte> b)
    {
        throw new NotImplementedException();
    }
}
