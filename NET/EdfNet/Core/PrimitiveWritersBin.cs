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


    public static int TryRead(ReadOnlySpan<byte> src, EdfType edfType, out object? obj)
    {
        return edfType.Type switch
        {
            PoType.Char => TryReadChar(src, null == edfType.Dims ? 0 : edfType.Dims[0], out obj),
            PoType.UInt8 => TryRead<byte>(src, out obj),
            PoType.Int8 => TryRead<sbyte>(src, out obj),
            PoType.UInt16 => TryRead<ushort>(src, out obj),
            PoType.Int16 => TryRead<short>(src, out obj),
            PoType.UInt32 => TryRead<uint>(src, out obj),
            PoType.Int32 => TryRead<int>(src, out obj),
            PoType.UInt64 => TryRead<ulong>(src, out obj),
            PoType.Int64 => TryRead<long>(src, out obj),
            PoType.Single => TryRead<float>(src, out obj),
            PoType.Double => TryRead<double>(src, out obj),
            PoType.String => TryReadString(src, out obj),
            _ => throw new EdfException($"Unsupported type: {edfType.Type}"),
        };
    }
    public static int ReadValue<T>(ReadOnlySpan<byte> dst, out T? val)
        where T : struct
    {
        var len = Marshal.SizeOf<T>();
        if (dst.Length < len)
        {
            val = default;
            return -1; //throw new EdfDstBufOverflowException();
        }
        val = MemoryMarshal.Read<T>(dst);
        return len;
    }
    public static int TryRead<T>(ReadOnlySpan<byte> dst, out object? val)
        where T : struct
    {
        var len = ReadValue<T>(dst, out var ret);
        val = ret;
        return len;
    }
    public static int TryReadCharValue(ReadOnlySpan<byte> src, int edfLen, out byte[]? charArray)
    {
        if (src.Length < edfLen)
        {
            charArray = default;
            return -1; //throw new EdfDstBufOverflowException();
        }
        var ret = new byte[edfLen];
        src.Slice(0, edfLen).CopyTo(ret);
        charArray = ret;
        return edfLen;
    }
    public static int TryReadChar(ReadOnlySpan<byte> src, int edfLen, out object? charArray)
    {
        var len = TryReadCharValue(src, edfLen, out byte[]? ret);
        charArray = ret;
        return edfLen;
    }
    public static int TryReadString(ReadOnlySpan<byte> src, out object? str)
    {
        var len = EdfBinString.ReadBin(src, out var s);
        if (1 > len)
        {
            str = default;
            return -1; //throw new EdfDstBufOverflowException();
        }
        str = s;
        return len;
    }
}
