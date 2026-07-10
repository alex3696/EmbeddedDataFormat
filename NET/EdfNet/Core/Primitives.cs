namespace EdfNet.Core;

public static class Primitives
{
    /// <summary>
    /// Convert primitive to binary
    /// </summary>
    /// <param name="t"></param>
    /// <param name="obj"></param>
    /// <param name="dst"></param>
    /// <returns>error code, 0 when OK</returns>
    public static EdfErr TrySrcToBin<TEnumerator>(PoType t, ref TEnumerator flatObj, Span<byte> dst, out int w)
        where TEnumerator : struct, IEdfByteEnumerator
    {
        w = t.GetSizeOf();
        if (dst.Length < w)
            return EdfErr.DstBufOverflow;
        if (t != flatObj.CurrentPoType)
            return EdfErr.WrongType;
        switch (t)
        {
            case PoType.Struct:
            default: w = 0; return EdfErr.WrongType;
            case PoType.Char: w = flatObj.Write(dst); break;
            case PoType.UInt8: w = flatObj.Write(dst); break;
            case PoType.Int8: w = flatObj.Write(dst); break;
            case PoType.UInt16: w = flatObj.Write(dst); break;
            case PoType.Int16: w = flatObj.Write(dst); break;
            case PoType.UInt32: w = flatObj.Write(dst); break;
            case PoType.Int32: w = flatObj.Write(dst); break;
            case PoType.UInt64: w = flatObj.Write(dst); break;
            case PoType.Int64: w = flatObj.Write(dst); break;
            case PoType.Half: w = flatObj.Write(dst); break;
            case PoType.Single: w = flatObj.Write(dst); break;
            case PoType.Double: w = flatObj.Write(dst); break;
            case PoType.String: w = flatObj.Write(dst); break;
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

    public static EdfErr TrySrcToTxt<TEnumerator>(PoType t, ref TEnumerator flatObj, Span<byte> dst, out int w)
        where TEnumerator : struct, IEdfByteEnumerator
    {
        if(PoType.Char == t && PoType.UInt8 == flatObj.CurrentPoType)
        {

        }

        w = flatObj.Write(dst);
        if (0 > w)
            return EdfErr.DstBufOverflow;
        return EdfErr.IsOk;
    }
}

