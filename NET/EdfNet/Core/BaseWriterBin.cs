namespace EdfNet.Core;

public abstract class BaseWriterBin : BaseDisposable, IWriter
{
    public Config Cfg { get; }
    public Schema? CurrentSchema;
    protected readonly Stream _stream;
    private readonly byte[] _blkBuf;
    private readonly BinBlock _blk;
    protected readonly BinDataBlock _blkData;
    public ushort CurrentDataLen => _blkData.DataLen;

    public BaseWriterBin(Stream stream, Config? cfg = default)
    {
        Cfg = cfg ?? Config.Default;
        _stream = stream;
        _blkBuf = new byte[Cfg.Blocksize];
        _blk = new(_blkBuf);
        _blkData = new(_blkBuf);
        if (0 == stream.Position)
            Write(Cfg);
    }
    protected override void Dispose(bool disposing)
    {
        Flush();
        _stream.Flush();
        base.Dispose(disposing);
    }
    public void Flush()
    {
        switch (_blk.Type)
        {
            default: break;
            case BlockType.Config:
            case BlockType.Schema:
                if (0 < _blk.ContentLen)
                    _blk.Reset();
                break;
            case BlockType.Data:
                if (null != CurrentSchema && 0 != _blkData.DataLen)
                {
                    _stream.Write(_blk);
                    _blkData.DataLen = 0;
                }
                break;
        }
    }
    public void Write(Config cfg)
    {
        Flush();
        _blk.Reset();
        _blk.Type = BlockType.Config;
        var buf = new SpanBufferWriter(_blk.ContentBuffer);
        buf.Append(cfg.VersMajor);
        buf.Append(cfg.VersMinor);
        buf.Append(cfg.Encoding);
        buf.Append(cfg.Blocksize);
        buf.Append((ushort)0);
        buf.Append(cfg.Flags);
        _blk.ContentLen = (ushort)buf.WrittedCount;
        ArgumentOutOfRangeException.ThrowIfNotEqual(_blk.ContentLen, 12);
        _stream.Write(_blk);
        _blk.Reset();
    }
    public virtual void Write(Schema sch)
    {
        Flush();
        _blk.Reset();
        _blk.Type = BlockType.Schema;
        var buf = new SpanBufferWriter(_blk.ContentBuffer);
        buf.Append(sch.Id);
        buf.Append(sch.Name);
        buf.Append(sch.Desc);
        Append(ref buf, sch.Type);
        _blk.ContentLen = (ushort)buf.WrittedCount;
        _stream.Write(_blk);
        _blk.Reset();
        CurrentSchema = sch;
        _blkData.Type = BlockType.Data;
        _blkData.SchemaId = sch.Id;
        _blkData.PrimOffset = 0;
        _blkData.RecordId = 0;
        _blkData.DataLen = 0;
    }

    public EdfErr Write<T>(T val) where T : class
    {
        return EdfErr.WrongType;
    }
    public EdfErr WriteValue<T>(in T val) where T : struct, allows ref struct
    {
        return EdfErr.WrongType;
    }


    public abstract EdfErr WriteEnumerator<TEnumerator>(ref TEnumerator enumerator)
        where TEnumerator : struct, IEdfByteEnumerator;

    private static void Append(ref SpanBufferWriter buf, EdfType t)
    {
        buf.Append(t.Type);
        if (null != t.Dims && 0 < t.Dims.Length)
        {
            buf.Append((byte)t.Dims.Length);
            for (int i = 0; i < t.Dims.Length; i++)
                buf.Append(t.Dims[i]);
        }
        else
            buf.Append((byte)0);

        buf.Append(t.Name);

        if (PoType.Struct == t.Type && null != t.Childs && 0 < t.Childs.Length)
        {
            buf.Append((byte)t.Childs.Length);
            for (int i = 0; i < t.Childs.Length; i++)
            {
                Append(ref buf, t.Childs[i]);
            }
        }
    }

}
