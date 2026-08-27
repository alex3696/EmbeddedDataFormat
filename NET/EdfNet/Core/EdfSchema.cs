namespace EdfNet.Core;

public class EdfSchema : IEquatable<EdfSchema>
{
    public ushort Id; // var id
    public string? Name; // var name
    public string? Desc; // var description
    public required EdfType Type; // var type

    public bool Equals(EdfSchema? other)
    {
        if (null == other) return false;
        if (Id != other.Id) return false;
        if (Name != other.Name) return false;
        if (Desc != other.Desc) return false;
        if (!Type.Equals(other.Type)) return false;
        return true;
    }
}
