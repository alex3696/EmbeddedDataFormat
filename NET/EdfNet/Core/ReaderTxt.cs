namespace EdfNet.Core;

public class ReaderTxt
{
    protected Config _cfg;
    private BlockType _currentBlockType;
    private readonly StreamBufferedReader _bufferedReader;
    private readonly CircularEdfTypeEnumeratorTxt _enum = new();
    protected readonly EdfOptions _options = EdfOptions.Default;

    public Config Cfg => _cfg;
    public Schema? CurrentSchema;

    public ReaderTxt(Stream stream, Config? cfg = default)
    {
        _bufferedReader = new StreamBufferedReader(stream, 1024);
        _cfg = cfg ?? Config.Default;
    }

    public bool ReadBlock()
    {
        EdfTokenizer tokenizer = new(_bufferedReader);
        var token = tokenizer.Peek();
        switch (token.Type)
        {
            case TextTokenType.ConfigBegin: ReadConfig(ref tokenizer); break;
            case TextTokenType.SchemaBegin: ReadSchema(ref tokenizer); break;
            case TextTokenType.RecBegin: ReadRecord(); break;
            case TextTokenType.BlockEnd: return false;
            default: throw new Exception($"Wrong block Type: {token.Type}");
        }
        return true;
    }
    public BlockType GetBlockType() => _currentBlockType;

    private void ReadConfig(ref EdfTokenizer tokenizer)
    {
        try
        {
            _currentBlockType = BlockType.Config;
            _cfg = ConfigParser.Parse(tokenizer);
        }
        catch (EdfParseException ex)
        {
            throw new Exception($"Ошибка чтения блока конфигурации: {ex.Message}", ex);
        }
    }
    private void ReadSchema(ref EdfTokenizer tokenizer)
    {
        try
        {
            _currentBlockType = BlockType.Schema;
            CurrentSchema = EdfTypeParser.ParseSchema(ref tokenizer);
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
        EdfTokenizer tokenizer = new(_bufferedReader);
        //tokenizer.Expect(TextTokenType.RecBegin);
        IFormatter<T> formatter = EdfProvider<T>.Formatter ?? throw GetException(typeof(T));
        var reader = new BufReaderTxt(_bufferedReader, _enum);
        var result = formatter.Deserialize(ref reader, _options);
        tokenizer.Expect(TextTokenType.BlockEnd);
        return result;
    }


    private static InvalidOperationException GetException(Type type)
    {
        return new InvalidOperationException($"Тип {type.FullName} не зарегистрирован в системе сериализации.");
    }
}
