namespace NetEdf;

public static class EdfBinString
{
    public static int SizeOf(string? str)
    {
        if (string.IsNullOrEmpty(str))
            return 1;
        return (byte)int.Min(0xFE, Encoding.UTF8.GetByteCount(str));
    }
    public static int WriteBin(string? str, Stream dst)
    {
        Span<byte> buf = stackalloc byte[256];
        var ret = WriteBin(str, buf);
        if (0 < ret)
        {
            dst.Write(buf.Slice(0, ret));
        }
        return ret;
    }
    public static int WriteBin(string? str, Span<byte> dst)
    {
        if (1 > dst.Length)
            return -1;
        if (string.IsNullOrEmpty(str))
        {
            dst[0] = 0;
            return 1;
        }
        var len = (byte)int.Min(0xFE, Encoding.UTF8.GetByteCount(str));
        if (len > dst.Length)
            return dst.Length - len;
        Encoding.UTF8.GetBytes(str, dst.Slice(1, len));
        dst[0] = len;
        return 1 + len;
    }
    public static int ReadBin(ReadOnlySpan<byte> b, out string? str)
    {
        if (byte.MaxValue == b[0])
            throw new ArgumentException("BString overflow");
        var len = b[0];
        str = Encoding.UTF8.GetString(b.Slice(1, len));
        return 1 + len;
    }
}
