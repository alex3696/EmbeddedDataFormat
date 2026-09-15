namespace EdfNet.Core.Binary;

internal static class BinaryEdfSchemaSerializer
{
    public static EdfSchema? TryReadSchema(this BinBlock block)
    {
        if (block.Type != EdfBlockType.Schema)
            return null;
        var b = block.CurrentContent;
        int pos = 0;
        ushort id = MemoryMarshal.Read<ushort>(b[..sizeof(ushort)]);
        pos += sizeof(ushort);
        pos += EdfBinString.ReadBin(b[pos..], out string? name);
        pos += EdfBinString.ReadBin(b[pos..], out string? desc);
        var type = TryReadType(b[pos..]);
        return new EdfSchema()
        {
            Id = id,
            Name = name,
            Desc = desc,
            Type = type
        };
    }
    public static EdfType TryReadType(ReadOnlySpan<byte> b) => ParseType(b, out _);
    static EdfType ParseType(ReadOnlySpan<byte> b, out ReadOnlySpan<byte> rest)
    {
        rest = b;
        if (2 > rest.Length)
            throw new ArgumentException($"array is too small {b.Length}");
        if (!Enum.IsDefined((EdfPrimitiveType)b[0]))
            throw new ArgumentException("type mismatch");
        // type
        var type = (EdfPrimitiveType)b[0];
        rest = rest.Slice(1);
        // dim
        var dimsCount = rest[0];
        rest = rest.Slice(1);
        ushort[]? dims = null;
        if (0 < dimsCount)
        {
            dims = new ushort[dimsCount];
            for (int i = 0; i < dimsCount; i++)
            {
                dims[i] = BinaryPrimitives.ReadUInt16LittleEndian(rest);
                rest = rest.Slice(sizeof(UInt16));
            }
        }
        // name
        var nameLen = EdfBinString.ReadBin(rest, out string? name);
        rest = rest.Slice(nameLen);
        // childs
        List<EdfType>? childs = null;
        if (EdfPrimitiveType.Struct == type && 0 < rest.Length)
        {
            byte childsCount = rest[0];
            rest = rest.Slice(1);
            childs = new List<EdfType>(childsCount);
            for (int i = 0; i < childsCount; i++)
                childs.Add(ParseType(rest, out rest));
        }
        return new EdfType(type, name, dims, childs?.ToArray());
    }

    public static void WriteSchema(this BinBlock block, EdfSchema sch)
    {
        block.Reset();
        block.Type = EdfBlockType.Schema;
        var buf = new SpanBufferWriter(block.ContentBuffer);
        buf.Append(sch.Id);
        buf.Append(sch.Name);
        buf.Append(sch.Desc);
        Append(ref buf, sch.Type);
        block.ContentLen = (ushort)buf.WrittedCount;
    }
    private static void Append(ref SpanBufferWriter buf, EdfType t)
    {
        buf.Append(t.Type);
        if (null != t.Dims && 0 < t.Dims.Length)
        {
            buf.Append((byte)t.Dims.Length);
            for (int i = 0; i < t.Dims.Length; i++)
                buf.Append(t.Dims[i]);
        }
        else
            buf.Append((byte)0);

        buf.Append(t.Name);

        if (EdfPrimitiveType.Struct == t.Type && null != t.Childs && 0 < t.Childs.Length)
        {
            buf.Append((byte)t.Childs.Length);
            for (int i = 0; i < t.Childs.Length; i++)
            {
                Append(ref buf, t.Childs[i]);
            }
        }
    }
}
