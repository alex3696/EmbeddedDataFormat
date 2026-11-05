using EdfNet.Extensions;

namespace EdfNet.Core;

public class PrimitiveFormatterUInt8 : IFormatter<byte>
{
    public void Serialize<TWriter>(ref TWriter writer, in byte val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public void Deserialize<TReader>(ref TReader reader, ref byte val, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        val = reader.ReadUInt8();
    }
}
public class PrimitiveFormatterInt8 : IFormatter<sbyte>
{
    public void Serialize<TWriter>(ref TWriter writer, in sbyte val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public void Deserialize<TReader>(ref TReader reader, ref sbyte val, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        val = reader.ReadInt8();
    }
}
public class PrimitiveFormatterUInt16 : IFormatter<ushort>
{
    public void Serialize<TWriter>(ref TWriter writer, in ushort val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public void Deserialize<TReader>(ref TReader reader, ref ushort val, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        val = reader.ReadUInt16();
    }
}
public class PrimitiveFormatterInt16 : IFormatter<short>
{
    public void Serialize<TWriter>(ref TWriter writer, in short val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public void Deserialize<TReader>(ref TReader reader, ref short val, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        val = reader.ReadInt16();
    }
}
public class PrimitiveFormatterUInt32 : IFormatter<uint>
{
    public void Serialize<TWriter>(ref TWriter writer, in uint val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public void Deserialize<TReader>(ref TReader reader, ref uint val, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        val = reader.ReadUInt32();
    }
}
public class PrimitiveFormatterInt32 : IFormatter<int>
{
    public void Serialize<TWriter>(ref TWriter writer, in int val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public void Deserialize<TReader>(ref TReader reader, ref int val, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        val = reader.ReadInt32();
    }
}
public class PrimitiveFormatterUInt64 : IFormatter<ulong>
{
    public void Serialize<TWriter>(ref TWriter writer, in ulong val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public void Deserialize<TReader>(ref TReader reader, ref ulong val, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        val = reader.ReadUInt64();
    }
}
public class PrimitiveFormatterInt64 : IFormatter<long>
{
    public void Serialize<TWriter>(ref TWriter writer, in long val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public void Deserialize<TReader>(ref TReader reader, ref long val, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        val = reader.ReadInt64();
    }
}
public class PrimitiveFormatterSingle : IFormatter<float>
{
    public void Serialize<TWriter>(ref TWriter writer, in float val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public void Deserialize<TReader>(ref TReader reader, ref float val, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        val = reader.ReadSingle();
    }
}
public class PrimitiveFormatterDouble : IFormatter<double>
{
    public void Serialize<TWriter>(ref TWriter writer, in double val, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public void Deserialize<TReader>(ref TReader reader, ref double val, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        val = reader.ReadDouble();
    }
}
public class PrimitiveFormatterString : IFormatter<string?>
{
    public void Serialize<TWriter>(ref TWriter writer, in string? val, Interfaces.EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct
    {
        writer.Write(val);
    }
    public void Deserialize<TReader>(ref TReader reader, ref string? val, Interfaces.EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        val = reader.ReadString();
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
    public void Deserialize<TReader>(ref TReader reader, ref TARRAY val, Interfaces.EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct
    {
        var edfType = reader.CurrentType;
        if (null == edfType)
            throw new InvalidOperationException("Current type is not an array or has no dimensions.");
        if (EdfPrimitiveType.Char == edfType.Type)
        {
            byte[] chArr = reader.ReadCharArray();
            val = Unsafe.As<byte[], TARRAY>(ref chArr);
            return;
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
            val = Unsafe.As<Array, TARRAY>(ref arr);//return (TARRAY)(object)arr;
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
