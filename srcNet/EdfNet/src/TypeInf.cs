namespace NetEdf.src;

[DebuggerDisplay("{DebugString(),nq}")]
public class TypeInf : IEquatable<TypeInf>
{
    public PoType Type;// { get; set; }
    public string? Name;// { get; set; }
    public uint[]? Dims;// { get; set; }
    public TypeInf[]? Childs;// { get; set; }

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
    public TypeInf(PoType type, string? name = default, uint[]? dims = default, TypeInf[]? childs = default)
    {
        Name = name;
        Type = type;
        Dims = dims ?? [];
        Childs = (PoType.Struct == type) ? (Childs = childs ?? []) : [];
    }
    public TypeInf(string? name, PoType type, uint[]? dims = default, TypeInf[]? childs = default)
    {
        Name = name;
        Type = type;
        Dims = dims ?? [];
        Childs = (PoType.Struct == type) ? (Childs = childs ?? []) : [];
    }
    public TypeInf(string? name, uint[]? dims = null, TypeInf[]? childs = null)
        : this(name, PoType.Struct, dims, childs)
    {
    }
    public TypeInf()
        : this(string.Empty, PoType.Int32)
    {
    }
    public bool Equals(TypeInf? y)
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
    public override bool Equals(object? obj) => Equals(obj as TypeInf);
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
