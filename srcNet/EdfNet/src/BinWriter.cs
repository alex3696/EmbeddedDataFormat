namespace NetEdf.src;

public class BinWriter : BaseBlockWriter
{
    readonly Stream _bw;
    BinBlock _current;
    byte _seq;
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
        Write(_current);
        _dw.Clear();
        _current.Clear();
    }
    public int Write(BinBlock bb)
    {
        if (0 == bb.Type || 0 == bb.Qty)
            return 0;

        _bw.WriteByte((byte)bb.Type);
        _bw.WriteByte(_seq++);
        _bw.Write(BitConverter.GetBytes((ushort)bb.Qty));
        _bw.Write(bb.Data);
        return bb.Qty;
    }


    public override void Write(Header h)
    {
        Flush();
        _currDataType = null;
        _current.Type = BlockType.Header;
        _current.Add(h.ToBytes());
        Flush();
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
                    Write(_current);
                    _current.Clear();
                    dst = _current.EmptySpan;
                }
            }
            while (0 <= ret);
        }
    }

}
