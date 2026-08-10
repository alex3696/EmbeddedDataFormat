using System.Buffers.Text;
using System.Globalization;

namespace EdfNet.Core;

public class PrimitiveWritersTxt
{
    public static int TryRead<T>(ReadOnlySpan<byte> src, out T? val)
        where T : struct
    {
        int w;
        switch (Type.GetTypeCode(typeof(T)))
        {
            case TypeCode.Byte:
                if (!Utf8Parser.TryParse(src, out byte vByte, out w))
                    break;
                val = Unsafe.As<byte, T>(ref vByte);
                return w;
            case TypeCode.SByte:
                if (!Utf8Parser.TryParse(src, out sbyte vSByte, out w))
                    break;
                val = Unsafe.As<sbyte, T>(ref vSByte);
                return w;
            default: break;
        }
        throw new NotSupportedException($"Тип {typeof(T).Name} не поддерживается.");
    }

    public static int TryWrite(Span<byte> dst, EdfType edfType, object obj)
    {
        return edfType.Type switch
        {
            PoType.Char => TryWriteChar(dst, (byte[])obj, null == edfType.Dims ? 0 : edfType.Dims[0]),
            PoType.UInt8 => TryWrite(dst, (byte)obj),
            PoType.Int8 => TryWrite(dst, (sbyte)obj),
            PoType.UInt16 => TryWrite(dst, (ushort)obj),
            PoType.Int16 => TryWrite(dst, (int)obj),
            PoType.UInt32 => TryWrite(dst, (uint)obj),
            PoType.Int32 => TryWrite(dst, (int)obj),
            PoType.UInt64 => TryWrite(dst, (ulong)obj),
            PoType.Int64 => TryWrite(dst, (long)obj),
            PoType.Single => TryWrite(dst, (float)obj),
            PoType.Double => TryWrite(dst, (double)obj),
            PoType.String => EdfBinString.WriteTxt((string?)obj, dst),
            _ => throw new EdfException($"Unsupported type: {edfType.Type}"),
        };
    }
    public static int TryWrite<T>(Span<byte> dst, T val)
        where T : IUtf8SpanFormattable
    {
        try
        {
            if (val.TryFormat(dst, out int w, default, CultureInfo.InvariantCulture))
                return w;
        }
        catch (Exception)
        {
        }
        return -1;
    }
    public static int TryWriteChar(Span<byte> dst, byte[] src, int edfLen)
    {
        var len = (byte)int.Min(edfLen, src.Length);
        int firstZero = Array.FindIndex(src, (byte nn) => nn == 0);
        if (0 < firstZero && 256 > firstZero)
            len = (byte)int.Min(len, firstZero);
        if (2 + len > dst.Length)
            return -1;
        dst[0] = 34;
        src.AsSpan(0, len).CopyTo(dst.Slice(1, len));
        dst[len + 1] = 34;
        return 2 + len;
    }
}
