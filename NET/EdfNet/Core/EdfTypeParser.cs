namespace EdfNet.Core;

public class EdfParseException : Exception
{
    public int Line { get; }
    public int Column { get; }

    public EdfParseException(string message, int line, int column)
        : base($"[{line}:{column}] {message}")
    {
        Line = line;
        Column = column;
    }
}

public static class EdfTypeParser
{
    public static EdfType Parse(ReadOnlySpan<byte> text)
    {
        EdfTokenizer tokenizer = new(text);
        var result = ParseType(ref tokenizer);
        var eof = tokenizer.Peek();
        if (eof.Type != TextTokenType.EOF)
            throw new EdfParseException($"Unexpected token '{eof.Text}'", eof.Line, eof.Column);
        return result;
    }

    private static EdfType ParseType(ref EdfTokenizer tokenizer)
    {
        var typeToken = tokenizer.Expect(TextTokenType.Identifier);
        var poType = ParsePoType(typeToken);

        var dims = ParseDimensions(ref tokenizer);

        var nameToken = tokenizer.Expect(TextTokenType.StringLiteral);
        var name = nameToken.Text;

        if (poType == PoType.Struct)
        {
            tokenizer.Expect(TextTokenType.LBrace);
            var childs = ParseChilds(ref tokenizer);
            tokenizer.Expect(TextTokenType.RBrace);
            return new EdfType(poType, name, dims, childs);
        }
        else
        {
            tokenizer.Expect(TextTokenType.Semicolon);
            return new EdfType(poType, name, dims, null);
        }
    }

    private static ushort[] ParseDimensions(ref EdfTokenizer tokenizer)
    {
        var dims = new List<ushort>();
        while (tokenizer.Peek().Type == TextTokenType.LBracket)
        {
            tokenizer.Consume(); // [
            var numToken = tokenizer.Expect(TextTokenType.Number);
            if (!ushort.TryParse(numToken.Text, out var dim))
                throw new EdfParseException(
                    $"Array dimension must be 0..65535, got '{numToken.Text}'",
                    numToken.Line, numToken.Column);
            dims.Add(dim);
            tokenizer.Expect(TextTokenType.RBracket); // ]
        }
        return dims.ToArray();
    }

    private static EdfType[] ParseChilds(ref EdfTokenizer tokenizer)
    {
        var childs = new List<EdfType>();
        while (tokenizer.Peek().Type != TextTokenType.RBrace && tokenizer.Peek().Type != TextTokenType.EOF)
        {
            childs.Add(ParseType(ref tokenizer));
        }
        return childs.ToArray();
    }

    private static PoType ParsePoType(TextToken token)
    {
        if (!Enum.TryParse<PoType>(token.Text, out var result))
            throw new EdfParseException($"Unknown type '{token.Text}'", token.Line, token.Column);
        return result;
    }
}
