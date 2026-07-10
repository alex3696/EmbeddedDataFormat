namespace EdfNet.Ref;

public class BinWriter : BaseWriter
{
    private readonly Stream _bw;
    private readonly byte[] _blkBuf;
    private readonly BinBlock _blk;
    private readonly BinDataBlock _blkData;

    private uint _recId = 0;
    private ushort _prmOffset = 0;

    public ushort CurrentDataLen => _blkData.DataLen;
    protected override ushort _DataLen
    {
        get => _blkData.DataLen;
        set => _blkData.DataLen = value;
    }
    protected override Span<byte> _DataBuffer => _blkData.DataBuffer;

    protected override EdfErr TrySrcToX<TEnumerator>(PoType t, ref TEnumerator flatObj, Span<byte> dst, out int w)
    {
        return Primitives.TrySrcToBin(t, ref flatObj, dst, out w);
    }

    protected override EdfErr WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int skip, ref int wqty, ref int writed)
        => EdfErr.IsOk;

    public BinWriter(Stream stream, Config? cfg = default)
        : base(cfg ?? Config.Default)
    {
        _bw = stream;
        _blkBuf = new byte[Cfg.Blocksize];
        _blk = new(_blkBuf);
        _blkData = new(_blkBuf);
        if (0 == stream.Position)
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
        switch (_blk.Type)
        {
            default: break;
            case BlockType.Config:
            case BlockType.Schema:
                if (0 < _blk.ContentLen)
                {
                    _bw.Write(_blk);
                    _blk.Reset();
                }
                break;
            case BlockType.Data:
                if (null != CurrentSchema && 0 != _blkData.DataLen)
                {
                    _bw.Write(_blk);
                    PrepareNewBlock();
                }
                break;
        }
    }
    public override void Write(Config h)
    {
        Flush();
        _blk.Reset();
        _blk.Type = BlockType.Config;
        _blk.Append(h.VersMajor);
        _blk.Append(h.VersMinor);
        _blk.Append(h.Encoding);
        _blk.Append(h.Blocksize);
        _blk.Append((ushort)0);
        _blk.Append(h.Flags);
        ArgumentOutOfRangeException.ThrowIfNotEqual(_blk.ContentLen, 12);
        _bw.Write(_blk);
        _blk.Reset();
    }
    public override void Write(Schema sch)
    {
        Flush();
        _blk.Reset();
        _blk.Type = BlockType.Schema;
        _blk.Append(sch.Id);
        _blk.Append(sch.Name);
        _blk.Append(sch.Desc);
        Append(_blk, sch.Type);
        _bw.Write(_blk);
        _blk.Reset();
        CurrentSchema = sch;
        _blk.Type = BlockType.Data;
        _recId = 0;
        _prmOffset = 0;
        PrepareNewBlock();
    }
    public override EdfErr Write(object obj)
    {
        var enm = new ObjByteEnumerator(obj);
        return WriteEnumerator(ref enm);
    }

    private static void Append(BinBlock blk, EdfType t)
    {
        blk.Append(t.Type);
        if (null != t.Dims && 0 < t.Dims.Length)
        {
            blk.Append((byte)t.Dims.Length);
            for (int i = 0; i < t.Dims.Length; i++)
                blk.Append(t.Dims[i]);
        }
        else
            blk.Append((byte)0);

        blk.Append(t.Name);

        if (PoType.Struct == t.Type && null != t.Childs && 0 < t.Childs.Length)
        {
            blk.Append((byte)t.Childs.Length);
            for (int i = 0; i < t.Childs.Length; i++)
            {
                Append(blk, t.Childs[i]);
            }
        }
    }

    void PrepareNewBlock()
    {
        ArgumentNullException.ThrowIfNull(CurrentSchema);
        _blkData.Clear();
        _blkData.SchemaId = CurrentSchema.Id;
        _blkData.RecordId = _recId;
        _blkData.PrimOffset = _prmOffset;
    }
}
