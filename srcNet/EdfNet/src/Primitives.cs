namespace NetEdf.src;

public static class Primitives
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="dst">>destination stream</param>
    /// <param name="t"></param>
    /// <param name="obj">source data object</param>
    /// <returns>writed bytes count</returns>
    /// <exception cref="OverflowException"></exception>
    /// <exception cref="NotSupportedException"></exception>
    public static int SrcToBin(this Stream dst, PoType t, object obj)
    {
        Span<byte> b = stackalloc byte[t.GetSizeOf()];
        var w = SrcToBin(b, t, obj);
        dst.Write(b.Slice(0, w));
        return w;
    }
    public static int SrcToBinRef(this ref Span<byte> dst, PoType t, object obj)
    {
        var w = SrcToBin(dst, t, obj);
        dst = dst.Slice(w);
        return w;
    }
    public static int SrcToBin(this Span<byte> dst, PoType t, object obj)
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
            case PoType.UInt8: MemoryMarshal.Write(dst, (byte)obj); break;
            case PoType.Int8: MemoryMarshal.Write(dst, (sbyte)obj); break;
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
    public static EdfErr TryBinToSrc(PoType t, ReadOnlySpan<byte> src, out int r, out object? obj)
    {
        obj = default;
        r = t.GetSizeOf();
        if (r > src.Length)
            return EdfErr.SrcDataRequred;
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
                if (0 >= r)
                    return EdfErr.SrcDataRequred;
                obj = str;
                break;
        }
        return EdfErr.IsOk;
    }

    public static EdfErr TryFormat<T>(PoType t, T obj, Span<byte> dst, out int w)
        where T : IUtf8SpanFormattable
    {
        if (obj.TryFormat(dst, out w, default, null))
            return EdfErr.IsOk;
        return EdfErr.DstBufOverflow;
    }
    public static EdfErr TrySrcToTxt(PoType t, object obj, Span<byte> dst, out int w)
    {
        switch (t)
        {
            case PoType.Struct:
            default: w = 0; break;
            case PoType.Char:
            case PoType.UInt8: return TryFormat(t, (byte)obj, dst, out w);
            case PoType.Int8: return TryFormat(t, (sbyte)obj, dst, out w);
            case PoType.UInt16: return TryFormat(t, (ushort)obj, dst, out w);
            case PoType.Int16: return TryFormat(t, (short)obj, dst, out w);
            case PoType.UInt32: return TryFormat(t, (uint)obj, dst, out w);
            case PoType.Int32: return TryFormat(t, (int)obj, dst, out w);
            case PoType.UInt64: return TryFormat(t, (ulong)obj, dst, out w);
            case PoType.Int64: return TryFormat(t, (long)obj, dst, out w);
            case PoType.Half: return TryFormat(t, (Half)obj, dst, out w);
            case PoType.Single: return TryFormat(t, (float)obj, dst, out w);
            case PoType.Double: return TryFormat(t, (double)obj, dst, out w);
            case PoType.String:
                {
                    Span<byte> buf = stackalloc byte[256];
                    w = Encoding.UTF8.GetBytes((string)obj, buf);
                    if (w > dst.Length + 2)
                        return EdfErr.DstBufOverflow;
                    dst[0] = (byte)'"';
                    buf.Slice(0, w).CopyTo(dst.Slice(1));
                    dst[w + 1] = (byte)'"';
                    w += 2;
                    return EdfErr.IsOk;
                }
        }
        return EdfErr.WrongType;
    }
}

