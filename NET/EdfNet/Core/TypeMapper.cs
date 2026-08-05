namespace EdfNet.Core;

public static class TypeMapper
{
    public static bool IsSimpleType(this Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string);
    }
    public static bool IsSimpleType(this Type type, EdfType? et)
    {
        return type.IsSimpleType() || (type == typeof(byte[]) && PoType.Char == et?.Type);
    }
    public static bool IsStructType(this Type type)
    {
        if (type.IsClass || (type.IsValueType && !IsSimpleType(type)))
        {
            //return type.IsDefined(typeof(EdfSerializableAttribute), inherit: false);
            return true;
        }
        return false;
    }
}
