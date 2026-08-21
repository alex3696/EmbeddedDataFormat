using System.Buffers;

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
public class PrimitiveArrayFormatter<TARRAY, TITEM> : IFormatter<TARRAY>
    where TITEM : struct
{
    public void Serialize<TWriter>(ref TWriter writer, in TARRAY arrObj, EdfOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        if (arrObj is not Array arr)
            throw new ArgumentException("Invalid array type");
        var edfType = writer.CurrentType;
        if (null == edfType)
            throw new InvalidOperationException("Current type is not an array or has no dimensions.");
        if(PoType.Char == edfType.Type && arrObj is byte[] chArr)
        {
            writer.WriteCharArray(chArr, (int)edfType.GetTotalElements());
            return;
        }
        int[] dims = null!;
        try
        {
            int ranks = edfType.Dims.Length;
            dims = ArrayPool<int>.Shared.Rent(ranks);
            for (int i = 0; i < ranks; i++)
            {
                if (arr.GetLength(i) != edfType.Dims[i])
                    throw new InvalidOperationException($"Array rank mismatch at dimension {i}. Expected {edfType.Dims[i]}, got {arr.GetLength(i)}.");
                dims[i] = edfType.Dims[i];
            }
            for (int i = 0; i < arr.Length; i++)
            {
                ref TITEM item = ref arr.GetElementAtFlatIndexUnsafe<TITEM>(i);
                writer.Write<TITEM>(item);
            }
        }
        finally
        {
            if (dims != null)
                ArrayPool<int>.Shared.Return(dims);
        }
    }
    public TARRAY Deserialize<TReader>(ref TReader reader, EdfOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        var edfType = reader.GetCurrentType();
        if (null == edfType)
            throw new InvalidOperationException("Current type is not an array or has no dimensions.");
        if (PoType.Char == edfType.Type)
        {
            var len = (int)edfType.GetTotalElements();
            var chArr = reader.ReadCharArray(len) as Array;
            return Unsafe.As<Array, TARRAY>(ref chArr);
        }
        int[] dims = null!;
        try
        {
            int ranks = edfType.Dims.Length;
            dims = ArrayPool<int>.Shared.Rent(ranks);
            for (int i = 0; i < ranks; i++)
                dims[i] = edfType.Dims[i];
            var arr = Array.CreateInstanceFromArrayType(typeof(TARRAY), dims);
            reader.ReadBeginArray();
            for (int i = 0; i < arr.Length; i++)
            {
                arr.GetElementAtFlatIndexUnsafe<TITEM>(i) = reader.Read<TITEM>();
            }
            reader.ReadEndArray();
            return Unsafe.As<Array, TARRAY>(ref arr);//return (TARRAY)(object)arr;
        }
        finally
        {
            if (dims != null)
                ArrayPool<int>.Shared.Return(dims);
        }
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
                switch (Type.GetTypeCode(itemType))
                {
                    default: break;
                    case TypeCode.Byte: return new PrimitiveArrayFormatter<T, byte>();
                    case TypeCode.SByte: return new PrimitiveArrayFormatter<T, sbyte>();
                    case TypeCode.Int16: return new PrimitiveArrayFormatter<T, short>();
                    case TypeCode.UInt16: return new PrimitiveArrayFormatter<T, ushort>();
                    case TypeCode.Int32: return new PrimitiveArrayFormatter<T, int>();
                    case TypeCode.UInt32: return new PrimitiveArrayFormatter<T, uint>();
                    case TypeCode.Int64: return new PrimitiveArrayFormatter<T, long>();
                    case TypeCode.UInt64: return new PrimitiveArrayFormatter<T, ulong>();
                    case TypeCode.Single: return new PrimitiveArrayFormatter<T, float>();
                    case TypeCode.Double: return new PrimitiveArrayFormatter<T, double>();
                }
            }
        }
        return null;
    }
}
