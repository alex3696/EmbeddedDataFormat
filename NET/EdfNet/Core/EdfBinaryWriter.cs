using EdfNet.Core.Binary;

namespace EdfNet.Core;

public class EdfBinaryWriter : BaseDisposable, IWriter
{
    public EdfConfig Cfg { get; }
    public EdfSchema? CurrentSchema;
    protected readonly Stream _stream;
    private readonly byte[] _blkBuf;
    protected readonly BinBlock _blk;
    protected readonly BufStateBin _state;
    protected readonly EdfFormatterOptions _options = EdfFormatterOptions.Default;

    public ushort CurrentDataLen => _blk.DataLen;

    public EdfBinaryWriter(Stream stream, EdfConfig? cfg = default)
    {
        Cfg = cfg ?? EdfConfig.Default;
        _stream = stream;
        _blkBuf = new byte[Cfg.BlockSize];
        _blk = new(_blkBuf);
        if (0 == stream.Position)
            WriteConfig(Cfg);
        _state = new(_stream, _blk);
    }
    protected override void Dispose(bool disposing)
    {
        Flush();
        _stream.Flush();
        _state?.Dispose();
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
    public void WriteSchema(EdfSchema sch)
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
        _state.Enum.Reset(sch.Type);
    }
    public EdfErrorCode WriteValue<T>(in T val)
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(CurrentSchema);
        //IFormatter<T>? formatter = _options.Resolver.GetFormatter<T>();
        IFormatter<T> formatter = EdfFormatterProvider<T>.Formatter;
        EdfFormatterNotRegistredException.ThrowIfNull(formatter);
        var writer = new BufWriterBin(_state);
        formatter.Serialize(ref writer, val, _options);
        return _state.Enum.PrimOffset == 0 ? EdfErrorCode.IsOk : EdfErrorCode.SrcDataRequred;
    }
    public EdfErrorCode WriteInfData<T>(ushort id, EdfPrimitiveType pt, string name, T d)
    {
        WriteSchema(new EdfSchema() { Id = id, Type = new(pt), Name = name, });
        return WriteValue(d);
    }



}
