using EdfNet.Buffers;
using EdfNet.Core.Binary;

namespace EdfNet.Core;

public abstract class BaseWriterBin : BaseDisposable, IWriter
{
    public EdfConfig Cfg { get; }
    public EdfSchema? CurrentSchema;
    protected readonly Stream _stream;
    private readonly byte[] _blkBuf;
    private readonly BinBlock _blk;
    protected readonly BinDataBlock _blkData;
    public ushort CurrentDataLen => _blkData.DataLen;

    public BaseWriterBin(Stream stream, EdfConfig? cfg = default)
    {
        Cfg = cfg ?? EdfConfig.Default;
        _stream = stream;
        _blkBuf = new byte[Cfg.BlockSize];
        _blk = new(_blkBuf);
        _blkData = new(_blkBuf);
        if (0 == stream.Position)
            WriteConfig(Cfg);
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
    public void WriteConfig(EdfConfig cfg)
    {
        Flush();
        _blk.Reset();
        _blk.Type = BlockType.Config;
        var buf = new SpanBufferWriter(_blk.ContentBuffer);
        buf.Append(cfg.VersMajor);
        buf.Append(cfg.VersMinor);
        buf.Append(cfg.Encoding);
        buf.Append(cfg.BlockSize);
        buf.Append((ushort)0);
        buf.Append(cfg.Flags);
        _blk.ContentLen = (ushort)buf.WrittedCount;
        ArgumentOutOfRangeException.ThrowIfNotEqual(_blk.ContentLen, 12);
        _stream.Write(_blk);
        _blk.Reset();
    }
    public virtual void WriteSchema(EdfSchema sch)
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

    public virtual EdfErr WriteValue<T>(in T val)
    {
        return EdfErr.WrongType;
    }
    public EdfErr WriteInfData<T>(ushort id, PoType pt, string name, T d)
    {
        WriteSchema(new EdfSchema() { Id = id, Type = new(pt), Name = name, });
        return WriteValue(d);
    }

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
