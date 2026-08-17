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

public interface IFormatterArray<TARRAY> : IFormatter<TARRAY>
{
    void Serialize<TWriter>(ref TWriter writer, int[] Dims, in TARRAY arrObj, EdfOptions options)
        where TWriter : struct, IBufWriter, allows ref struct;
    TARRAY Deserialize<TReader>(ref TReader reader, int[] Dims, EdfOptions options)
        where TReader : struct, IBufReader, allows ref struct;
}

public class PrimitiveArrayFormatter<TARRAY, TITEM> : IFormatter<TARRAY>
    //where TARRAY : IList
    where TITEM : struct
{
    public void Serialize<TWriter>(ref TWriter writer, int[] dims, in TARRAY arrObj, EdfOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        if (arrObj is not Array arr)
            return;
        writer.BeginArray();
        for (int i = 0; i < arr.Length; i++)
        {
            ref TITEM item = ref arr.GetElementAtFlatIndex<TITEM>(i);
            writer.Write<TITEM>(item);
        }
        writer.EndArray();
    }
    public TARRAY Deserialize<TReader>(ref TReader reader, int[] dims, EdfOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        reader.ReadBeginArray();
        var arr = Array.CreateInstanceFromArrayType(typeof(TARRAY), dims);
        for (int i = 0; i < arr.Length; i++)
        {
            arr.GetElementAtFlatIndex<TITEM>(i) = reader.Read<TITEM>();
        }
        reader.ReadEndArray();
        return Unsafe.As<Array, TARRAY>(ref arr);//return (TARRAY)(object)arr;
    }

    public void Serialize<TWriter>(ref TWriter writer, in TARRAY value, EdfOptions options) where TWriter : struct, IBufWriter, allows ref struct
    {
        throw new NotImplementedException();
    }

    public TARRAY Deserialize<TReader>(ref TReader reader, EdfOptions options) where TReader : struct, IBufReader, allows ref struct
    {
        throw new NotImplementedException();
    }
}

public class PrimitiveResolver : IFormatterResolver
{
    public static readonly PrimitiveResolver Instance = new();
    public IFormatter<T>? GetFormatter<T>()
    {
        var type = typeof(T);
        switch (Type.GetTypeCode(type))
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
        if (type.IsArray)
        {
            var itemType = type.GetElementType();
            if (itemType != null)
            {
                // Динамически создаем тип PrimitiveArrayFormatter<T, itemType>
                //    var formatterType = typeof(PrimitiveArrayFormatter<,>).MakeGenericType(type, itemType);
                // Создаем и возвращаем экземпляр форматтера
                //    return (IFormatter<T>?)Activator.CreateInstance(formatterType);
            }
        }
        return null;
    }
}
