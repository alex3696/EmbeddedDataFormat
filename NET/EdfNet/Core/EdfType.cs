namespace EdfNet.Core;

[DebuggerDisplay("{DebugString(),nq}")]
public class EdfType : IEquatable<EdfType>
{
    public EdfPrimitiveType Type;// { get; set; }
    public string? Name;// { get; set; }
    public ushort[] Dims;// { get; set; }
    public EdfType[] Childs = [];// { get; set; }
    private int _totalElements = int.MinValue; // -1 = не вычислено
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
        if (EdfPrimitiveType.Struct == Type && null != Childs && 0 < Childs.Length)
        {
            string offset = GetOffset(noffset);
            childs += $"\n{offset}{{";
            foreach (var it in Childs)
                childs += $"{offset}{it.DebugString(noffset + 1)};\n";
            childs += $"\n{offset}}}";
        }
        return $"{Type} \"{Name}\"{dims}{childs}";
    }
    public EdfType(EdfPrimitiveType type, string? name = default, ushort[]? dims = default, EdfType[]? childs = default)
    {
        Name = name;
        Type = type;
        Dims = dims ?? [];
        Childs = (EdfPrimitiveType.Struct == type) ? (Childs = childs ?? []) : [];
    }
    public EdfType()
        : this(EdfPrimitiveType.Int32)
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
        if (_totalElements > int.MinValue) return (uint)_totalElements;
        uint totalElement = 1;
        for (int i = 0; i < Dims?.Length; i++)
            totalElement *= Dims[i];
        _totalElements = (int)totalElement;
        return totalElement;
    }

    private bool? _hasDynamicFields = default;
    private uint? _flatLen = default;

    public bool HasDynamicFields
    {
        get
        {
            if (null != _hasDynamicFields)
                return _hasDynamicFields.Value;
            GetTypeInfo(out bool hasDynamicFields, out uint flatLen);
            return hasDynamicFields;
        }
    }
    public uint FlatLen
    {
        get
        {
            if (null != _flatLen)
                return _flatLen.Value;
            GetTypeInfo(out bool hasDynamicFields, out uint flatLen);
            return flatLen;
        }
    }
    public void GetTypeInfo(out bool hasDynamicFields, out uint sz)
    {
        if (null == _hasDynamicFields || null == _flatLen)
        {
            GetTypeInfo(this, out hasDynamicFields, out sz);
            _hasDynamicFields = hasDynamicFields;
            _flatLen = sz;
        }
        else
        {
            hasDynamicFields = _hasDynamicFields.Value;
            sz = _flatLen.Value;
        }
    }
    public static void GetTypeInfo(EdfType t, out bool hasDynamicFields, out uint sz)
    {
        switch (t.Type)
        {
            case EdfPrimitiveType.Struct:
                sz = 0;
                hasDynamicFields = false;
                for (int i = 0; i < t.Childs.Length; i++)
                {
                    GetTypeInfo(t.Childs[i], out bool dynField, out uint len);
                    sz += len;
                    if (dynField)
                        hasDynamicFields = true;
                }
                break;
            case EdfPrimitiveType.String:
                sz = 1;
                hasDynamicFields = true;
                break;
            default:
                sz = t.Type.GetSizeOf();
                hasDynamicFields = false;
                break;
        }//switch
        for (int i = 0; i < t.Dims.Length; i++)
            sz *= t.Dims[i];
    }
}
