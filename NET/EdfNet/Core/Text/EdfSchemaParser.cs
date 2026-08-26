using System.Buffers.Text;

namespace EdfNet.Core.Text;

public static class EdfSchemaParser
{
    /// <summary>
    /// Парсит содержимое блока схемы (без маркера &lt;?).
    /// Ожидает: {Id;"Name"[;"Desc"]} Type ...
    /// </summary>
    public static EdfSchema ParseBlockContent(EdfTokenReader tokenizer)
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
        var type = EdfTypeParser.ParseType(tokenizer);

        return new EdfSchema { Id = id, Name = name, Desc = desc, Type = type };
    }

    /// <summary>
    /// Standalone парсинг полного блока схемы (с маркером &lt;? и &gt;).
    /// </summary>
    public static EdfSchema ParseBlock(EdfTokenReader tokenizer)
    {
        tokenizer.ExpectAdvance(TextTokenType.SchemaBegin); // <?
        var schema = ParseBlockContent(tokenizer);
        tokenizer.ExpectAdvance(TextTokenType.BlockEnd);    // >
        return schema;
    }
}
