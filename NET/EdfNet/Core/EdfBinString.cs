using System.Buffers;
using System.Text.Unicode;

namespace EdfNet.Core;

public static class EdfBinString
{
    public const int MaxLen = 255;

    public static int SizeOf(ReadOnlySpan<byte> b) => int.Min(MaxLen, b[0]) + 1;
    public static int SizeOf(string? str)
    {
        if (string.IsNullOrEmpty(str))
            return 1;
        return 1 + int.Min(MaxLen, Encoding.UTF8.GetByteCount(str));
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
        var len = (byte)int.Min(MaxLen, Encoding.UTF8.GetByteCount(str));
        if (len > dst.Length)
            return dst.Length - len;
        CopyStringToSpan(str, dst.Slice(1, len));
        dst[0] = len;
        return 1 + len;
    }
    public static int ReadBin(ReadOnlySpan<byte> b, out string? str)
    {
        if (1 > b.Length)
        {
            str = null;
            return -1;
        }
        var len = b[0];
        if (MaxLen < len)
            throw new ArgumentException("BString overflow");
        if (len > b.Length)
        {
            str = null;
            return -1;
        }
        str = Encoding.UTF8.GetString(b.Slice(1, len));
        return 1 + len;
    }
    public static int CopyStringToSpan(string? str, Span<byte> dst)
    {
        if (string.IsNullOrEmpty(str) || dst.IsEmpty)
        {
            return 0;
        }
        // Безопасно конвертирует строку в UTF-8, пока в dst есть место
        OperationStatus status = Utf8.FromUtf16(
            str.AsSpan(),
            dst,
            out int charsRead,
            out int bytesWritten,
            replaceInvalidSequences: false
        );
        // bytesWritten вернет точное число успешно записанных байт
        return bytesWritten;
    }
}

