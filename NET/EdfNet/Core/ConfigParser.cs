namespace EdfNet.Core;

public static class ConfigParser
{
    public static Config Parse(ReadOnlySpan<byte> text)
    {
        EdfTokenizer tokenizer = new(text);

        // <~
        tokenizer.Expect(TextTokenType.ConfigBegin);
        // {
        tokenizer.Expect(TextTokenType.StructBegin);

        // VersMajor
        var majorToken = tokenizer.Expect(TextTokenType.Number);
        if (!byte.TryParse(majorToken.Text, out byte versMajor))
            throw new EdfParseException($"Invalid VersMajor '{majorToken.Text}'", majorToken.Line, majorToken.Column);
        tokenizer.Expect(TextTokenType.VarEnd);

        // VersMinor
        var minorToken = tokenizer.Expect(TextTokenType.Number);
        if (!byte.TryParse(minorToken.Text, out byte versMinor))
            throw new EdfParseException($"Invalid VersMinor '{minorToken.Text}'", minorToken.Line, minorToken.Column);
        tokenizer.Expect(TextTokenType.VarEnd);

        // Blocksize
        var bsToken = tokenizer.Expect(TextTokenType.Number);
        if (!ushort.TryParse(bsToken.Text, out ushort blocksize))
            throw new EdfParseException($"Invalid Blocksize '{bsToken.Text}'", bsToken.Line, bsToken.Column);
        tokenizer.Expect(TextTokenType.VarEnd);

        // Encoding
        var encToken = tokenizer.Expect(TextTokenType.Number);
        if (!ushort.TryParse(encToken.Text, out ushort encoding))
            throw new EdfParseException($"Invalid Encoding '{encToken.Text}'", encToken.Line, encToken.Column);
        tokenizer.Expect(TextTokenType.VarEnd);

        // Flags
        var flagsToken = tokenizer.Expect(TextTokenType.Number);
        if (!uint.TryParse(flagsToken.Text, out uint flags))
            throw new EdfParseException($"Invalid Flags '{flagsToken.Text}'", flagsToken.Line, flagsToken.Column);
        tokenizer.Expect(TextTokenType.VarEnd);

        // }
        tokenizer.Expect(TextTokenType.StructEnd);
        // >
        tokenizer.Expect(TextTokenType.BlockEnd);

        return new Config(blocksize)
        {
            VersMajor = versMajor,
            VersMinor = versMinor,
            Encoding = encoding,
            Flags = (Options)flags
        };
    }
}
