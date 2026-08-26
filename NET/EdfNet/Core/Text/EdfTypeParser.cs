using System.Buffers.Text;

namespace EdfNet.Core.Text;

public static class EdfTypeParser
{
    public static EdfType ParseType(EdfTokenReader tokenizer)
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
