using EdfNet.Buffers;
using EdfNet.Core.Text;

namespace EdfNet.Core;

public class ReaderTxt
{
    protected EdfConfig _cfg;
    private BlockType _currentBlockType;
    private readonly StreamBufferedReader _bufferedReader;
    private readonly EdfTokenReader _tokenReader;

    private readonly CircularEdfTypeEnumeratorTxt _enum = new();
    protected readonly Interfaces.EdfOptions _options = Interfaces.EdfOptions.Default;

    public EdfConfig Cfg => _cfg;
    public EdfSchema? CurrentSchema;

    public ReaderTxt(Stream stream, EdfConfig? cfg = default)
    {
        _cfg = cfg ?? EdfConfig.Default;
        _bufferedReader = new StreamBufferedReader(stream, 1024);
        _tokenReader = new(_bufferedReader);
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
    public BlockType GetBlockType() => _currentBlockType;

    private void ReadConfig()
    {
        try
        {
            _currentBlockType = BlockType.Config;
            _cfg = ConfigParser.Parse(_tokenReader);
        }
        catch (EdfParseException ex)
        {
            throw new Exception($"Ошибка чтения блока конфигурации: {ex.Message}", ex);
        }
    }
    private void ReadSchema()
    {
        try
        {
            _currentBlockType = BlockType.Schema;
            CurrentSchema = EdfSchemaParser.ParseBlock(_tokenReader);
            _enum.Reset(CurrentSchema.Type);
        }
        catch (EdfParseException ex)
        {
            throw new Exception($"Ошибка чтения блока схемы: {ex.Message}", ex);
        }
    }
    private void ReadRecord()
    {
        _currentBlockType = BlockType.Data;
    }

    public T ReadValue<T>()
    {
        IFormatter<T> formatter = EdfProvider<T>.Formatter ?? throw GetException(typeof(T));
        var reader = new BufReaderTxt(_tokenReader, _enum);
        var result = formatter.Deserialize(ref reader, _options);
        return result;
    }


    private static InvalidOperationException GetException(Type type)
    {
        return new InvalidOperationException($"Тип {type.FullName} не зарегистрирован в системе сериализации.");
    }
}
