using System.Buffers.Text;

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
    /// <summary>
    /// Парсит описание типа (без блоковых маркеров).
    /// </summary>
    public static EdfType Parse(EdfTokenReader tokenizer)
    {
        var result = ParseType(tokenizer);
        if (tokenizer.TokenType != TextTokenType.EOF)
            throw new EdfParseException("Unexpected token", tokenizer.TokenLine, tokenizer.TokenColumn);
        return result;
    }

    /// <summary>
    /// Парсит содержимое блока схемы (без маркера &lt;?).
    /// Ожидает: {Id;"Name"[;"Desc"]} Type ...
    /// </summary>
    public static Schema ParseSchemaContent(EdfTokenReader tokenizer)
    {
        tokenizer.ExpectAdvance(TextTokenType.StructBegin); // {

        // Id
        tokenizer.Expect(TextTokenType.Number);
        if (!Utf8Parser.TryParse(tokenizer.TokenValue, out ushort id, out int c1) || c1 != tokenizer.TokenValue.Length)
            throw new EdfParseException("Invalid schema Id", tokenizer.TokenLine, tokenizer.TokenColumn);
        tokenizer.Advance();
        tokenizer.ExpectAdvance(TextTokenType.VarEnd);

        // Name
        tokenizer.Expect(TextTokenType.StringLiteral);
        string name = tokenizer.GetString();
        tokenizer.Advance();
        tokenizer.ExpectAdvance(TextTokenType.VarEnd);

        // Desc — опционально
        string? desc = null;
        if (!tokenizer.MoveNext())
            throw new EdfParseException("Unexpected end of input", tokenizer.TokenLine, tokenizer.TokenColumn);
        if (tokenizer.TokenType == TextTokenType.StringLiteral)
        {
            desc = tokenizer.GetString();
            tokenizer.Advance();
            tokenizer.ExpectAdvance(TextTokenType.VarEnd);
        }

        tokenizer.ExpectAdvance(TextTokenType.StructEnd); // }

        // Type
        var type = ParseType(tokenizer);

        return new Schema { Id = id, Name = name, Desc = desc, Type = type };
    }

    /// <summary>
    /// Standalone парсинг полного блока схемы (с маркером &lt;? и &gt;).
    /// </summary>
    public static Schema ParseSchema(EdfTokenReader tokenizer)
    {
        tokenizer.ExpectAdvance(TextTokenType.SchemaBegin); // <?
        var schema = ParseSchemaContent(tokenizer);
        tokenizer.ExpectAdvance(TextTokenType.BlockEnd);    // >
        return schema;
    }

    private static EdfType ParseType(EdfTokenReader tokenizer)
    {
        tokenizer.Expect(TextTokenType.Identifier);
        var poType = ParsePoType(tokenizer.TokenValue);
        tokenizer.Advance();

        var dims = ParseDimensions(tokenizer);

        tokenizer.Expect(TextTokenType.StringLiteral);
        string name = tokenizer.GetString();
        tokenizer.Advance();

        if (poType == PoType.Struct)
        {
            tokenizer.ExpectAdvance(TextTokenType.StructBegin);
            var childs = ParseChilds(tokenizer);
            tokenizer.ExpectAdvance(TextTokenType.StructEnd);
            return new EdfType(poType, name, dims, childs);
        }
        else
        {
            tokenizer.ExpectAdvance(TextTokenType.VarEnd);
            return new EdfType(poType, name, dims, null);
        }
    }

    private static ushort[] ParseDimensions(EdfTokenReader tokenizer)
    {
        var dims = new List<ushort>();
        while (true)
        {
            if (!tokenizer.MoveNext()) break;
            if (tokenizer.TokenType != TextTokenType.ArrayBegin) break;
            tokenizer.Advance(); // [
            tokenizer.Expect(TextTokenType.Number);
            if (!Utf8Parser.TryParse(tokenizer.TokenValue, out ushort dim, out int c) || c != tokenizer.TokenValue.Length)
                throw new EdfParseException("Invalid array dimension", tokenizer.TokenLine, tokenizer.TokenColumn);
            dims.Add(dim);
            tokenizer.Advance();
            tokenizer.ExpectAdvance(TextTokenType.ArrayEnd); // ]
        }
        return dims.ToArray();
    }

    private static EdfType[] ParseChilds(EdfTokenReader tokenizer)
    {
        var childs = new List<EdfType>();
        while (true)
        {
            if (!tokenizer.MoveNext()) break;
            if (tokenizer.TokenType == TextTokenType.StructEnd || tokenizer.TokenType == TextTokenType.EOF)
                break;
            childs.Add(ParseType(tokenizer));
        }
        return childs.ToArray();
    }

    private static PoType ParsePoType(ReadOnlySpan<byte> tokenValue)
    {
        string text = Encoding.UTF8.GetString(tokenValue);
        if (!Enum.TryParse<PoType>(text, out var result))
            throw new EdfParseException($"Unknown type '{text}'", 0, 0);
        return result;
    }
}
