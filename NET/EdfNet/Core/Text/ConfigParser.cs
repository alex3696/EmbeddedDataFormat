using System.Buffers.Text;

namespace EdfNet.Core.Text;

public static class ConfigParser
{
    /// <summary>
    /// Парсит содержимое блока конфигурации (без маркера &lt;~).
    /// Ожидает: { VersMajor; VersMinor; Blocksize; Encoding; Flags; }
    /// </summary>
    public static EdfConfig ParseContent(EdfTokenReader tokenizer)
    {
        tokenizer.ExpectAdvance(TextTokenType.StructBegin); // {

        // VersMajor
        tokenizer.Expect(TextTokenType.Number);
        if (!Utf8Parser.TryParse(tokenizer.TokenValue, out byte versMajor, out int c1) || c1 != tokenizer.TokenValue.Length)
            throw new EdfParseException("Invalid VersMajor", tokenizer.TokenLine, tokenizer.TokenColumn);
        tokenizer.Advance();
        tokenizer.ExpectAdvance(TextTokenType.VarEnd);

        // VersMinor
        tokenizer.Expect(TextTokenType.Number);
        if (!Utf8Parser.TryParse(tokenizer.TokenValue, out byte versMinor, out int c2) || c2 != tokenizer.TokenValue.Length)
            throw new EdfParseException("Invalid VersMinor", tokenizer.TokenLine, tokenizer.TokenColumn);
        tokenizer.Advance();
        tokenizer.ExpectAdvance(TextTokenType.VarEnd);

        // Blocksize
        tokenizer.Expect(TextTokenType.Number);
        if (!Utf8Parser.TryParse(tokenizer.TokenValue, out ushort blocksize, out int c3) || c3 != tokenizer.TokenValue.Length)
            throw new EdfParseException("Invalid Blocksize", tokenizer.TokenLine, tokenizer.TokenColumn);
        tokenizer.Advance();
        tokenizer.ExpectAdvance(TextTokenType.VarEnd);

        // Encoding
        tokenizer.Expect(TextTokenType.Number);
        if (!Utf8Parser.TryParse(tokenizer.TokenValue, out ushort encoding, out int c4) || c4 != tokenizer.TokenValue.Length)
            throw new EdfParseException("Invalid Encoding", tokenizer.TokenLine, tokenizer.TokenColumn);
        tokenizer.Advance();
        tokenizer.ExpectAdvance(TextTokenType.VarEnd);

        // Flags
        tokenizer.Expect(TextTokenType.Number);
        if (!Utf8Parser.TryParse(tokenizer.TokenValue, out uint flags, out int c5) || c5 != tokenizer.TokenValue.Length)
            throw new EdfParseException("Invalid Flags", tokenizer.TokenLine, tokenizer.TokenColumn);
        tokenizer.Advance();
        tokenizer.ExpectAdvance(TextTokenType.VarEnd);

        tokenizer.ExpectAdvance(TextTokenType.StructEnd); // }

        return new EdfConfig(blocksize)
        {
            VersMajor = versMajor,
            VersMinor = versMinor,
            Encoding = encoding,
            Flags = (EdfConfigOptions)flags
        };
    }

    /// <summary>
    /// Standalone парсинг полного блока конфигурации (с маркером <~ ... >).
    /// </summary>
    public static EdfConfig Parse(EdfTokenReader tokenizer)
    {
        tokenizer.ExpectAdvance(TextTokenType.ConfigBegin); // <~
        var cfg = ParseContent(tokenizer);
        tokenizer.ExpectAdvance(TextTokenType.BlockEnd);    // >
        return cfg;
    }

}
