namespace NetEdf.StoreTypes;

public class CString
{
    public static int Copy(ReadOnlySpan<byte> src, Span<byte> dst, out int readed, out int writed)
    {
        if (dst.Length < src.Length)
        {
            readed = writed = 0;
            return 1;
        }
        readed = writed = src.Length;
        src.CopyTo(dst);
        return 0;
    }

    public static bool TryParse(ReadOnlySpan<byte> b, out string? str, out int srcLen)
    {
        srcLen = b.Length;
        str = Encoding.UTF8.GetString(b);
        return true;
    }

    public static byte[] GetBytes(string? str, sbyte maxLen)
    {
        byte strlen = (byte)maxLen;
        var b = new byte[strlen]; //Array.Clear(b, 0, b.Length);
        Encoding.UTF8.GetBytes(str, b.AsSpan(0, strlen));
        return b;
    }

    /// <summary>
    /// null terminated cstring
    /// </summary>
    /// <param name="str"></param>
    /// <returns></returns>
    public static byte[] GetBytes(string? str)
    {
        byte strlen = (byte)(string.IsNullOrEmpty(str) ? 0 : int.Min(0xFE, Encoding.UTF8.GetByteCount(str)));
        var b = new byte[strlen + 1];
        Encoding.UTF8.GetBytes(str, b.AsSpan(0, strlen));
        return b;
    }
}
