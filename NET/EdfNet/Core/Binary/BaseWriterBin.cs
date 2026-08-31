namespace EdfNet.Core.Binary;

public abstract class BaseWriterBin : BaseDisposable, IWriter
{
    public EdfConfig Cfg { get; }
    public EdfSchema? CurrentSchema;
    protected readonly Stream _stream;
    private readonly byte[] _blkBuf;
    protected readonly BinBlock _blk;
    public ushort CurrentDataLen => _blk.DataLen;

    public BaseWriterBin(Stream stream, EdfConfig? cfg = default)
    {
        Cfg = cfg ?? EdfConfig.Default;
        _stream = stream;
        _blkBuf = new byte[Cfg.BlockSize];
        _blk = new(_blkBuf);
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
            case EdfBlockType.Config:
            case EdfBlockType.Schema:
                if (0 < _blk.ContentLen)
                    _blk.Reset();
                break;
            case EdfBlockType.Data:
                if (null != CurrentSchema && 0 != _blk.DataLen)
                {
                    _stream.Write(_blk);
                    _blk.DataLen = 0;
                }
                break;
        }
    }
    public void WriteConfig(EdfConfig cfg)
    {
        Flush();
        _blk.WriteConfig(cfg);
        _stream.Write(_blk);
        _blk.Reset();
    }
    public virtual void WriteSchema(EdfSchema sch)
    {
        Flush();
        _blk.WriteSchema(sch);
        _stream.Write(_blk);
        _blk.Reset();
        CurrentSchema = sch;
        _blk.Type = EdfBlockType.Data;
        _blk.SchemaId = sch.Id;
        _blk.PrimOffset = 0;
        _blk.RecordId = 0;
        _blk.DataLen = 0;
    }

    public virtual EdfErrorCode WriteValue<T>(in T val)
    {
        return EdfErrorCode.WrongType;
    }
    public EdfErrorCode WriteInfData<T>(ushort id, EdfPrimitiveType pt, string name, T d)
    {
        WriteSchema(new EdfSchema() { Id = id, Type = new(pt), Name = name, });
        return WriteValue(d);
    }



}
