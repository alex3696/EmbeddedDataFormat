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

public ref struct EdfTokenizer
{
    private ReadOnlySpan<byte> _text;
    private int _pos;
    private int _line;
    private int _column;
    private TextToken _current;
    private bool _hasCurrent;

    public EdfTokenizer(ReadOnlySpan<byte> text) => Reset(text);

    public void Reset(ReadOnlySpan<byte> text)
    {
        _text = text;
        _pos = 0;
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

    private TextToken ReadNextCore()
    {
        SkipWhitespaceAndComments();
        if (_pos >= _text.Length)
            return new TextToken(TextTokenType.EOF, "", _line, _column);

        int startLine = _line;
        int startCol = _column;
        byte b = _text[_pos];

        // Блоковые маркеры: <~ <? <=
        if (b == (byte)'<')
        {
            if (_pos + 1 < _text.Length)
            {
                byte next = _text[_pos + 1];
                if (next == (byte)'~')
                {
                    Advance(); Advance();
                    return new TextToken(TextTokenType.ConfigBegin, "<~", startLine, startCol);
                }
                if (next == (byte)'?')
                {
                    Advance(); Advance();
                    return new TextToken(TextTokenType.SchemaBegin, "<?", startLine, startCol);
                }
                if (next == (byte)'=')
                {
                    Advance(); Advance();
                    return new TextToken(TextTokenType.RecBegin, "<=", startLine, startCol);
                }
            }
            throw new EdfParseException($"Unexpected '<'", startLine, startCol);
        }

        switch (b)
        {
            case (byte)'>':
                Advance();
                return new TextToken(TextTokenType.BlockEnd, ">", startLine, startCol);
            case (byte)'{':
                Advance();
                return new TextToken(TextTokenType.StructBegin, "{", startLine, startCol);
            case (byte)'}':
                Advance();
                return new TextToken(TextTokenType.StructEnd, "}", startLine, startCol);
            case (byte)'[':
                Advance();
                return new TextToken(TextTokenType.ArrayBegin, "[", startLine, startCol);
            case (byte)']':
                Advance();
                return new TextToken(TextTokenType.ArrayEnd, "]", startLine, startCol);
            case (byte)';':
                Advance();
                return new TextToken(TextTokenType.VarEnd, ";", startLine, startCol);
            case (byte)'"':
                Advance(); // opening quote
                int strStart = _pos;
                while (_pos < _text.Length && _text[_pos] != (byte)'"')
                    Advance();
                if (_pos >= _text.Length)
                    throw new EdfParseException("Unterminated string literal", startLine, startCol);
                string str = Encoding.UTF8.GetString(_text.Slice(strStart, _pos - strStart));
                Advance(); // closing quote
                return new TextToken(TextTokenType.StringLiteral, str, startLine, startCol);
            default:
                // Число: [-]цифры[.цифры]
                if (IsAsciiDigit(b) || (b == (byte)'-' && _pos + 1 < _text.Length && IsAsciiDigit(_text[_pos + 1])))
                {
                    int numStart = _pos;
                    if (_text[_pos] == (byte)'-')
                        Advance();
                    while (_pos < _text.Length && IsAsciiDigit(_text[_pos]))
                        Advance();
                    if (_pos < _text.Length && _text[_pos] == (byte)'.')
                    {
                        Advance();
                        while (_pos < _text.Length && IsAsciiDigit(_text[_pos]))
                            Advance();
                    }
                    string num = Encoding.UTF8.GetString(_text.Slice(numStart, _pos - numStart));
                    return new TextToken(TextTokenType.Number, num, startLine, startCol);
                }
                // Идентификатор
                if (IsAsciiLetter(b) || b == (byte)'_')
                {
                    int identStart = _pos;
                    while (_pos < _text.Length && IsAsciiLetterOrDigitOrUnderscore(_text[_pos]))
                        Advance();
                    string ident = Encoding.UTF8.GetString(_text.Slice(identStart, _pos - identStart));
                    return new TextToken(TextTokenType.Identifier, ident, startLine, startCol);
                }
                throw new EdfParseException($"Unexpected character '{(char)b}'", startLine, startCol);
        }
    }

    private void SkipWhitespaceAndComments()
    {
        while (_pos < _text.Length)
        {
            // Whitespace
            if (IsAsciiWhitespace(_text[_pos]))
            {
                Advance();
                continue;
            }

            // Single-line comment //
            if (_pos + 1 < _text.Length && _text[_pos] == (byte)'/' && _text[_pos + 1] == (byte)'/')
            {
                _pos += 2;
                _column += 2;
                while (_pos < _text.Length && _text[_pos] != (byte)'\n')
                {
                    _pos++;
                    _column++;
                }
                continue;
            }

            // Multi-line comment /* */
            if (_pos + 1 < _text.Length && _text[_pos] == (byte)'/' && _text[_pos + 1] == (byte)'*')
            {
                _pos += 2;
                _column += 2;
                while (_pos + 1 < _text.Length && !(_text[_pos] == (byte)'*' && _text[_pos + 1] == (byte)'/'))
                {
                    if (_text[_pos] == (byte)'\n')
                    {
                        _line++;
                        _column = 1;
                    }
                    else
                    {
                        _column++;
                    }
                    _pos++;
                }
                if (_pos + 1 >= _text.Length)
                    throw new EdfParseException("Unterminated block comment", _line, _column);
                _pos += 2;   // skip */
                _column += 2;
                continue;
            }

            break;
        }
    }

    private void Advance()
    {
        if (_pos >= _text.Length) return;
        if (_text[_pos] == (byte)'\n')
        {
            _line++;
            _column = 1;
        }
        else
        {
            _column++;
        }
        _pos++;
    }

    private static bool IsAsciiWhitespace(byte b) =>
        b is 0x09 or 0x0A or 0x0B or 0x0C or 0x0D or 0x20;

    private static bool IsAsciiLetter(byte b) =>
        (b >= 'A' && b <= 'Z') || (b >= 'a' && b <= 'z');

    private static bool IsAsciiDigit(byte b) =>
        b >= '0' && b <= '9';

    private static bool IsAsciiLetterOrDigitOrUnderscore(byte b) =>
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
