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
public class CharArrayFormatter : IFormatter<byte[]>
{
    public readonly int Len;
    public CharArrayFormatter(int len)
    {
        Len = len;
    }
    public void Serialize<TWriter>(ref TWriter writer, in byte[] val, EdfOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.WriteCharArray(val, Len);
    }
    public byte[] Deserialize<TReader>(ref TReader reader, EdfOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        return reader.ReadCharArray(Len);
    }
}

public class PrimitiveArrayFormatter<T> : IFormatter<T[]> where T : struct
{
    public readonly int Len;
    public PrimitiveArrayFormatter(int len)
    {
        Len = len;
    }
    public void Serialize<TWriter>(ref TWriter writer, in T[] val, EdfOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.BeginArray();
        for (int i = 0; i < val.Length; i++)
            writer.Write(val[i]);
        writer.EndArray();
    }
    public T[] Deserialize<TReader>(ref TReader reader, EdfOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        reader.ReadBeginArray();
        var arr = new T[Len];
        for (int i = 0; i < Len; ++i)
            arr[i] = reader.Read<T>();
        return arr;
    }
}


public class PrimitiveResolver : IFormatterResolver
{
    public static readonly PrimitiveResolver Instance = new();
    public IFormatter<T>? GetFormatter<T>()
    {
        switch (Type.GetTypeCode(typeof(T)))
        {
            default: break;
            case TypeCode.Byte: return (IFormatter<T>)(object)new PrimitiveFormatter<byte>();
            case TypeCode.SByte: return (IFormatter<T>)(object)new PrimitiveFormatter<sbyte>();
            case TypeCode.Int16: return (IFormatter<T>)(object)new PrimitiveFormatter<short>();
            case TypeCode.UInt16: return (IFormatter<T>)(object)new PrimitiveFormatter<ushort>();
            case TypeCode.Int32: return (IFormatter<T>)(object)new PrimitiveFormatter<int>();
            case TypeCode.UInt32: return (IFormatter<T>)(object)new PrimitiveFormatter<uint>();
            case TypeCode.Int64: return (IFormatter<T>)(object)new PrimitiveFormatter<long>();
            case TypeCode.UInt64: return (IFormatter<T>)(object)new PrimitiveFormatter<ulong>();
            case TypeCode.Single: return (IFormatter<T>)(object)new PrimitiveFormatter<float>();
            case TypeCode.Double: return (IFormatter<T>)(object)new PrimitiveFormatter<double>();
            case TypeCode.String: return (IFormatter<T>)(object)new PrimitiveFormatterString();
        }
        return null;
    }
}
