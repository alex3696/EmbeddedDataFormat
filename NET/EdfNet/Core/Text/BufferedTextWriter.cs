using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.Unicode;

namespace EdfNet.Core.Text;

public sealed class BufferedTextWriter
{
    private readonly byte[] _buf;
    private readonly Stream _stream;

    public BufferedTextWriter(Stream stream, byte[] buf)
    {
        _stream = stream;
        _buf = buf;
    }
    public void Write(byte b) => _stream.WriteByte(b);
    public void Write(ReadOnlySpan<byte> b) => _stream.Write(b);
    public void Write(string? str)
    {
        if (string.IsNullOrEmpty(str))
            return;
        int len = Encoding.UTF8.GetBytes(str, _buf);
        _stream.Write(_buf, 0, len);
    }
    public void WriteString(string? str, bool quoted, int maxByteCount)
    {
        if (quoted)
            Write(EdfTokenLiterals.Quote); // Write the opening quote
        if (!string.IsNullOrEmpty(str))
        {
            maxByteCount = 0 < maxByteCount ? maxByteCount : _buf.Length;
            OperationStatus status = Utf8.FromUtf16(
               str.AsSpan(),
               _buf.AsSpan(0, maxByteCount),
               out int charsRead,
               out int bytesWritten,
               replaceInvalidSequences: false
           );
            _stream.Write(_buf, 0, bytesWritten);
        }
        if (quoted)
            Write(EdfTokenLiterals.Quote); // Write the opening quote
    }
    public void WriteNumber(byte val)
    {
        if (!val.TryFormat(_buf, out int len, default, CultureInfo.InvariantCulture))
            ThrowFormatError(val);
        _stream.Write(_buf, 0, len);
    }
    public void WriteNumber(sbyte val)
    {
        if (!val.TryFormat(_buf, out int len, default, CultureInfo.InvariantCulture))
            ThrowFormatError(val);
        _stream.Write(_buf, 0, len);
    }
    public void WriteNumber(ushort val)
    {
        if (!val.TryFormat(_buf, out int len, default, CultureInfo.InvariantCulture))
            ThrowFormatError(val);
        _stream.Write(_buf, 0, len);
    }
    public void WriteNumber(short val)
    {
        if (!val.TryFormat(_buf, out int len, default, CultureInfo.InvariantCulture))
            ThrowFormatError(val);
        _stream.Write(_buf, 0, len);
    }
    public void WriteNumber(uint val)
    {
        if (!val.TryFormat(_buf, out int len, default, CultureInfo.InvariantCulture))
            ThrowFormatError(val);
        _stream.Write(_buf, 0, len);
    }
    public void WriteNumber(int val)
    {
        if (!val.TryFormat(_buf, out int len, default, CultureInfo.InvariantCulture))
            ThrowFormatError(val);
        _stream.Write(_buf, 0, len);
    }
    public void WriteNumber(ulong val)
    {
        if (!val.TryFormat(_buf, out int len, default, CultureInfo.InvariantCulture))
            ThrowFormatError(val);
        _stream.Write(_buf, 0, len);
    }
    public void WriteNumber(long val)
    {
        if (!val.TryFormat(_buf, out int len, default, CultureInfo.InvariantCulture))
            ThrowFormatError(val);
        _stream.Write(_buf, 0, len);
    }
    public void WriteNumber(float val)
    {
        if (!val.TryFormat(_buf, out int len, default, CultureInfo.InvariantCulture))
            ThrowFormatError(val);
        _stream.Write(_buf, 0, len);
    }
    public void WriteNumber(double val)
    {
        if (!val.TryFormat(_buf, out int len, default, CultureInfo.InvariantCulture))
            ThrowFormatError(val);
        _stream.Write(_buf, 0, len);
    }

    public void WriteNumber<T>(T val) where T : struct, IBinaryNumber<T>
    {
        int len = 0;
        switch (Type.GetTypeCode(typeof(T)))
        {
            default: throw new NotSupportedException($"Type {typeof(T).Name} not supported.");
            case TypeCode.Byte: WriteNumber(Unsafe.As<T, byte>(ref val)); break;
            case TypeCode.SByte: WriteNumber(Unsafe.As<T, sbyte>(ref val)); break;
            case TypeCode.UInt16: WriteNumber(Unsafe.As<T, ushort>(ref val)); break;
            case TypeCode.Int16: WriteNumber(Unsafe.As<T, short>(ref val)); break;
            case TypeCode.UInt32: WriteNumber(Unsafe.As<T, uint>(ref val)); break;
            case TypeCode.Int32: WriteNumber(Unsafe.As<T, int>(ref val)); break;
            case TypeCode.UInt64: WriteNumber(Unsafe.As<T, ulong>(ref val)); break;
            case TypeCode.Int64: WriteNumber(Unsafe.As<T, long>(ref val)); break;
            case TypeCode.Single: WriteNumber(Unsafe.As<T, float>(ref val)); break;
            case TypeCode.Double: WriteNumber(Unsafe.As<T, double>(ref val)); break;
        }
        _stream.Write(_buf, 0, len);
    }
    public void Flush() => _stream.Flush();
    [DoesNotReturn] private static void ThrowFormatError<T>(T val) => throw new Exception($"{typeof(T)} {val} format error.");
}
