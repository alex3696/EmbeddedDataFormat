namespace EdfNet.Interfaces;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class EdfSerializableAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
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
