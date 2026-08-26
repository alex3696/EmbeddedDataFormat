namespace EdfNet.Core.Binary;

public static class EdfSchemaParser
{
    public static EdfSchema? ReadSchema(this BinBlock block)
    {
        if (block.Type != BlockType.Schema)
            return null;
        var b = block.CurrentContent;
        int pos = 0;
        ushort id = MemoryMarshal.Read<ushort>(b[..sizeof(ushort)]);
        pos += sizeof(ushort);
        pos += EdfBinString.ReadBin(b[pos..], out string? name);
        pos += EdfBinString.ReadBin(b[pos..], out string? desc);
        var type = EdfTypeParser.Parse(b[pos..]);
        return new EdfSchema()
        {
            Id = id,
            Name = name,
            Desc = desc,
            Type = type
        };
    }
}
