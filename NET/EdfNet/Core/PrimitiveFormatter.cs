namespace EdfNet.Core;

public class PrimitiveFormatter<T> : IFormatter<T> where T : struct
{
    public void Serialize<TWriter>(ref TWriter writer, in T val, EdfOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }

    public T Deserialize<TReader>(ref TReader reader, EdfOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        return reader.Read<T>();
    }
}

public class PrimitiveFormatterByte : PrimitiveFormatter<byte> { }
public class PrimitiveFormatterSByte : PrimitiveFormatter<sbyte> { }
public class PrimitiveFormatterShort : PrimitiveFormatter<short> { }
public class PrimitiveFormatterUShort : PrimitiveFormatter<ushort> { }
public class PrimitiveFormatterInt : PrimitiveFormatter<int> { }
public class PrimitiveFormatterUint : PrimitiveFormatter<uint> { }
public class PrimitiveFormatterLong : PrimitiveFormatter<long> { }
public class PrimitiveFormatterULong : PrimitiveFormatter<ulong> { }
public class PrimitiveFormatterFloat : PrimitiveFormatter<float> { }
public class PrimitiveFormatterDouble : PrimitiveFormatter<double> { }

public class PrimitiveFormatterString : IFormatter<string?>
{
    public void Serialize<TWriter>(ref TWriter writer, in string? val, EdfOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }

    public string? Deserialize<TReader>(ref TReader reader, EdfOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        return reader.ReadString();
    }
}

public class PrimitiveResolver : IFormatterResolver
{
    public static PrimitiveResolver Instance = new();
    public IFormatter<T>? GetFormatter<T>()
    {
        if (typeof(T) == typeof(PrimitiveFormatterByte))
            return (IFormatter<T>)(object)new PrimitiveFormatterByte();
        if (typeof(T) == typeof(PrimitiveFormatterSByte))
            return (IFormatter<T>)(object)new PrimitiveFormatterSByte();
        if (typeof(T) == typeof(PrimitiveFormatterShort))
            return (IFormatter<T>)(object)new PrimitiveFormatterShort();
        if (typeof(T) == typeof(PrimitiveFormatterUShort))
            return (IFormatter<T>)(object)new PrimitiveFormatterUShort();
        if (typeof(T) == typeof(PrimitiveFormatterInt))
            return (IFormatter<T>)(object)new PrimitiveFormatterInt();
        if (typeof(T) == typeof(PrimitiveFormatterUint))
            return (IFormatter<T>)(object)new PrimitiveFormatterUint();
        if (typeof(T) == typeof(PrimitiveFormatterLong))
            return (IFormatter<T>)(object)new PrimitiveFormatterLong();
        if (typeof(T) == typeof(PrimitiveFormatterULong))
            return (IFormatter<T>)(object)new PrimitiveFormatterULong();
        if (typeof(T) == typeof(PrimitiveFormatterFloat))
            return (IFormatter<T>)(object)new PrimitiveFormatterFloat();
        if (typeof(T) == typeof(PrimitiveFormatterDouble))
            return (IFormatter<T>)(object)new PrimitiveFormatterDouble();
        if (typeof(T) == typeof(PrimitiveFormatterString))
            return (IFormatter<T>)(object)new PrimitiveFormatterString();
        return null;
    }
}
