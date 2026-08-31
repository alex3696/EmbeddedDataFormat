using EdfNet.Core.Text;

namespace EdfNet.Core;

public class EdfTextReader : BaseDisposable
{
    private EdfConfig _cfg;
    private EdfBlockType _currentBlockType;
    private readonly byte[] _readerBuf;
    private readonly TextStreamReader _bufferedReader;
    protected readonly EdfTokenReader _tokenReader;
    protected readonly TextCircularEdfTypeEnumerator _enum = new();
    protected readonly EdfFormatterOptions _options = EdfFormatterOptions.Default;

    public EdfConfig Cfg => _cfg;
    public EdfSchema? CurrentSchema;

    public EdfTextReader(Stream stream, EdfConfig? cfg = default)
    {
        _cfg = cfg ?? EdfConfig.Default;
        _readerBuf = ArrayPool<byte>.Shared.Rent(1024);
        _bufferedReader = new TextStreamReader(stream, _readerBuf);
        _tokenReader = new(_bufferedReader);
    }
    protected override void Dispose(bool disposing)
    {
        _enum.Dispose();
        ArrayPool<byte>.Shared.Return(_readerBuf);
        base.Dispose(disposing);
    }
    public bool ReadBlock()
    {
        if (!_tokenReader.HasValidToken)
        {
            if (!_tokenReader.MoveNext())
                return false;
        }
        switch (_tokenReader.TokenType)
        {
            case TextTokenType.ConfigBegin: ReadConfig(); break;
            case TextTokenType.SchemaBegin: ReadSchema(); break;
            case TextTokenType.RecBegin: ReadRecord(); break;
            case TextTokenType.BlockEnd: return false;
            default: throw new Exception($"Wrong block Type: {_tokenReader.TokenType}");
        }
        return true;
    }
    public EdfBlockType GetBlockType() => _currentBlockType;

    private void ReadConfig()
    {
        try
        {
            _currentBlockType = EdfBlockType.Config;
            _cfg = _tokenReader.TryReadConfig();
        }
        catch (EdfParseException ex)
        {
            throw new AggregateException($"Config block parse error", ex);
        }
    }
    private void ReadSchema()
    {
        try
        {
            _currentBlockType = EdfBlockType.Schema;
            CurrentSchema = TextEdfSchemaSerializer.ReadSchema(_tokenReader);
            _enum.Reset(CurrentSchema.Type);
        }
        catch (EdfParseException ex)
        {
            throw new AggregateException($"Schema block parse error", ex);
        }
    }
    private void ReadRecord()
    {
        _currentBlockType = EdfBlockType.Data;
    }

    public T ReadValue<T>()
    {
        IFormatter<T> formatter = EdfFormatterProvider<T>.Formatter;
        EdfFormatterNotRegistredException.ThrowIfNull(formatter);
        var reader = new BufReaderTxt(_tokenReader, _enum);
        var result = formatter.Deserialize(ref reader, _options);
        return result;
    }
}
