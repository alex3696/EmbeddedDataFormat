using System.Buffers.Text;
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
            _stream.Write(EdfTokenLiterals.Quote); // Write the opening quote
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
            _stream.Write(EdfTokenLiterals.Quote); // Write the opening quote
    }
    public void WriteNumber<T>(T val) where T : struct
    {
        int len = 0;
        switch (Type.GetTypeCode(typeof(T)))
        {
            default: ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.Byte: if (!Utf8Formatter.TryFormat(Unsafe.As<T, byte>(ref val), _buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.SByte: if (!Utf8Formatter.TryFormat(Unsafe.As<T, sbyte>(ref val), _buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.UInt16: if (!Utf8Formatter.TryFormat(Unsafe.As<T, ushort>(ref val), _buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.Int16: if (!Utf8Formatter.TryFormat(Unsafe.As<T, short>(ref val), _buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.UInt32: if (!Utf8Formatter.TryFormat(Unsafe.As<T, uint>(ref val), _buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.Int32: if (!Utf8Formatter.TryFormat(Unsafe.As<T, int>(ref val), _buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.UInt64: if (!Utf8Formatter.TryFormat(Unsafe.As<T, ulong>(ref val), _buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.Int64: if (!Utf8Formatter.TryFormat(Unsafe.As<T, long>(ref val), _buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.Single: if (!Utf8Formatter.TryFormat(Unsafe.As<T, float>(ref val), _buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.Double: if (!Utf8Formatter.TryFormat(Unsafe.As<T, double>(ref val), _buf, out len)) ThrowNotSupportedType(typeof(T)); break;
        }
        _stream.Write(_buf, 0, len);
    }
    public void Flush() => _stream.Flush();
    private void ThrowNotSupportedType(Type t) => throw new NotSupportedException($"Type {t.Name} not supported.");
}
