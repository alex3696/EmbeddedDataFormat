using EdfNet.Extensions;

namespace EdfNet.Core;

public class PrimitiveFormatterUInt8 : IFormatter<byte>
{
    public void Serialize<TWriter>(ref TWriter writer, in byte val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public byte Deserialize<TReader>(ref TReader reader, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        return reader.ReadUInt8();
    }
}
public class PrimitiveFormatterInt8 : IFormatter<sbyte>
{
    public void Serialize<TWriter>(ref TWriter writer, in sbyte val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public sbyte Deserialize<TReader>(ref TReader reader, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        return reader.ReadInt8();
    }
}
public class PrimitiveFormatterUInt16 : IFormatter<ushort>
{
    public void Serialize<TWriter>(ref TWriter writer, in ushort val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public ushort Deserialize<TReader>(ref TReader reader, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        return reader.ReadUInt16();
    }
}
public class PrimitiveFormatterInt16 : IFormatter<short>
{
    public void Serialize<TWriter>(ref TWriter writer, in short val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public short Deserialize<TReader>(ref TReader reader, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        return reader.ReadInt16();
    }
}
public class PrimitiveFormatterUInt32 : IFormatter<uint>
{
    public void Serialize<TWriter>(ref TWriter writer, in uint val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public uint Deserialize<TReader>(ref TReader reader, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        return reader.ReadUInt32();
    }
}
public class PrimitiveFormatterInt32 : IFormatter<int>
{
    public void Serialize<TWriter>(ref TWriter writer, in int val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public int Deserialize<TReader>(ref TReader reader, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        return reader.ReadInt32();
    }
}
public class PrimitiveFormatterUInt64 : IFormatter<ulong>
{
    public void Serialize<TWriter>(ref TWriter writer, in ulong val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public ulong Deserialize<TReader>(ref TReader reader, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        return reader.ReadUInt64();
    }
}
public class PrimitiveFormatterInt64 : IFormatter<long>
{
    public void Serialize<TWriter>(ref TWriter writer, in long val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public long Deserialize<TReader>(ref TReader reader, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        return reader.ReadInt64();
    }
}
public class PrimitiveFormatterSingle : IFormatter<float>
{
    public void Serialize<TWriter>(ref TWriter writer, in float val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public float Deserialize<TReader>(ref TReader reader, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        return reader.ReadSingle();
    }
}
public class PrimitiveFormatterDouble : IFormatter<double>
{
    public void Serialize<TWriter>(ref TWriter writer, in double val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public double Deserialize<TReader>(ref TReader reader, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        return reader.ReadDouble();
    }
}
public class PrimitiveFormatterString : IFormatter<string?>
{
    public void Serialize<TWriter>(ref TWriter writer, in string? val, Interfaces.EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public string? Deserialize<TReader>(ref TReader reader, Interfaces.EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        return reader.ReadString();
    }
}
public class PrimitiveArrayFormatter<TARRAY, TITEM> : IFormatter<TARRAY>
    where TITEM : struct, IBinaryNumber<TITEM>
{
    public void Serialize<TWriter>(ref TWriter writer, in TARRAY arrObj, Interfaces.EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        var edfType = writer.CurrentType;
        if (null == edfType || 0 == edfType.Dims.Length)
            throw new InvalidOperationException("Current type is null or not an array.");
        if (EdfPrimitiveType.Char == edfType.Type && arrObj is byte[] chArr)
        {
            writer.WriteCharArray(chArr);
            return;
        }
        if (arrObj is not Array arr)
            throw new ArgumentException("Invalid array type");
        for (int i = 0; i < arr.Length && ReferenceEquals(edfType, writer.CurrentType); i++)
        {
            ref TITEM item = ref arr.GetElementAtFlatIndexUnsafe<TITEM>(i);
            writer.Write(item);
        }
    }
    public TARRAY Deserialize<TReader>(ref TReader reader, Interfaces.EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        var edfType = reader.CurrentType;
        if (null == edfType)
            throw new InvalidOperationException("Current type is not an array or has no dimensions.");
        if (EdfPrimitiveType.Char == edfType.Type)
        {
            var len = (int)edfType.GetTotalElements();
            var chArr = reader.ReadCharArray() as Array;
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
            for (int i = 0; i < arr.Length; i++)
            {
                arr.GetElementAtFlatIndexUnsafe<TITEM>(i) = reader.Read<TITEM>();
            }
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
            case TypeCode.Byte: return (IFormatter<T>)new PrimitiveFormatterUInt8();
            case TypeCode.SByte: return (IFormatter<T>)new PrimitiveFormatterInt8();
            case TypeCode.Int16: return (IFormatter<T>)new PrimitiveFormatterInt16();
            case TypeCode.UInt16: return (IFormatter<T>)new PrimitiveFormatterUInt16();
            case TypeCode.Int32: return (IFormatter<T>)new PrimitiveFormatterInt32();
            case TypeCode.UInt32: return (IFormatter<T>)new PrimitiveFormatterUInt32();
            case TypeCode.Int64: return (IFormatter<T>)new PrimitiveFormatterInt64();
            case TypeCode.UInt64: return (IFormatter<T>)new PrimitiveFormatterUInt64();
            case TypeCode.Single: return (IFormatter<T>)new PrimitiveFormatterSingle();
            case TypeCode.Double: return (IFormatter<T>)new PrimitiveFormatterDouble();
            case TypeCode.String: return (IFormatter<T>)new PrimitiveFormatterString();
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
