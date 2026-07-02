namespace NetEdf.src;

[DebuggerDisplay("{DebugString(),nq}")]
public class EdfType : IEquatable<EdfType>
{
    public PoType Type;// { get; set; }
    public string? Name;// { get; set; }
    public ushort[]? Dims;// { get; set; }
    public EdfType[]? Childs;// { get; set; }

    protected static string GetOffset(int noffset)
    {
        string offset = "";
        for (int i = 0; i < noffset; i++)
            offset += "  ";
        return offset;
    }
    public string DebugString(int noffset = 0)
    {
        string dims = string.Empty;
        if (null != Dims && 0 < Dims.Length)
            foreach (var d in Dims)
                dims += $"[{d}]";
        string childs = string.Empty;
        if (PoType.Struct == Type && null != Childs && 0 < Childs.Length)
        {
            string offset = GetOffset(noffset);
            childs += $"\n{offset}{{";
            foreach (var it in Childs)
                childs += $"{offset}{it.DebugString(noffset + 1)};\n";
            childs += $"\n{offset}}}";
        }
        return $"{Type} \"{Name}\"{dims}{childs}";
    }
    public EdfType(PoType type, string? name = default, ushort[]? dims = default, EdfType[]? childs = default)
    {
        Name = name;
        Type = type;
        Dims = dims ?? [];
        Childs = (PoType.Struct == type) ? (Childs = childs ?? []) : [];
    }
    public EdfType(string? name, PoType type, ushort[]? dims = default, EdfType[]? childs = default)
    {
        Name = name;
        Type = type;
        Dims = dims ?? [];
        Childs = (PoType.Struct == type) ? (Childs = childs ?? []) : [];
    }
    public EdfType(string? name, ushort[]? dims = null, EdfType[]? childs = null)
        : this(name, PoType.Struct, dims, childs)
    {
    }
    public EdfType()
        : this(string.Empty, PoType.Int32)
    {
    }
    public bool Equals(EdfType? y)
    {
        if (y is null)
            return false;
        if (ReferenceEquals(this, y))
            return true;
        if (Type != y.Type)
            return false;
        if (Name != y.Name)
            return false;
        if (!(Dims ?? []).SequenceEqual(y.Dims ?? []))
            return false;
        if (!(Childs ?? []).SequenceEqual(y.Childs ?? []))
            return false;
        return true;
    }
    public override bool Equals(object? obj) => Equals(obj as EdfType);
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Type);
        hash.Add(Name);
        if (Dims != null) foreach (var d in Dims) hash.Add(d);
        if (Childs != null) foreach (var i in Childs) hash.Add(i);
        return hash.ToHashCode();
    }
    public uint GetTotalElements()
    {
        uint totalElement = 1;
        for (int i = 0; i < Dims?.Length; i++)
            totalElement *= Dims[i];
        return totalElement;
    }


}
