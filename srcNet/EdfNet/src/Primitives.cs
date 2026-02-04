namespace NetEdf.src;

public static class Primitives
{
    public const int ErrDstBufOverflow = 1;
    public const int ErrWrongType = -2;

    public static int SrcToBin(PoType t, object obj, Stream dst)
    {
        Span<byte> b = stackalloc byte[t.GetSizeOf()];
        int w = 0;
        var err = SrcToBin(t, obj, b, ref w);
        if (0 == err)
            dst.Write(b.Slice(0, w));
        return w;
    }
    /// <summary>
    /// Convert primitive to binary
    /// </summary>
    /// <param name="t"></param>
    /// <param name="obj"></param>
    /// <param name="dst"></param>
    /// <returns>error code, 0 when OK</returns>
    public static int SrcToBin(PoType t, object obj, Span<byte> dst, ref int w)
    {
        w = t.GetSizeOf();
        if (dst.Length < w)
            return ErrDstBufOverflow;
        switch (t)
        {
            case PoType.Struct:
            default: w = 0; return ErrWrongType;
            case PoType.Char:
            case PoType.UInt8: dst[0] = (byte)obj; break;
            case PoType.Int8: dst[0] = (byte)obj; break;
            case PoType.UInt16: MemoryMarshal.Write(dst, (ushort)obj); break;
            case PoType.Int16: MemoryMarshal.Write(dst, (short)obj); break;
            case PoType.UInt32: MemoryMarshal.Write(dst, (uint)obj); break;
            case PoType.Int32: MemoryMarshal.Write(dst, (int)obj); break;
            case PoType.UInt64: MemoryMarshal.Write(dst, (ulong)obj); break;
            case PoType.Int64: MemoryMarshal.Write(dst, (long)obj); break;
            case PoType.Half: MemoryMarshal.Write(dst, (Half)obj); break;
            case PoType.Single: MemoryMarshal.Write(dst, (float)obj); break;
            case PoType.Double: MemoryMarshal.Write(dst, (double)obj); break;
            case PoType.String:
                int len = EdfBinString.WriteBin((string)obj, dst);
                if (0 > len)
                    return ErrDstBufOverflow;
                w = len;
                break;
        }
        return 0;
    }
    public static int BinToSrc(PoType t, ReadOnlySpan<byte> src, ref int r, out object? obj)
    {
        r = t.GetSizeOf();
        obj = default;
        switch (t)
        {
            case PoType.Struct:
            default: r = 0; return ErrWrongType;
            case PoType.Char:
            case PoType.UInt8: obj = MemoryMarshal.Read<byte>(src); break;
            case PoType.Int8: obj = MemoryMarshal.Read<sbyte>(src); break;
            case PoType.UInt16: obj = MemoryMarshal.Read<ushort>(src); break;
            case PoType.Int16: obj = MemoryMarshal.Read<short>(src); break;
            case PoType.UInt32: obj = MemoryMarshal.Read<uint>(src); break;
            case PoType.Int32: obj = MemoryMarshal.Read<int>(src); break;
            case PoType.UInt64: obj = MemoryMarshal.Read<ulong>(src); break;
            case PoType.Int64: obj = MemoryMarshal.Read<long>(src); break;
            case PoType.Half: obj = MemoryMarshal.Read<Half>(src); break;
            case PoType.Single: obj = MemoryMarshal.Read<float>(src); break;
            case PoType.Double: obj = MemoryMarshal.Read<double>(src); break;
            case PoType.String:
                r = EdfBinString.ReadBin(src, out string? str);
                if (0 < r)
                    obj = str;
                break;
        }
        return 0;
    }

}
