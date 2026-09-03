namespace EdfNet.Core.Text;

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

/// <summary>
/// Zero-allocation токен-ридер для EDF текстового формата.
/// TokenValue возвращает ReadOnlySpan<byte> из текущего буфера IBufferedReader.
/// Валиден только до следующего MoveNext() или Advance().
/// </summary>
public class EdfTokenReader
{
    private readonly IBufferedReader _reader;
    private TextTokenType _tokenType;
    private int _advanceLen = -1;   // -1 = нет токена, >=0 = длина токена (включая кавычки для StringLiteral)
    private int _line = 1;
    private int _column = 1;
    private int _tokenLine;
    private int _tokenColumn;

    public EdfTokenReader(IBufferedReader reader)
    {
        _reader = reader;
        _tokenType = TextTokenType.EOF;
    }

    public bool HasValidToken => 0 < _advanceLen;

    public TextTokenType TokenType => _tokenType;
    public int TokenLine => _tokenLine;
    public int TokenColumn => _tokenColumn;

    /// <summary>
    /// Значение текущего токена (без кавычек для StringLiteral).
    /// Валиден только до MoveNext() или Advance().
    /// </summary>
    public ReadOnlySpan<byte> TokenValue
    {
        get
        {
            if (_advanceLen < 0) throw new InvalidOperationException("No current token.");
            var span = _reader.GetSpan(_advanceLen);
            return _tokenType == TextTokenType.StringLiteral
                ? span.Slice(1, _advanceLen - 2)
                : span.Slice(0, _advanceLen);
        }
    }

    public bool MoveNext()
    {
        if (_advanceLen > 0)
            throw new InvalidOperationException("Previous token not consumed. Call Advance() first.");
        if (_advanceLen == 0)
            return false;

        SkipWhitespaceAndComments();

        var buf = _reader.GetSpan(2);
        if (buf.Length == 0)
        {
            _tokenType = TextTokenType.EOF;
            _advanceLen = 0;
            return false;
        }

        _tokenLine = _line;
        _tokenColumn = _column;
        byte b = buf[0];

        switch (b)
        {
            case (byte)'<':
                if (buf.Length >= 2)
                {
                    switch (buf[1])
                    {
                        case (byte)'~': SetToken(TextTokenType.ConfigBegin, 2); return true;
                        case (byte)'?': SetToken(TextTokenType.SchemaBegin, 2); return true;
                        case (byte)'=': SetToken(TextTokenType.RecBegin, 2); return true;
                    }
                }
                throw new EdfParseException("Unexpected '<'", _line, _column);
            case (byte)'>': SetToken(TextTokenType.BlockEnd, 1); return true;
            case (byte)'{': SetToken(TextTokenType.StructBegin, 1); return true;
            case (byte)'}': SetToken(TextTokenType.StructEnd, 1); return true;
            case (byte)'[': SetToken(TextTokenType.ArrayBegin, 1); return true;
            case (byte)']': SetToken(TextTokenType.ArrayEnd, 1); return true;
            case (byte)';': SetToken(TextTokenType.VarEnd, 1); return true;
            case (byte)'"': return ReadStringLiteral();
            default:
                if (IsAsciiDigit(b) || (b == (byte)'-' && buf.Length > 1 && IsAsciiDigit(buf[1])))
                    return ReadNumber();
                if (IsAsciiLetter(b) || b == (byte)'_')
                    return ReadIdentifier();
                throw new EdfParseException($"Unexpected character 0x{b:X2}", _line, _column);
        }
    }

    public void Advance()
    {
        if (_advanceLen < 0) throw new InvalidOperationException("No token to advance. Call MoveNext() first.");
        AdvanceReader(_advanceLen);
        _advanceLen = -1;
    }

    public void Expect(TextTokenType type)
    {
        if (_advanceLen < 0) MoveNext();
        if (_tokenType != type)
            throw new EdfParseException(
                $"Expected {Describe(type)} but got {Describe(_tokenType)}",
                _tokenLine, _tokenColumn);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void ExpectAdvance(TextTokenType type)
    {
        Expect(type);
        Advance();
    }

    /// <summary>
    /// Декодирует текущий StringLiteral в string и съедает токен.
    /// </summary>
    public string GetString()
    {
        if (_tokenType != TextTokenType.StringLiteral)
            throw new InvalidOperationException("Current token is not a string literal");
        string s = Encoding.UTF8.GetString(TokenValue);
        return s;
    }

    // -----------------------------------------------------------------
    //  Внутренние читалки токенов
    // -----------------------------------------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void SetToken(TextTokenType type, int advanceLen)
    {
        _tokenType = type;
        _advanceLen = advanceLen;
    }

    private bool ReadStringLiteral()
    {
        var buf = _reader.GetSpan(256);
        if (buf.Length == 0 || buf[0] != (byte)'"')
            throw new EdfParseException("Expected opening quote", _tokenLine, _tokenColumn);
        int quoteIdx = buf.Slice(1).IndexOf((byte)'"');
        if (quoteIdx < 0)
            throw new EdfParseException("Unterminated string literal", _tokenLine, _tokenColumn);
        // quoteIdx = индекс закрывающей кавычки. Длина = открывающая + контент + закрывающая
        SetToken(TextTokenType.StringLiteral, quoteIdx + 2);
        return true;
    }

    private bool ReadNumber()
    {
        var buf = _reader.GetSpan(64);
        int len = 0;
        if (buf[len] == (byte)'-') len++;
        while (len < buf.Length && IsAsciiDigit(buf[len])) len++;
        if (len < buf.Length && buf[len] == (byte)'.')
        {
            len++;
            while (len < buf.Length && IsAsciiDigit(buf[len])) len++;
        }
        SetToken(TextTokenType.Number, len);
        return true;
    }

    private bool ReadIdentifier()
    {
        var buf = _reader.GetSpan(64);
        int len = 0;
        while (len < buf.Length && IsAsciiLetterOrDigitOrUnderscore(buf[len])) len++;
        SetToken(TextTokenType.Identifier, len);
        return true;
    }

    // -----------------------------------------------------------------
    //  Пропуск whitespace и комментариев
    // -----------------------------------------------------------------
    private void SkipWhitespaceAndComments()
    {
        while (true)
        {
            var buf = _reader.GetSpan(2);
            if (buf.Length == 0) return;

            byte b = buf[0];
            if (IsAsciiWhitespace(b))
            {
                int whitespaceCount = 1;
                while (whitespaceCount < buf.Length && IsAsciiWhitespace(buf[whitespaceCount]))
                    whitespaceCount++;
                AdvanceReader(whitespaceCount);
                continue;
            }
            //var idx = buf.IndexOfAnyExcept(WhitespaceValues);
            //if (0 != idx)
            //{
            //    AdvanceReader(0 < idx ? idx : buf.Length);
            //    continue;
            //}

            if (buf.Length >= 2 && b == (byte)'/' && buf[1] == (byte)'/')
            {
                AdvanceReader(2);
                var rest = _reader.GetSpan(1);
                int nl = rest.IndexOf((byte)'\n');
                if (nl >= 0)
                    AdvanceReader(nl + 1);
                else
                    AdvanceReader(rest.Length);
                continue;
            }

            if (buf.Length >= 2 && b == (byte)'/' && buf[1] == (byte)'*')
            {
                AdvanceReader(2);
                while (true)
                {
                    var rest = _reader.GetSpan(2);
                    if (rest.Length < 2)
                        throw new EdfParseException("Unterminated block comment", _line, _column);
                    int end = rest.IndexOf("*/"u8);
                    if (end >= 0)
                    {
                        AdvanceReader(end + 2);
                        break;
                    }
                    AdvanceReader(rest.Length - 1);
                }
                continue;
            }

            break;
        }
    }

    private void AdvanceReader(int count)
    {
        var span = _reader.GetSpan(count);
        int actual = Math.Min(count, span.Length);
        for (int i = 0; i < actual; i++)
        {
            if (span[i] == (byte)'\n') { _line++; _column = 1; }
            else _column++;
        }
        _reader.Advance(actual);
    }

    // -----------------------------------------------------------------
    //  Публичные хелперы
    // -----------------------------------------------------------------
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiWhitespace(byte b) =>
        b <= 0x20 && ((1UL << b) & 0x0000000100003E00UL) != 0;
    public static bool IsAsciiWhitespace1(byte b) =>
        b is 0x09 or 0x0A or 0x0B or 0x0C or 0x0D or 0x20;
    public static readonly SearchValues<byte> WhitespaceValues =
         SearchValues.Create((ReadOnlySpan<byte>)[0x09, 0x0A, 0x0B, 0x0C, 0x0D, 0x20]);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetter(byte b) =>
        (b >= 'A' && b <= 'Z') || (b >= 'a' && b <= 'z');

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiDigit(byte b) =>
        b >= '0' && b <= '9';

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsAsciiLetterOrDigitOrUnderscore(byte b) =>
        IsAsciiLetter(b) || IsAsciiDigit(b) || b == (byte)'_';

    public static string Describe(TextTokenType type) => type switch
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
