namespace NetEdf.src;

/// <summary>
///  Plain object type
/// </summary>
public enum PoType : byte
{
    Struct = 0,
    // integres
    Int8,
    UInt8,
    Int16,
    UInt16,
    Int32,
    UInt32,
    Int64,
    UInt64,
    // float
    Half,
    Single,
    Double,
    // strings
    Char,
    String,
}
public static class PoTypeExt
{
    public static byte GetSizeOf(this PoType p)
    {
        return p switch
        {
            PoType.UInt8 or PoType.Int8 => 1,
            PoType.UInt16 or PoType.Int16 or PoType.Half => 2,
            PoType.UInt32 or PoType.Int32 or PoType.Single => 4,
            PoType.UInt64 or PoType.Int64 or PoType.Double => 8,
            PoType.String or PoType.Char => 1,
            _ => 0,
        };
    }
}

public class TypeInfo : IEquatable<TypeInfo>
{
    public string? Name { get; set; }
    public PoType Type { get; set; }
    public uint[] Dims { get; set; }
    public TypeInfo[] Items { get; set; }

    public TypeInfo(string? name, PoType type, uint[]? dims = default, TypeInfo[]? childs = default)
    {
        Name = name;
        Type = type;
        Dims = dims ?? [];
        Items = (PoType.Struct == type) ? (Items = childs ?? []) : [];
    }
    public TypeInfo(string? name, uint[]? dims = null, TypeInfo[]? childs = null)
        : this(name, PoType.Struct, dims, childs)
    {
    }
    public TypeInfo()
        : this(string.Empty, PoType.Int32)
    {
    }

    public uint ValueSize => GetValueSize();
    public uint GetValueSize()
    {
        uint sz;
        if (PoType.Struct == Type)
        {
            sz = 0;
            for (int i = 0; i < Items.Length; i++)
                sz += Items[i].GetValueSize();
        }
        else
        {
            sz = Type.GetSizeOf();
        }
        for (int i = 0; i < Dims.Length; i++)
            sz *= Dims[i];
        return sz;
    }


    public bool Equals(TypeInfo? y)
    {
        if (null == y)
            return false;
        return Enumerable.SequenceEqual(ToBytes(), y.ToBytes());
    }
    public override bool Equals(object? obj) => Equals(obj as TypeInfo);
    public override int GetHashCode() => ToBytes().GetHashCode();

    public byte[] ToBytes()
    {
        // type
        var ret = new List<byte>(capacity: 256) { (byte)Type };
        // dim
        ret.Add((byte)Dims.Length);
        for (int i = 0; i < Dims.Length; i++)
        {
            byte[] dst = new byte[sizeof(UInt32)];
            BinaryPrimitives.WriteUInt32LittleEndian(dst, Dims[i]);
            ret.AddRange(dst);
        }
        // name
        byte[] bName = Encoding.UTF8.GetBytes(Name ?? string.Empty);
        byte bNameSize = (byte)(255 < bName.Length ? 255 : bName.Length);
        ret.Add(bNameSize);
        ret.AddRange(bName.AsSpan(0, bNameSize).ToArray());
        // childs
        if (PoType.Struct == Type)
        {
            ret.Add((byte)Items.Length);
            for (int i = 0; i < Items.Length; i++)
                ret.AddRange(Items[i].ToBytes());
        }
        return [.. ret];
    }
    public static TypeInfo Parse(ReadOnlySpan<byte> b) => FromBytes(b, out _);
    static TypeInfo FromBytes(ReadOnlySpan<byte> b, out ReadOnlySpan<byte> rest)
    {
        rest = b;
        if (2 > rest.Length)
            throw new ArgumentException($"array is too small {b.Length}");
        if (!Enum.IsDefined(typeof(PoType), b[0]))
            throw new ArgumentException("type mismatch");
        // type
        var type = (PoType)b[0];
        rest = rest.Slice(1);
        // dim
        var dimsCount = rest[0];
        rest = rest.Slice(1);
        uint[]? dims = null;
        if (0 < dimsCount)
        {
            dims = new uint[dimsCount];
            for (int i = 0; i < dimsCount; i++)
            {
                dims[i] = BinaryPrimitives.ReadUInt32LittleEndian(rest);
                rest = rest.Slice(sizeof(UInt32));
            }
        }
        // name
        byte bNameSize = rest[0];
        rest = rest.Slice(1);
        if (255 < bNameSize)
            throw new ArgumentException("name len mismatch");
        var name = Encoding.UTF8.GetString(rest.Slice(0, bNameSize).ToArray());
        rest = rest.Slice(bNameSize);
        // childs
        List<TypeInfo>? childs = null;
        if (PoType.Struct == type && 0 < rest.Length)
        {
            byte childsCount = rest[0];
            rest = rest.Slice(1);
            childs = new List<TypeInfo>(childsCount);
            for (int i = 0; i < childsCount; i++)
                childs.Add(FromBytes(rest, out rest));
        }
        return new TypeInfo(name, type, dims, childs?.ToArray());
    }

    public static string ToString(TypeInfo s)
    {
        StringBuilder sb = new(capacity: 512);
        ToString(s, sb, 0);
        return sb.ToString(1, sb.Length - 1);
    }
    public static void ToString(TypeInfo s, StringBuilder sb, int noffset)
    {
        string offset = GetOffset(noffset);

        string dim = "";
        foreach (var d in s.Dims)
            dim += $"[{d}]";
        sb.Append($"\n{offset}{s.Type}{dim} '{s.Name}'");
        if (0 < s.Items.Length)
        {
            sb.Append($"\n{offset}{{");
            foreach (var it in s.Items)
                ToString(it, sb, noffset + 1);
            sb.Append($"\n{offset}}}");
        }
        sb.Append(';');
    }
    public static string GetOffset(int noffset)
    {
        string offset = "";
        for (int i = 0; i < noffset; i++)
            offset += "  ";
        return offset;
    }

}