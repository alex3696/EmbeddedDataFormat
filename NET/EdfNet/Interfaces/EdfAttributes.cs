namespace EdfNet.Interfaces;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class EdfSerializableAttribute : Attribute
{
    public ushort Id { get; set; }
    public string? Name { get; set; }
    public string? Desc { get; set; }
    public EdfSerializableAttribute(ushort id = default, string? name = default, string? desc = default)
    {
        Id = id;
        Name = name;
        Desc = desc;
    }
}

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
public class EdfArrayAttribute(params ushort[] dimensions) : Attribute
{
    public ushort[] Dimensions { get; set; } = dimensions;
}

[AttributeUsage(AttributeTargets.Property)]
public class EdfCharArrayAttribute(byte len) : Attribute
{
    public byte Len { get; set; } = len;
}

[AttributeUsage(AttributeTargets.Property)]
public class EdfIgnoreAttribute : Attribute { }
