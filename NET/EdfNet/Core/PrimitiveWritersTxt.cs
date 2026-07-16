using System.Globalization;

namespace EdfNet.Core;

public class PrimitiveWritersTxt
{
    public static int TryWrite(Stream dst, EdfType edfType, object obj)
    {
        return edfType.Type switch
        {
            PoType.Char => PrimitiveWritersTxt.TryWriteChar(dst, (byte[])obj, null == edfType.Dims ? 0 : edfType.Dims[0]),
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
            PoType.String => TryWriteString(dst, (string?)obj),
            _ => throw new EdfException($"Unsupported type: {edfType.Type}"),
        };
    }
    public static int TryWrite<T>(Stream dst, T val)
        where T : IUtf8SpanFormattable
    {
        Span<byte> buf = stackalloc byte[128];
        try
        {
            if (val.TryFormat(buf, out int w, default, CultureInfo.InvariantCulture))
            {
                dst.Write(buf.Slice(0, w));
                return w;
            }
        }
        catch (Exception)
        {
        }
        return -1;
    }
    public static int TryWriteChar(Stream dst, byte[] src, int edfLen)
    {
        int i = 0;
        dst.WriteByte(34);
        for (; i < int.Min(edfLen, src.Length); i++)
        {
            if (i + 1 > dst.Length)
                return -1;
            if (0 == src[i])
                break;
            dst.WriteByte(src[i]);
        }
        dst.WriteByte(34);
        return i + 2;
    }
    public static int TryWriteString(Stream dst, string? str)
    {
        str ??= string.Empty;
        dst.WriteByte(34);
        var len = (byte)int.Min(EdfBinString.MaxLen, Encoding.UTF8.GetByteCount(str));
        dst.Write(Encoding.UTF8.GetBytes(str).AsSpan(0, len));
        dst.WriteByte(34);
        return len + 2;
    }
}
