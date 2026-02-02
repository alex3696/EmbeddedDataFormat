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
        _current.Qty += (ushort)EdfWriteBin(t.Id, ms);
        _current.Qty += (ushort)EdfWriteBin(t.Inf, ms);
        _current.Qty += (ushort)EdfBinString.WriteBin(t.Name, ms);
        _current.Qty += (ushort)EdfBinString.WriteBin(t.Desc, ms);
        _currDataType = t.Inf;
        Flush();
    }
    public int WriteBin(TypeInfo t, object obj)
    {
        Flush();
        _current.Type = BlockType.VarData;
        var props = obj.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? [];


        return 1;
    }


    public static long EdfWriteBin(TypeInfo inf, Stream dst)
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
                EdfWriteBin(inf.Items[i], dst);
            }
        }
        return dst.Position - begin;
    }
    public int EdfWriteBin(int val, Stream dst)
    {
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32LittleEndian(buffer, val);
        dst.Write(buffer);
        return sizeof(int);
    }


    public override void WriteVarInfo(TypeInfo t)
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
