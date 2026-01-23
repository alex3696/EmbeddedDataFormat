namespace NetEdf.StoreTypes;

public class BString
{
    public static int Copy(ReadOnlySpan<byte> src, Span<byte> dst, out int readed, out int writed)
    {
        int strlen = src[0];
        int blength = 1;
        readed = writed = 0;
        if (0 < blength)
        {
            blength += strlen;
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
        byte strlen = b[0];
        str = Encoding.UTF8.GetString(b.Slice(1, strlen));
        srcLen = 1 + strlen;
        return true;
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
            return [0];
        byte strlen = (byte)int.Min(0xFE, Encoding.UTF8.GetByteCount(str));
        var b = new byte[1 + strlen];
        b[0] = strlen;
        Encoding.UTF8.GetBytes(str, b.AsSpan(1, strlen));
        return b;
    }
    public static int GetBytes(string? str, Span<byte> b)
    {
        if (string.IsNullOrEmpty(str))
        {
            b[0] = 0;
            return 1;
        }
        byte strlen = (byte)int.Min(0xFF, Encoding.UTF8.GetByteCount(str));
        b[0] = strlen;
        Encoding.UTF8.GetBytes(str, b.Slice(1, strlen));// Write string
        return strlen;
    }

}
