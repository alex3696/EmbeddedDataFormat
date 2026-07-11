namespace EdfNet.Core;

public static class Primitives
{
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

}

