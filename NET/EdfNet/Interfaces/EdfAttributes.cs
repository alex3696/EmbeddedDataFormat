namespace EdfNet.Interfaces;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class EdfSerializableAttribute : Attribute { }

[AttributeUsage(AttributeTargets.Property)]
public class EdfArrayAttribute(params int[] dimensions) : Attribute
{
    public int[] Dimensions { get; set; } = dimensions;
}

//[AttributeUsage(AttributeTargets.Property)]
//public class EdfCharArrayAttribute(byte len) : Attribute
//{
//    public byte Len { get; set; } = len;
//}
