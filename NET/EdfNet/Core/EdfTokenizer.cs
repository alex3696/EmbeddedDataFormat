namespace EdfNet.Core;

public enum TextTokenType
{
    EOF,
    Identifier,
    StringLiteral,
    Number,
    StructBegin,    // {
    StructEnd,      // }
    ArrayBegin,     // [
    ArrayEnd,       // ]
    VarEnd,         // ;   — терминатор только для примитивов
    ConfigBegin,    // <~
    SchemaBegin,    // <?
    RecBegin,       // <=
    BlockEnd,       // >
}

public readonly record struct TextToken(TextTokenType Type, string Text, int Line, int Column);

/// <summary>
/// Токенизатор EDF текстового формата.
/// полагаясь на упредительное чтение StreamBufferedReader.
/// </summary>
public ref struct EdfTokenizer
{
    public const int NumberTokenMaxLength = 64;
    public const int StringTokenMaxLength = 258; // "..[255char]..";
    public const int IdentifierTokenMaxLength = 64;

    private readonly IBufferedReader _reader;
    private int _line = 1;
    private int _column = 1;
    private TextToken _current;
    private bool _hasCurrent;

    public EdfTokenizer(IBufferedReader reader)
    {
        _reader = reader;
    }

    public void Reset()
    {
        _line = 1;
        _column = 1;
        _hasCurrent = false;
    }

    public TextToken Peek()
    {
        if (!_hasCurrent)
        {
            _current = ReadNextCore();
            _hasCurrent = true;
        }
        return _current;
    }

    public TextToken Consume()
    {
        var token = Peek();
        _hasCurrent = false;
        return token;
    }

    public TextToken Expect(TextTokenType type)
    {
        var token = Consume();
        if (token.Type != type)
            throw new EdfParseException(
                $"Expected {Describe(type)} but got {Describe(token.Type)} '{token.Text}'",
                token.Line, token.Column);
        return token;
    }

    // -----------------------------------------------------------------
    //  Продвижение с подсчетом Line/Column
    // -----------------------------------------------------------------
    private void Advance(int count)
    {
        var buf = _reader.GetSpan(count);
        int actual = Math.Min(count, buf.Length);
        for (int i = 0; i < actual; i++)
        {
            if (buf[i] == (byte)'\n') { _line++; _column = 1; }
            else _column++;
        }
        _reader.Advance(actual);
    }

    // -----------------------------------------------------------------
    //  Основной цикл чтения токена
    // -----------------------------------------------------------------
    private TextToken ReadNextCore()
    {
        SkipWhitespaceAndComments();

        var buf = _reader.GetSpan(2);
        if (buf.Length == 0)
            return new TextToken(TextTokenType.EOF, "", _line, _column);

        int startLine = _line;
        int startCol = _column;
        byte b = buf[0];

        switch (b)
        {
            case (byte)'<': // Блоковые маркеры: <~ <? <=
                if (buf.Length >= 2)
                {
                    switch (buf[1])
                    {
                        default: break;
                        case (byte)'~': Advance(2); return new(TextTokenType.ConfigBegin, "<~", startLine, startCol);
                        case (byte)'?': Advance(2); return new(TextTokenType.SchemaBegin, "<?", startLine, startCol);
                        case (byte)'=': Advance(2); return new(TextTokenType.RecBegin, "<=", startLine, startCol);
                    }
                }
                throw new EdfParseException("Unexpected '<'", startLine, startCol);
            case (byte)'>': Advance(1); return new TextToken(TextTokenType.BlockEnd, ">", startLine, startCol);
            case (byte)'{': Advance(1); return new TextToken(TextTokenType.StructBegin, "{", startLine, startCol);
            case (byte)'}': Advance(1); return new TextToken(TextTokenType.StructEnd, "}", startLine, startCol);
            case (byte)'[': Advance(1); return new TextToken(TextTokenType.ArrayBegin, "[", startLine, startCol);
            case (byte)']': Advance(1); return new TextToken(TextTokenType.ArrayEnd, "]", startLine, startCol);
            case (byte)';': Advance(1); return new TextToken(TextTokenType.VarEnd, ";", startLine, startCol);
            case (byte)'"': return ReadStringLiteral(startLine, startCol);
            default:
                if (IsAsciiDigit(b) || (b == (byte)'-' && buf.Length > 1 && IsAsciiDigit(buf[1])))
                    return ReadNumber(startLine, startCol);
                if (IsAsciiLetter(b) || b == (byte)'_')
                    return ReadIdentifier(startLine, startCol);
                throw new EdfParseException($"Unexpected character '{(char)b}'", startLine, startCol);
        }
    }

    // -----------------------------------------------------------------
    //  StringLiteral
    // -----------------------------------------------------------------
    private TextToken ReadStringLiteral(int startLine, int startCol)
    {
        Advance(1); // skip opening quote
        var buf = _reader.GetSpan(StringTokenMaxLength);
        int quoteIdx = buf.IndexOf((byte)'"');
        if (quoteIdx < 0)
            throw new EdfParseException("Unterminated string literal", startLine, startCol);
        string str = Encoding.UTF8.GetString(buf.Slice(0, quoteIdx));
        Advance(quoteIdx + 1); // content + closing quote
        return new TextToken(TextTokenType.StringLiteral, str, startLine, startCol);
    }

    // -----------------------------------------------------------------
    //  Number: [-]digits[.digits] 
    // -----------------------------------------------------------------
    private TextToken ReadNumber(int startLine, int startCol)
    {
        var buf = _reader.GetSpan(NumberTokenMaxLength);
        int len = 0;
        if (buf[len] == (byte)'-') len++;
        while (len < buf.Length && IsAsciiDigit(buf[len])) len++;
        if (len < buf.Length && buf[len] == (byte)'.')
        {
            len++;
            while (len < buf.Length && IsAsciiDigit(buf[len])) len++;
        }
        string num = Encoding.UTF8.GetString(buf.Slice(0, len));
        Advance(len);
        return new TextToken(TextTokenType.Number, num, startLine, startCol);
    }

    // -----------------------------------------------------------------
    //  Identifier
    // -----------------------------------------------------------------
    private TextToken ReadIdentifier(int startLine, int startCol)
    {
        var buf = _reader.GetSpan(IdentifierTokenMaxLength);
        int len = 0;
        while (len < buf.Length && IsAsciiLetterOrDigitOrUnderscore(buf[len])) len++;
        string ident = Encoding.UTF8.GetString(buf.Slice(0, len));
        Advance(len);
        return new TextToken(TextTokenType.Identifier, ident, startLine, startCol);
    }

    // -----------------------------------------------------------------
    //  Пропуск whitespace и комментариев
    // -----------------------------------------------------------------
    private void SkipWhitespaceAndComments()
    {
        while (true)
        {
            var buf = _reader.GetSpan(2);
            if (buf.Length == 0) break;

            byte b = buf[0];
            if (IsAsciiWhitespace(b))
            {
                Advance(1);
                continue;
            }

            if (buf.Length >= 2 && b == (byte)'/' && buf[1] == (byte)'/')
            {
                Advance(2);
                while (true)
                {
                    var inner = _reader.GetSpan(1);
                    if (inner.Length == 0 || inner[0] == (byte)'\n') break;
                    Advance(1);
                }
                continue;
            }

            if (buf.Length >= 2 && b == (byte)'/' && buf[1] == (byte)'*')
            {
                Advance(2);
                while (true)
                {
                    var inner = _reader.GetSpan(2);
                    if (inner.Length < 2)
                        throw new EdfParseException("Unterminated block comment", _line, _column);
                    if (inner[0] == (byte)'*' && inner[1] == (byte)'/')
                    {
                        Advance(2);
                        break;
                    }
                    Advance(1);
                }
                continue;
            }

            break;
        }
    }

    // -----------------------------------------------------------------
    //  Хелперы
    // -----------------------------------------------------------------
    public static bool IsAsciiWhitespace(byte b) =>
        b is 0x09 or 0x0A or 0x0B or 0x0C or 0x0D or 0x20;

    public static bool IsAsciiLetter(byte b) =>
        (b >= 'A' && b <= 'Z') || (b >= 'a' && b <= 'z');

    public static bool IsAsciiDigit(byte b) =>
        b >= '0' && b <= '9';

    public static bool IsAsciiLetterOrDigitOrUnderscore(byte b) =>
        IsAsciiLetter(b) || IsAsciiDigit(b) || b == (byte)'_';

    private static string Describe(TextTokenType type) => type switch
    {
        TextTokenType.EOF => "end of input",
        TextTokenType.Identifier => "identifier",
        TextTokenType.StringLiteral => "string literal",
        TextTokenType.Number => "number",
        TextTokenType.StructBegin => "'{'",
        TextTokenType.StructEnd => "'}'",
        TextTokenType.ArrayBegin => "'['",
        TextTokenType.ArrayEnd => "']'",
        TextTokenType.VarEnd => "';'",
        TextTokenType.ConfigBegin => "'<~'",
        TextTokenType.SchemaBegin => "'<?'",
        TextTokenType.RecBegin => "'<='",
        TextTokenType.BlockEnd => "'>'",
        _ => type.ToString()
    };
}
