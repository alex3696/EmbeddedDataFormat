namespace NetEdf.StoreTypes;

public class VarString
{
    public static int Copy(ReadOnlySpan<byte> src, Span<byte> dst, out int readed, out int writed)
    {
        int blength = VarInt.DecodeUInt32(src, out uint strlen);
        readed = writed = 0;
        if (0 < blength)
        {
            blength += (int)strlen;
            if (src.Length < blength)
                return -1;
            if (dst.Length < blength)
                return 1;
            src.Slice(0, blength).CopyTo(dst);
            readed = writed = blength;
            return 0;
        }
        return 0;
    }

    public static bool TryParse(ReadOnlySpan<byte> b, out string? str, out int srcLen)
    {
        int blength = VarInt.DecodeUInt32(b, out uint strlen);
        if (0 < blength)
        {
            str = Encoding.UTF8.GetString(b.Slice(blength, (int)strlen));
            srcLen = blength + (int)strlen;
            return true;
        }
        srcLen = 0;
        str = null;
        return false;
    }
    public static string Parse(ReadOnlySpan<byte> b)
    {
        if (TryParse(b, out string? str, out _))
            return str ?? string.Empty;
        return string.Empty;
    }

    public static byte[] GetBytes(string? str)
    {
        if (string.IsNullOrEmpty(str))
            return [];
        var strlen = Encoding.UTF8.GetByteCount(str);
        var sizelen = VarInt.EncodeUInt32((uint)strlen);
        var b = new byte[sizelen.Length + strlen];
        sizelen.CopyTo(b, 0);
        Encoding.UTF8.GetBytes(str, b.AsSpan(sizelen.Length, strlen));
        return b;
    }
    public static int GetBytes(string? str, Span<byte> b)
    {
        if (string.IsNullOrEmpty(str))
            return 0;
        var strlen = Encoding.UTF8.GetByteCount(str);
        var sizelen = VarInt.EncodeUInt32((uint)strlen, b); // Write size
        Encoding.UTF8.GetBytes(str, b.Slice(sizelen, strlen));// Write string
        return strlen;
    }

}
