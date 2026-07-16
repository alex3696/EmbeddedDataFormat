namespace EdfNet.Core;

public static class PrimitiveWritersBin
{
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
            PoType.String => TryWriteString(dst, (string?)obj),
            _ => throw new EdfException($"Unsupported type: {edfType.Type}"),
        };
    }
    public static int TryWrite<T>(Span<byte> dst, T val)
        where T : struct
    {
        var len = Marshal.SizeOf<T>();
        if (dst.Length < len)
            return -1; //throw new EdfDstBufOverflowException();
        MemoryMarshal.Write(dst, val);
        return len;
    }
    public static int TryWriteChar(Span<byte> dst, byte[] src, int edfLen)
    {
        int i = 0;
        for (; i < int.Min(edfLen, src.Length); i++)
        {
            if (i > dst.Length)
                return -1; //throw new EdfDstBufOverflowException();
            if (0 == src[i])
                break;
            dst[i] = src[i];
        }
        dst.Slice(i, edfLen - i).Clear();
        return edfLen;
    }
    public static int TryWriteString(Span<byte> dst, string? str)
    {
        var len = EdfBinString.WriteBin(str, dst);
        if (1 > len)
            return -1; //throw new EdfDstBufOverflowException();
        return len;
    }

}
