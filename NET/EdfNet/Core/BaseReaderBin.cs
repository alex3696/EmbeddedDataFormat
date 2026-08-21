namespace EdfNet.Core;

public abstract class BaseReaderBin
{
    public Config Cfg { get; }
    public Schema? CurrentSchema;
    protected readonly Stream _stream;
    private readonly byte[] _blkBuf;
    private readonly BinBlock _blk;
    protected readonly BinDataBlock _blkData;

    public BaseReaderBin(Stream stream, Config? cfg = default)
    {
        _stream = stream;
        Cfg = cfg ?? Config.Default;
        _blk = new BinBlock(new byte[32]);
        if (ReadBlock())
        {
            var newCfg = ReadConfig();
            if (newCfg != null)
                Cfg = newCfg;
        }
        _blkBuf = new byte[Cfg.Blocksize];
        _blk = new(_blkBuf);
        _blkData = new(_blkBuf);
    }
    public bool ReadBlock()
    {
        if (0 < _stream.Read(_blk))
        {
            switch (_blk.Type)
            {
                default: throw new Exception($"Wrong block Type: {_blk.Type}");
                case BlockType.Config: break;
                case BlockType.Schema: ReadSchema(); break;
                case BlockType.Data: OnReadDatBlockStart(); break;
            }
            return true;
        }
        return false;
    }


    public BlockType GetBlockType() => _blk.Type;
    public ushort GetBlockLen() => _blk.TotalLen;
    public ReadOnlySpan<byte> GetBlockData() => _blkData.CurrentData;

    public Config? ReadConfig() => _blk.ReadConfig();
    public Schema? ReadSchema()
    {
        CurrentSchema = _blk.ReadSchema();
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
