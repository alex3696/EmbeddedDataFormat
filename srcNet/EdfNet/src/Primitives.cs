using NetEdf.StoreTypes;

namespace NetEdf.src;

// Особенности строк в .NET
// https://habr.com/ru/articles/172627/

public static class ArrayExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static byte[] GetBytes<T>(this T[] arr)
        where T : struct
    {
        return MemoryMarshal.Cast<T, byte>(arr).ToArray();
    }

}


public static class Primitives
{
    public static int SrcToBin<T>(PoType t, object obj, Span<byte> dst)
    {
        var needLen = t.GetSizeOf();
        if (dst.Length < needLen)
            return dst.Length - needLen;
        switch (t)
        {
            default: break;
            case PoType.Int8: dst[0] = (byte)obj; return 1;
            case PoType.UInt8: dst[0] = (byte)obj; return 1;
            case PoType.Int16: BinaryPrimitives.WriteInt16LittleEndian(dst, (short)obj); return 2;
            case PoType.UInt16: BinaryPrimitives.WriteUInt16LittleEndian(dst, (ushort)obj); return 2;
            case PoType.Int32: BinaryPrimitives.WriteInt32LittleEndian(dst, (int)obj); return 4;
            case PoType.UInt32: BinaryPrimitives.WriteUInt32LittleEndian(dst, (uint)obj); return 4;
            case PoType.Int64: BinaryPrimitives.WriteInt64LittleEndian(dst, (long)obj); return 8;
            case PoType.UInt64: BinaryPrimitives.WriteUInt64LittleEndian(dst, (ulong)obj); return 8;
            case PoType.Half: BinaryPrimitives.WriteHalfLittleEndian(dst, (Half)obj); return 2;
            case PoType.Single: BinaryPrimitives.WriteSingleLittleEndian(dst, (float)obj); return 4;
            case PoType.Double: BinaryPrimitives.WriteDoubleLittleEndian(dst, (double)obj); return 8;
            case PoType.Char: dst[0] = (byte)obj; return 1;
            case PoType.String:
                int len = EdfBinString.WriteBin((string)obj, dst);
                return 0 > len ? -len : 0;
        }
        return -2;
    }

    public static int PackToPack(PoType t, ReadOnlySpan<byte> src, Span<byte> dst, out int srcSz, out int dstSz)
    {
        if (src.Length < 1)
        {
            srcSz = dstSz = 0;
            return -1;
        }
        switch (t)
        {
            default:
                srcSz = dstSz = 0;
                return 0;
            case PoType.Int8:
            case PoType.UInt8:
                dst[0] = src[0];
                dstSz = srcSz = 1;
                return 0;
            case PoType.Int16:
                dstSz = srcSz = VarInt.DecodeInt16(src, out var i16);
                if (0 > srcSz)
                    return -1;
                if (dst.Length < dstSz)
                    return 1;
                VarInt.EncodeInt16(i16, dst);
                return 0;
            case PoType.UInt16:
                dstSz = srcSz = VarInt.DecodeUInt16(src, out var u16);
                if (0 > srcSz)
                    return -1;
                if (dst.Length < dstSz)
                    return 1;
                VarInt.EncodeUInt16(u16, dst);
                return 0;
            case PoType.Int32:
                dstSz = srcSz = VarInt.DecodeInt32(src, out var i32);
                if (0 > srcSz)
                    return -1;
                if (dst.Length < dstSz)
                    return 1;
                VarInt.EncodeInt32(i32, dst);
                return 0;
            case PoType.UInt32:
                dstSz = srcSz = VarInt.DecodeUInt32(src, out var u32);
                if (0 > srcSz)
                    return -1;
                if (dst.Length < dstSz)
                    return 1;
                VarInt.EncodeUInt32(u32, dst);
                return 0;
            case PoType.Int64:
                dstSz = srcSz = VarInt.DecodeInt64(src, out var i64);
                if (0 > srcSz)
                    return -1;
                if (dst.Length < dstSz)
                    return 1;
                VarInt.EncodeInt64(i64, dst);
                return 0;
            case PoType.UInt64:
                dstSz = srcSz = VarInt.DecodeUInt64(src, out var u64);
                if (0 > srcSz)
                    return -1;
                if (dst.Length < dstSz)
                    return 1;
                VarInt.EncodeUInt64(u64, dst);
                return 0;
            case PoType.Half:
            case PoType.Single:
            case PoType.Double:
                srcSz = dstSz = t.GetSizeOf();
                if (src.Length < srcSz)
                    return -1;
                if (dst.Length < dstSz)
                    return 1;
                src.Slice(0, dstSz).CopyTo(dst);
                return 0;
            case PoType.String:
                return BString.Copy(src, dst, out srcSz, out dstSz);
        }
    }
    public static int BinToPack(PoType t, ReadOnlySpan<byte> src, Span<byte> dst, out int srcSz, out int dstSz)
    {
        byte[] r;
        srcSz = t.GetSizeOf();
        if (src.Length < srcSz)
        {
            dstSz = 0;
            return -1;
        }
        switch (t)
        {
            default:
                srcSz = dstSz = 0;
                return -2;
            case PoType.Int8:
            case PoType.UInt8: r = [src[0]]; break;
            case PoType.UInt16: r = VarInt.EncodeUInt16(BinaryPrimitives.ReadUInt16LittleEndian(src)); break;
            case PoType.UInt32: r = VarInt.EncodeUInt32(BinaryPrimitives.ReadUInt32LittleEndian(src)); break;
            case PoType.UInt64: r = VarInt.EncodeUInt64(BinaryPrimitives.ReadUInt64LittleEndian(src)); break;
            case PoType.Int16: r = VarInt.EncodeInt16(BinaryPrimitives.ReadInt16LittleEndian(src)); break;
            case PoType.Int32: r = VarInt.EncodeInt32(BinaryPrimitives.ReadInt32LittleEndian(src)); break;
            case PoType.Int64: r = VarInt.EncodeInt64(BinaryPrimitives.ReadInt64LittleEndian(src)); break;
            case PoType.Half:
            case PoType.Single:
            case PoType.Double: r = src.Slice(0, srcSz).ToArray(); break;
            case PoType.String: return BString.Copy(src, dst, out srcSz, out dstSz);
        }
        if (dst.Length < r.Length)
        {
            dstSz = 0;
            return 1;
        }
        r.CopyTo(dst);
        dstSz = r.Length;
        return 0;
    }
    public static int PackToBin(PoType t, ReadOnlySpan<byte> src, Span<byte> dst, out int srcSz, out int dstSz)
    {
        dstSz = t.GetSizeOf();
        if (src.Length < 1)
        {
            srcSz = 0;
            return -1;
        }
        if (dst.Length < dstSz)
        {
            srcSz = 0;
            return 1;
        }
        switch (t)
        {
            default:
                srcSz = 0;
                return 0;
            case PoType.Int8:
            case PoType.UInt8:
                dst[0] = src[0];
                dstSz = srcSz = 1;
                return 0;
            case PoType.Int16:
                srcSz = VarInt.DecodeInt16(src, out Int16 i16);
                if (0 > srcSz)
                    return -1;
                BinaryPrimitives.TryWriteInt16LittleEndian(dst, i16);
                return 0;
            case PoType.UInt16:
                srcSz = VarInt.DecodeUInt16(src, out UInt16 u16);
                if (0 > srcSz)
                    return -1;
                BinaryPrimitives.TryWriteUInt16LittleEndian(dst, u16);
                return 0;
            case PoType.Int32:
                srcSz = VarInt.DecodeInt32(src, out Int32 i32);
                if (0 > srcSz)
                    return -1;
                BinaryPrimitives.TryWriteInt32LittleEndian(dst, i32);
                return 0;
            case PoType.UInt32:
                srcSz = VarInt.DecodeUInt32(src, out UInt32 u32);
                if (0 > srcSz)
                    return -1;
                BinaryPrimitives.TryWriteUInt32LittleEndian(dst, u32);
                return 0;
            case PoType.Int64:
                srcSz = VarInt.DecodeInt64(src, out Int64 i64);
                if (0 > srcSz)
                    return -1;
                BinaryPrimitives.TryWriteInt64LittleEndian(dst, i64);
                return 0;
            case PoType.UInt64:
                srcSz = VarInt.DecodeUInt64(src, out UInt64 u64);
                if (0 > srcSz)
                    return -1;
                BinaryPrimitives.TryWriteUInt64LittleEndian(dst, u64);
                return 0;
            case PoType.Half:
            case PoType.Single:
            case PoType.Double:
                srcSz = dstSz;
                if (src.Length < srcSz)
                    return -1;
                src.Slice(0, dstSz).CopyTo(dst);
                return 0;
            case PoType.String:
                return BString.Copy(src, dst, out srcSz, out dstSz);
        }
    }
    public static int PackToStr(PoType t, ReadOnlySpan<byte> src, Span<byte> dst, out int srcSz, out int dstSz)
    {
        dstSz = t.GetSizeOf();
        if (src.Length < 1)
        {
            srcSz = 0;
            return -1;
        }
        if (dst.Length < dstSz)
        {
            srcSz = 0;
            return 1;
        }
        switch (t)
        {
            default:
                srcSz = 0;
                return 0;
            case PoType.Int8:
            case PoType.UInt8:
                srcSz = 1;
                return 0 > srcSz ? -1 : ObjToBytes(src[0], dst, out dstSz);
            case PoType.Int16:
                srcSz = VarInt.DecodeInt16(src, out var i16);
                return 0 > srcSz ? -1 : ObjToBytes(i16, dst, out dstSz);
            case PoType.UInt16:
                srcSz = VarInt.DecodeUInt16(src, out var u16);
                return 0 > srcSz ? -1 : ObjToBytes(u16, dst, out dstSz);
            case PoType.Int32:
                srcSz = VarInt.DecodeInt32(src, out var i32);
                return 0 > srcSz ? -1 : ObjToBytes(i32, dst, out dstSz);
            case PoType.UInt32:
                srcSz = VarInt.DecodeUInt32(src, out var u32);
                return 0 > srcSz ? -1 : ObjToBytes(u32, dst, out dstSz);
            case PoType.Int64:
                srcSz = VarInt.DecodeInt64(src, out var i64);
                return 0 > srcSz ? -1 : ObjToBytes(i64, dst, out dstSz);
            case PoType.UInt64:
                srcSz = VarInt.DecodeUInt64(src, out var u64);
                return 0 > srcSz ? -1 : ObjToBytes(u64, dst, out dstSz);
            case PoType.Half:
                srcSz = 2;
                return src.Length < srcSz ? -1 : ObjToBytes(BinaryPrimitives.ReadHalfLittleEndian(src), dst, out dstSz);
            case PoType.Single:
                srcSz = 4;
                return src.Length < srcSz ? -1 : ObjToBytes(BinaryPrimitives.ReadSingleLittleEndian(src), dst, out dstSz);
            case PoType.Double:
                srcSz = 8;
                return src.Length < srcSz ? -1 : ObjToBytes(BinaryPrimitives.ReadDoubleLittleEndian(src), dst, out dstSz);
            case PoType.String:
                if (BString.TryParse(src, out var str, out srcSz))
                    return StringToBytes($"\"{str}\"", dst, out dstSz);
                return -1;
        }
    }

    public static int BinToBin(PoType t, ReadOnlySpan<byte> src, Span<byte> dst, out int srcSz, out int dstSz)
    {
        dstSz = srcSz = t.GetSizeOf();
        if (src.Length < srcSz)
            return -1;
        if (dst.Length < dstSz)
            return 1;
        switch (t)
        {
            default: srcSz = dstSz = 0; return -2;
            case PoType.Int8:
            case PoType.UInt8:
            case PoType.UInt16:
            case PoType.UInt32:
            case PoType.UInt64:
            case PoType.Int16:
            case PoType.Int32:
            case PoType.Int64:
            case PoType.Half:
            case PoType.Single:
            case PoType.Double: src.Slice(0, srcSz).CopyTo(dst.Slice(0, dstSz)); break;
            case PoType.Char: return CString.Copy(src, dst, out srcSz, out dstSz);
            case PoType.String: return BString.Copy(src, dst, out srcSz, out dstSz);
        }
        return 0;
    }
    public static int BinToStr(PoType t, ReadOnlySpan<byte> src, Span<byte> dst, out int srcSz, out int dstSz)
    {
        srcSz = t.GetSizeOf();
        if (src.Length < srcSz)
        {
            dstSz = 0;
            return -1;
        }
        if (dst.Length < 1)
        {
            dstSz = 0;
            return 1;
        }
        switch (t)
        {
            default:
                dstSz = 0;
                return 0;
            case PoType.Int8:
            case PoType.UInt8:
                return ObjToBytes(src[0], dst, out dstSz);
            case PoType.Int16:
                return ObjToBytes(BinaryPrimitives.ReadInt16LittleEndian(src), dst, out dstSz);
            case PoType.UInt16:
                return ObjToBytes(BinaryPrimitives.ReadUInt16LittleEndian(src), dst, out dstSz);
            case PoType.Int32:
                return ObjToBytes(BinaryPrimitives.ReadInt32LittleEndian(src), dst, out dstSz);
            case PoType.UInt32:
                return ObjToBytes(BinaryPrimitives.ReadUInt32LittleEndian(src), dst, out dstSz);
            case PoType.Int64:
                return ObjToBytes(BinaryPrimitives.ReadInt64LittleEndian(src), dst, out dstSz);
            case PoType.UInt64:
                return ObjToBytes(BinaryPrimitives.ReadUInt64LittleEndian(src), dst, out dstSz);
            case PoType.Half:
                return ObjToBytes(BinaryPrimitives.ReadHalfLittleEndian(src), dst, out dstSz);
            case PoType.Single:
                return ObjToBytes(BinaryPrimitives.ReadSingleLittleEndian(src), dst, out dstSz);
            case PoType.Double:
                return ObjToBytes(BinaryPrimitives.ReadDoubleLittleEndian(src), dst, out dstSz);
            case PoType.Char:
                if (CString.TryParse(src, out var cstr, out srcSz))
                    return StringToBytes($"\"{cstr}\"", dst, out dstSz);
                dstSz = 0;
                return -1;
            case PoType.String:
                if (BString.TryParse(src, out var str, out srcSz))
                    return StringToBytes($"\"{str}\"", dst, out dstSz);
                dstSz = 0;
                return -1;
        }
    }

    static int ObjToBytes<T>(T? obj, Span<byte> dst, out int dstSz)
    {
        string? str = Convert.ToString(obj, CultureInfo.InvariantCulture);
        if (string.IsNullOrEmpty(str))
        {
            dstSz = 0;
            return -1;
        }
        return StringToBytes(str, dst, out dstSz);
    }
    static int StringToBytes(string str, Span<byte> dst, out int dstSz)
    {
        var bb = Encoding.UTF8.GetBytes(str);
        dstSz = bb.Length;
        if (dst.Length < dstSz)
            return 1;
        bb.CopyTo(dst);
        return 0;
    }
}
