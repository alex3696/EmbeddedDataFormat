using System.Buffers.Text;

namespace EdfNet.Core;

public readonly ref struct BufWriterTxt : IBufWriter
{
    private readonly BufStateTxt _state;
    private readonly EdfType? _rootType;

    private readonly Span<byte> GetEmptyBuffer() => _state.Buf.Slice(_state.Writed);

    public BufWriterTxt(BufStateTxt state, EdfType? rootType)
    {
        _state = state;
        _rootType = rootType;
    }
    public void BeginArray() => Write(Separator.BeginArray);
    public void BeginStruct() => Write(Separator.BeginStruct);
    public void EndArray() => Write(Separator.EndArray);
    public void EndStruct() => Write(Separator.EndStruct);
    public void RecBegin() => Write(Separator.RecBegin);
    public void RecEnd() => Write(Separator.RecEnd);
    public void VarEnd() => Write(Separator.VarEnd);

    private void ThrowNotSupportedType(Type t) => throw new NotSupportedException($"Type {t.Name} not supported.");

    public int Write<T>(T val) where T : struct
    {
        EnsureEmpty();
        Span<byte> buf = GetEmptyBuffer();
        int len = 0;
        switch (Type.GetTypeCode(typeof(T)))
        {
            default: ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.Byte: if (!Utf8Formatter.TryFormat(Unsafe.As<T, byte>(ref val), buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.SByte: if (!Utf8Formatter.TryFormat(Unsafe.As<T, sbyte>(ref val), buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.UInt16: if (!Utf8Formatter.TryFormat(Unsafe.As<T, ushort>(ref val), buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.Int16: if (!Utf8Formatter.TryFormat(Unsafe.As<T, short>(ref val), buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.UInt32: if (!Utf8Formatter.TryFormat(Unsafe.As<T, uint>(ref val), buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.Int32: if (!Utf8Formatter.TryFormat(Unsafe.As<T, int>(ref val), buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.UInt64: if (!Utf8Formatter.TryFormat(Unsafe.As<T, ulong>(ref val), buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.Int64: if (!Utf8Formatter.TryFormat(Unsafe.As<T, long>(ref val), buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.Single: if (!Utf8Formatter.TryFormat(Unsafe.As<T, float>(ref val), buf, out len)) ThrowNotSupportedType(typeof(T)); break;
            case TypeCode.Double: if (!Utf8Formatter.TryFormat(Unsafe.As<T, double>(ref val), buf, out len)) ThrowNotSupportedType(typeof(T)); break;
        }
        int totalLen = len + Separator.VarEnd.Length;
        Separator.VarEnd.CopyTo(buf.Slice(len));
        _state.Writed += totalLen;
        return totalLen;
    }
    public int Write(string? str)
    {
        int contentLen = string.IsNullOrEmpty(str) ? 0 : Encoding.UTF8.GetByteCount(str);
        contentLen = int.Min(contentLen, EdfBinString.MaxLen);
        int totalLen = contentLen + 2 + Separator.VarEnd.Length; // "content";
        EnsureCapacity(totalLen);
        var dst = GetEmptyBuffer();
        dst[0] = 34; // "
        if (contentLen > 0)
            EdfBinString.CopyStringToSpan(str, dst.Slice(1, contentLen));
        dst[1 + contentLen] = 34; // "
        Separator.VarEnd.CopyTo(dst.Slice(2 + contentLen));
        _state.Writed += totalLen;
        return totalLen;
    }
    public EdfType? GetCurrentType()
    {
        return _rootType;
    }
    public int Write(ReadOnlySpan<byte> val)
    {
        var len = val.Length;
        EnsureCapacity(len);
        val.CopyTo(GetEmptyBuffer());
        _state.Writed += len;
        return len;
    }
    public int WriteCharArray(ReadOnlySpan<byte> charArray, int len)
    {
        if (len < 0) throw new ArgumentOutOfRangeException(nameof(len));
        int datLen = int.Min(len, charArray.Length);
        ReadOnlySpan<byte> zero = stackalloc byte[1];
        int firstZero = charArray.IndexOf(zero);
        if (firstZero >= 0)
            datLen = int.Min(datLen, firstZero);
        int totalLen = datLen + 2 + Separator.VarEnd.Length; // "content";
        EnsureCapacity(totalLen);
        var dst = GetEmptyBuffer();
        dst[0] = 34; // "
        if (datLen > 0)
            charArray.Slice(0, datLen).CopyTo(dst.Slice(1));
        dst[1 + datLen] = 34; // "
        Separator.VarEnd.CopyTo(dst.Slice(2 + datLen));
        _state.Writed += totalLen;
        return totalLen;
    }

    public void EnsureEmpty()
    {
        _state.Stream.Write(_state.Buf.Slice(0, _state.Writed));
        _state.Writed = 0;
    }
    private void EnsureCapacity(int len)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(len, _state.Buf.Length);
        var emptyLen = GetEmptyBuffer().Length;
        if (len > emptyLen)
        {
            _state.Stream.Write(_state.Buf.Slice(0, _state.Writed));
            _state.Writed = 0;
        }
    }
}
