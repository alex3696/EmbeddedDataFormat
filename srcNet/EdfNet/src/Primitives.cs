namespace NetEdf.src;

public static class Primitives
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="t"></param>
    /// <param name="obj">source data object</param>
    /// <param name="dst">>destination stream</param>
    /// <returns>writed bytes count</returns>
    /// <exception cref="OverflowException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    public static int SrcToBin(PoType t, object obj, Stream dst)
    {
        Span<byte> b = stackalloc byte[t.GetSizeOf()];
        int w = SrcToBin(t, obj, b);
        dst.Write(b.Slice(0, w));
        return w;
    }
    /// <summary>
    /// 
    /// </summary>
    /// <param name="t"></param>
    /// <param name="obj">source data object</param>
    /// <param name="dst">destination memory buffer</param>
    /// <returns>writed bytes count</returns>
    /// <exception cref="OverflowException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    public static int SrcToBin(PoType t, object obj, Span<byte> dst)
    {
        var ret = TrySrcToBin(t, obj, dst, out var w);
        switch (ret)
        {
            default: break;
            case EdfErr.DstBufOverflow: throw new OverflowException();
            case EdfErr.WrongType: throw new NotSupportedException($"{t}");
        }
        return w;
    }
    /// <summary>
    /// Convert primitive to binary
    /// </summary>
    /// <param name="t"></param>
    /// <param name="obj"></param>
    /// <param name="dst"></param>
    /// <returns>error code, 0 when OK</returns>
    public static EdfErr TrySrcToBin(PoType t, object obj, Span<byte> dst, out int w)
    {
        w = t.GetSizeOf();
        if (dst.Length < w)
            return EdfErr.DstBufOverflow;
        switch (t)
        {
            case PoType.Struct:
            default: w = 0; return EdfErr.WrongType;
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
                    return EdfErr.DstBufOverflow;
                w = len;
                break;
        }
        return EdfErr.IsOk;
    }
    public static EdfErr BinToSrc(PoType t, ReadOnlySpan<byte> src, ref int r, out object? obj)
    {
        r = t.GetSizeOf();
        obj = default;
        switch (t)
        {
            case PoType.Struct:
            default: r = 0; return EdfErr.WrongType;
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
        return EdfErr.IsOk;
    }

}
