namespace NetEdf.src;

public class BinWriter : BaseBlockWriter
{
    readonly Stream _bw;
    BinBlock _current;

    readonly StructWriter _dw;

    public BinWriter(Stream stream, Header cfg, StructWriter.WritePrimitivesFn fn)
        : base(cfg)
    {
        _dw = new StructWriter(fn);
        _bw = stream;
        _current = new BinBlock(0, new byte[_cfg.Blocksize], 0);
        Write(cfg);
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
        _dw.Clear();
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
    public int Write(TypeInf t, object obj)
    {
        Flush();
        _current.Type = BlockType.VarData;
        var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [];


        return 1;
    }
    int WriteObj(TypeInf inf, Span<byte> dst, object obj, ref int skip, ref int wqty)
    {
        int writed = 0;
        /*
        uint totalElement = 1;
        for (int i = 0; i < inf.Dims?.Length; i++)
            totalElement *= inf.Dims[i];

        if (0 == skip && 1 < totalElement)
            WriteSep(SepBeginArray, ref dst, ref writed);
        for (int i = 0; i < totalElement; i++)
        {
            if (PoType.Struct == inf.Type)
            {
                if (inf.Items != null && 0 != inf.Items.Length)
                {
                    if (0 == skip)
                        WriteSep(SepBeginStruct, ref dst, ref writed);
                    foreach (var s in inf.Items)
                    {
                        var wr = WriteObj(s, ref src, ref dst, ref skip, ref wqty, ref readed, ref writed);
                        if (0 != wr)
                            return wr;
                    }
                    if (0 == skip)
                        WriteSep(SepEndStruct, ref dst, ref writed);
                }
            }
            else
            {
                if (0 < skip)
                {
                    skip--;
                    wqty++;
                    continue;
                }
                int wr = _writePrimitives(inf.Type, src, dst, out int r, out int w);
                if (0 == wr)
                {
                    wqty++;
                    readed += r;
                    writed += w;
                    src = src.Slice(r);
                    dst = dst.Slice(w);
                    WriteSep(SepVar, ref dst, ref writed);
                }
                else
                    return wr;
            }
        }
        if (0 == skip && 1 < totalElement)
            WriteSep(SepEndArray, ref dst, ref writed);
        */
        return 0;
    }
    public int WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int writed) => 0;
    public byte[]? SepBeginStruct = null;
    public byte[]? SepEndStruct = null;
    public byte[]? SepBeginArray = null;
    public byte[]? SepEndArray = null;
    public byte[]? SepVar = null;
    public byte[]? SepRecBegin = null;
    public byte[]? SepRecEnd = null;


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

    public override void WriteVarData(ReadOnlySpan<byte> src)
    {
        if (null != _currDataType && 0 < src.Length)
        {
            _current.Type = BlockType.VarData;
            int ret, r, w;
            int readed = 0;
            int writed = 0;
            Span<byte> dst = _current.EmptySpan;
            do
            {
                ret = _dw.WriteMultipleValues(_currDataType, src, dst, out r, out w);
                src = src.Slice(r);
                dst = dst.Slice(w);
                _current.Qty += (ushort)w;
                readed += r;
                writed += w;
                if (0 < ret)
                {
                    _current.Write(_bw);
                    _current.Clear();
                    dst = _current.EmptySpan;
                }
            }
            while (0 <= ret);
        }
    }

}
