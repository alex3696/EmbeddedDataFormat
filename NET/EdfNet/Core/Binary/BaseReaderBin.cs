namespace EdfNet.Core.Binary;

public abstract class BaseReaderBin : BaseDisposable
{
    public EdfConfig Cfg { get; }
    public EdfSchema? CurrentSchema;
    protected readonly Stream _stream;
    private readonly byte[] _blkBuf;
    private readonly BinBlock _blk;
    protected readonly BinDataBlock _blkData;

    public BaseReaderBin(Stream stream, EdfConfig? cfg = default)
    {
        _stream = stream;
        Cfg = cfg ?? EdfConfig.Default;
        var tmpBuf = ArrayPool<byte>.Shared.Rent(32);
        _blk = new BinBlock(tmpBuf);
        if (ReadBlock())
        {
            var newCfg = ReadConfig();
            if (newCfg != null)
                Cfg = newCfg;
        }
        ArrayPool<byte>.Shared.Return(tmpBuf);
        _blkBuf = ArrayPool<byte>.Shared.Rent(Cfg.BlockSize);
        _blk = new(_blkBuf);
        _blkData = new(_blkBuf);
    }
    protected override void Dispose(bool disposing)
    {
        ArrayPool<byte>.Shared.Return(_blkBuf);
        base.Dispose(disposing);
    }
    public bool ReadBlock()
    {
        if (0 < _stream.Read(_blk))
        {
            switch (_blk.Type)
            {
                default: throw new Exception($"Wrong block Type: {_blk.Type}");
                case EdfBlockType.Config: break;
                case EdfBlockType.Schema: ReadSchema(); break;
                case EdfBlockType.Data: OnReadDatBlockStart(); break;
            }
            return true;
        }
        return false;
    }

    public EdfBlockType GetBlockType() => _blk.Type;
    public ushort GetBlockLen() => _blk.TotalLen;
    public ReadOnlySpan<byte> GetBlockData() => _blkData.CurrentData;

    public EdfConfig? ReadConfig() => _blk.TryReadConfig();
    public EdfSchema? ReadSchema()
    {
        CurrentSchema = _blk.TryReadSchema();
        OnSchemaBlockRead();
        return CurrentSchema;
    }

    protected virtual void OnSchemaBlockRead()
    {
    }
    protected virtual void OnReadDatBlockStart()
    {
        ArgumentNullException.ThrowIfNull(CurrentSchema, nameof(CurrentSchema));
        //ArgumentOutOfRangeException.ThrowIfNotEqual(_blkData.SchemaId, CurrentSchema.Id, nameof(BinDataBlock.SchemaId));
        //ArgumentOutOfRangeException.ThrowIfNotEqual(_blkData.RecordId, _recId, nameof(BinDataBlock.RecordId));
        //ArgumentOutOfRangeException.ThrowIfNotEqual(_blkData.PrimOffset, _prmOffset, nameof(BinDataBlock.PrimOffset));
    }
}
