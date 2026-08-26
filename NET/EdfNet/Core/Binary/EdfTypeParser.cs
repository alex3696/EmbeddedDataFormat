namespace EdfNet.Core.Binary;

public static class EdfTypeParser
{
    public static EdfType Parse(ReadOnlySpan<byte> b) => ParseType(b, out _);
    static EdfType ParseType(ReadOnlySpan<byte> b, out ReadOnlySpan<byte> rest)
    {
        rest = b;
        if (2 > rest.Length)
            throw new ArgumentException($"array is too small {b.Length}");
        if (!Enum.IsDefined((PoType)b[0]))
            throw new ArgumentException("type mismatch");
        // type
        var type = (PoType)b[0];
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
        if (PoType.Struct == type && 0 < rest.Length)
        {
            byte childsCount = rest[0];
            rest = rest.Slice(1);
            childs = new List<EdfType>(childsCount);
            for (int i = 0; i < childsCount; i++)
                childs.Add(ParseType(rest, out rest));
        }
        return new EdfType(type, name, dims, childs?.ToArray());
    }
}
