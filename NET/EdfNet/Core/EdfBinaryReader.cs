using EdfNet.Core.Binary;

namespace EdfNet.Core;

public class EdfBinaryReader : BaseDisposable
{
    public EdfConfig Cfg { get; }
    public EdfSchema? CurrentSchema;
    protected readonly Stream _stream;
    private readonly byte[] _blkBuf;
    protected readonly BinBlock _blk;
    protected readonly BufStateBin _state;
    protected readonly EdfFormatterOptions _options = EdfFormatterOptions.Default;


    public EdfBinaryReader(Stream stream, EdfConfig? cfg = default)
    {
        _stream = stream;
        Cfg = cfg ?? EdfConfig.Default;
        var tmpBuf = ArrayPool<byte>.Shared.Rent(32);
        _blk = new(tmpBuf);
        if (ReadBlock())
        {
            var newCfg = ReadConfig();
            if (newCfg != null)
                Cfg = newCfg;
        }
        ArrayPool<byte>.Shared.Return(tmpBuf);
        _blkBuf = ArrayPool<byte>.Shared.Rent(Cfg.BlockSize);
        _blk = new(_blkBuf);
        _state = new BufStateBin(stream, _blk);
    }
    protected override void Dispose(bool disposing)
    {
        ArrayPool<byte>.Shared.Return(_blkBuf);
        _state.Dispose();
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
    public ReadOnlySpan<byte> GetBlockData() => _blk.CurrentData;

    public int DataAvailable => _state.ReadAvailableLen;

    public EdfConfig? ReadConfig() => _blk.TryReadConfig();
    public EdfSchema? ReadSchema()
    {
        CurrentSchema = _blk.TryReadSchema();
        _state.Readed = 0;
        if (CurrentSchema?.Type != null)
        {
            _state.Enum.Reset(CurrentSchema.Type);
            OnSchemaBlockRead();
        }
        return CurrentSchema;
    }

    protected virtual void OnSchemaBlockRead()
    {

    }
    protected virtual void OnReadDatBlockStart()
    {
        _state.Readed = 0;
        BinaryBlockSequenceException.ThrowIfNotEqualSchemaId(_blk.SchemaId, CurrentSchema?.Id ?? ushort.MaxValue);
        BinaryBlockSequenceException.ThrowIfNotEqualRecordId(_blk.RecordId, _state.Enum.RecordId);
        BinaryBlockSequenceException.ThrowIfNotEqualPrimOffset(_blk.PrimOffset, _state.Enum.PrimOffset);
    }

    public T ReadValue<T>()
    {
        ObjectDisposedException.ThrowIf(IsDisposed, this);
        ArgumentNullException.ThrowIfNull(CurrentSchema, nameof(CurrentSchema));
        IFormatter<T> formatter = EdfFormatterProvider<T>.Formatter;
        EdfFormatterNotRegistredException.ThrowIfNull(formatter);
        var reader = new BufReaderBin(_state);
        return formatter.Deserialize(ref reader, _options);
    }
}
