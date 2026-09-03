namespace Test.BinSiamFormat;

public static class EncodingExt
{
    public static int GetBytes(this Encoding enc, string s, Span<byte> dst)
    {
        var tmp = new byte[dst.Length];
        var b = enc.GetBytes(s, 0, s.Length, tmp, 0);
        tmp.AsSpan(0, b).CopyTo(dst);
        return b;
    }
    public static string GetString(this Encoding enc, Span<byte> src)
    {
        var tmp = src.ToArray();
        return enc.GetString(tmp, 0, tmp.Length);
    }
}

public static class StringTool
{
    public static readonly char[] TrailingSymbols = new char[] { '\0', '\t', '\n', '\r', ' ' };

    public static void SetStringUtf8(string? str, byte[] dst, bool removeBreakSymols = true)
    {
        Array.Clear(dst, 0, dst.Length);
        if (null == str)
            return;
        str = str.TrimEnd(TrailingSymbols);
        if (0 == str.Length)
            return;
        System.Text.Encoding.UTF8.GetBytes(str, 0, Math.Min(str.Length, dst.Length), dst, 0);
    }
    public static void SetStringUtf8(string? str, Span<byte> dst, bool removeBreakSymols = true)
    {
        dst.Clear();
        if (null == str)
            return;
        str = str.TrimEnd(TrailingSymbols);
        if (0 == str.Length)
            return;
        System.Text.Encoding.UTF8.GetBytes(str).CopyTo(dst);
    }
    public static string GetStringUtf8(byte[] bytes, bool removeBreakSymols = true)
    {
        return GetStringUtf8(bytes, 0, bytes.Length, removeBreakSymols);
    }
    public static string GetStringUtf8(byte[] bytes, int index, int count, bool removeBreakSymols = true)
    {
        try
        {
            Encoding enc = Encoding.UTF8;
            return GetString(enc, bytes, index, count, removeBreakSymols);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Can`t get encoding UTF8 {ex}");
        }
        return string.Empty;
    }
    //public static string GetString1251(byte[] bytes)
    //{
    //    return GetString1251(bytes, 0, bytes.Length);
    //}
    public static string GetString1251(byte[] bytes, int index, int count, bool removeBreakSymols = true)
    {
        try
        {
            Encoding enc = Encoding.GetEncoding(1251);
            return GetString(enc, bytes, index, count, removeBreakSymols);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Can`t get encoding 1251 {ex}");
        }
        return string.Empty;
    }
    private static string GetString(Encoding enc, byte[] bytes, int index, int count, bool removeBreakSymols)
    {
        try
        {
            if (null == enc)
                enc = Encoding.Default;
            var zeroIdx = Array.FindIndex(bytes, index, count, (x) => 0 == x);
            if (0 <= zeroIdx)
                count = zeroIdx - index;
            var str = enc.GetString(bytes, index, count);
            str = str.TrimEnd(TrailingSymbols);
            if (removeBreakSymols)
            {
                var chars = str.Where(ch => 31 < ch).ToArray();
                str = "";
                foreach (var ch in chars)
                    str += ch;
            }
            return str;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Decoding error {ex}");
        }
        return string.Empty;
    }

    public static string ToUpperFist(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;
        return char.ToUpper(str[0]) + str.Substring(1);
    }
    public static string? ToSizeLabel(ulong? v)
    {
        if (null == v)
            return null;
        string unit = "B";
        double size = (double)v;
        double ret = 0;

        ret = size / 1024;
        if (1d < ret)
        {
            size = ret;
            unit = "KB";
            ret = size / 1024;
            if (1d < ret)
            {
                size = ret;
                unit = "MB";
                ret = size / 1024;
                if (1d < ret)
                {
                    size = ret;
                    unit = "GB";
                }
            }
        }
        return $"{Math.Round(size, 1)}{unit}";
    }
    public static string? ToSubscriptString<T>(this T num)
        where T : struct
    {
        string? str = num.ToString();
        if (str is null)
            return null;
        string ret = string.Empty;
        foreach (var ch in str)
        {
            if (byte.TryParse(ch.ToString(), out byte b) && 10 > b)
                ret += char.ConvertFromUtf32(0x2080 + b);
        }
        return ret;
    }
}
