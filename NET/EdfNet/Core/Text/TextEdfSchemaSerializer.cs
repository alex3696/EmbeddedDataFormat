using System.Buffers.Text;

namespace EdfNet.Core.Text;

internal static class TextEdfSchemaSerializer
{
    /// <summary>
    /// Standalone парсинг полного блока схемы (с маркером &lt;? и &gt;).
    /// </summary>
    public static EdfSchema ReadSchema(this EdfTokenReader tokenizer)
    {
        tokenizer.ExpectAdvance(TextTokenType.SchemaBegin); // <?
        var schema = ReadSchemaBlockContent(tokenizer);
        tokenizer.ExpectAdvance(TextTokenType.BlockEnd);    // >
        return schema;
    }
    /// <summary>
    /// Парсит содержимое блока схемы (без маркера &lt;?).
    /// Ожидает: {Id;"Name"[;"Desc"]} Type ...
    /// </summary>
    public static EdfSchema ReadSchemaBlockContent(EdfTokenReader tokenizer)
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
        var type = ReadType(tokenizer);

        return new EdfSchema { Id = id, Name = name, Desc = desc, Type = type };
    }
    public static EdfType ReadType(EdfTokenReader tokenizer)
    {
        tokenizer.Expect(TextTokenType.Identifier);
        var poType = ParsePoType(tokenizer.TokenValue);
        tokenizer.Advance();

        var dims = ParseDimensions(tokenizer);

        string name = string.Empty;
        if (tokenizer.TokenType == TextTokenType.StringLiteral)
        {
            name = tokenizer.GetString();
            tokenizer.Advance();
        }

        if (poType == EdfPrimitiveType.Struct)
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
            childs.Add(ReadType(tokenizer));
        }
        return childs.ToArray();
    }
    private static EdfPrimitiveType ParsePoType(ReadOnlySpan<byte> tokenValue)
    {
        string text = Encoding.UTF8.GetString(tokenValue);
        if (!Enum.TryParse<EdfPrimitiveType>(text, out var result))
            throw new EdfParseException($"Unknown type '{text}'", 0, 0);
        return result;
    }

    public static void WriteSchema(this BufferedTextWriter writer, EdfSchema sch)
    {
        writer.Flush();
        writer.Write(EdfTokenLiterals.EndLine);
        writer.Write(EdfTokenLiterals.SchemaBegin);
        writer.Write(EdfTokenLiterals.Space);
        writer.Write(EdfTokenLiterals.StructBegin);
        writer.Write($"{sch.Id};\"{sch.Name}\";");
        if (!string.IsNullOrEmpty(sch.Desc))
            writer.Write($"\"{sch.Desc}\";");
        writer.Write(EdfTokenLiterals.StructEnd);
        writer.Write(EdfTokenLiterals.Space);
        writer.ToString(sch.Type);
        writer.Write(EdfTokenLiterals.BlockEnd);
        writer.Write(EdfTokenLiterals.EndLine);
        writer.Flush();
    }
    private static void ToString(this BufferedTextWriter writer, EdfType t, int noffset = 0)
    {
        string offset = GetOffset(noffset);
        writer.Write(offset);
        writer.Write(t.Type.ToString());
        if (null != t.Dims)
        {
            foreach (var d in t.Dims)
                writer.Write($"[{d}]");
        }
        if (!string.IsNullOrEmpty(t.Name))
            writer.Write($" \"{t.Name}\"");
        if (EdfPrimitiveType.Struct == t.Type && null != t.Childs && 0 < t.Childs.Length)
        {
            writer.Write($"\n{offset}{{");
            foreach (var it in t.Childs)
            {
                writer.Write($"\n");
                ToString(writer, it, noffset + 1);
            }
            writer.Write($"\n{offset}}}");
        }
        else
            writer.Write(";");
    }
    private static string GetOffset(int noffset)
    {
        string offset = "";
        for (int i = 0; i < noffset; i++)
            offset += "  ";
        return offset;
    }
}
