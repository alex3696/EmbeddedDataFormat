namespace EdfNet.Core;

public class BinWriter : BaseWriter
{
    private readonly Stream _bw;
    private readonly BinBlock _blk;
    public ushort CurrentDataLen => _blk.DataLen;
    protected override ushort _DataLen
    {
        get => _blk.DataLen;
        set => _blk.DataLen = value;
    }
    protected override Span<byte> _DataBuffer => _blk.DataBuffer;

    protected override EdfErr TrySrcToX(PoType t, object obj, Span<byte> dst, out int w)
        => Primitives.TrySrcToBin(t, obj, dst, out w);
    protected override EdfErr WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int skip, ref int wqty, ref int writed)
        => EdfErr.IsOk;

    public BinWriter(Stream stream, Config? cfg = default)
        : base(cfg ?? Config.Default)
    {
        _bw = stream;
        _blk = new(Cfg.Blocksize);
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
        if (null != CurrentSchema && 0 != _blk.DataLen)
        {
            _bw.Write(_blk);
        }
        _blk.Clear();
    }
    public override void Write(Config h)
    {
        Flush();
        _blk.Type = BlockType.Config;
        _blk.Append(h.VersMajor);
        _blk.Append(h.VersMinor);
        _blk.Append(h.Encoding);
        _blk.Append(h.Blocksize);
        _blk.Append((ushort)0);
        _blk.Append(h.Flags);
        ArgumentOutOfRangeException.ThrowIfNotEqual(_blk.DataLen, 12);
        _bw.Write(_blk);
        _blk.Reset();
    }
    public override void Write(Schema sch)
    {
        Flush();
        _blk.Type = BlockType.Schema;
        _blk.Append(sch.Id);
        _blk.Append(sch.Name);
        _blk.Append(sch.Desc);
        Append(_blk, sch.Type);
        _bw.Write(_blk);
        _blk.Reset();
        CurrentSchema = sch;
        _blk.Type = BlockType.Data;
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
}
