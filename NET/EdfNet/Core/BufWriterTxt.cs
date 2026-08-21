using System.Buffers.Text;

namespace EdfNet.Core;

public readonly ref struct BufWriterTxt : IBufWriter
{
    private readonly BufStateTxt _state;
    private readonly Span<byte> GetEmptyBuffer() => _state.Buf.Slice(_state.Writed);
    private void ThrowNotSupportedType(Type t) => throw new NotSupportedException($"Type {t.Name} not supported.");
    public EdfType CurrentType => _state.Enum.CurrentType;

    public BufWriterTxt(BufStateTxt state)
    {
        _state = state;
    }

    private void EnsureValueToken()
    {
        while (_state.Enum.CurrentToken != Token.Value)
        {
            var token = _state.Enum.CurrentToken;
            switch (token)
            {
                case Token.BeginRecord: WriteSepAndMoveNext(Separator.RecBegin); break;
                case Token.EndRecord: WriteSepAndMoveNext(Separator.RecEnd); EnsureEmpty(); return;
                case Token.BeginStruct: WriteSepAndMoveNext(Separator.StructBegin); break;
                case Token.EndStruct: WriteSepAndMoveNext(Separator.StructEnd); break;
                case Token.BeginArray: WriteSepAndMoveNext(Separator.ArrayBegin); break;
                case Token.EndArray: WriteSepAndMoveNext(Separator.ArrayEnd); break;
                default: throw new NotSupportedException($"Token {token} not supported.");
            }
            //_state.Enum.MoveNext();
        }
    }
    public void Write<T>(T val) where T : struct
    {
        EnsureValueToken();
        if (CurrentType.Type != typeof(T).GetPoType())
            throw new EdfWrongTypeException();
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
        _state.Writed += len;
        WriteSepAndMoveNext(Separator.VarEnd);
        EnsureValueToken();
    }
    public void Write(string? str)
    {
        EnsureValueToken();
        if (CurrentType.Type != PoType.String)
            throw new EdfWrongTypeException();
        int contentLen = string.IsNullOrEmpty(str) ? 0 : Encoding.UTF8.GetByteCount(str);
        contentLen = int.Min(contentLen, EdfBinString.MaxLen);
        int totalLen = contentLen + 2;// "content"
        EnsureCapacity(totalLen);
        var dst = GetEmptyBuffer();
        dst[0] = 34; // "
        if (contentLen > 0)
            EdfBinString.CopyStringToSpan(str, dst.Slice(1, contentLen));
        dst[1 + contentLen] = 34; // "
        _state.Writed += totalLen;
        WriteSepAndMoveNext(Separator.VarEnd);
        EnsureValueToken();
    }
    public void WriteCharArray(ReadOnlySpan<byte> charArray)
    {
        EnsureValueToken();
        if (CurrentType.Type != PoType.Char)
            throw new EdfWrongTypeException();
        int len = (int)CurrentType.GetTotalElements();
        ArgumentOutOfRangeException.ThrowIfNegative(len);
        int datLen = int.Min(len, charArray.Length);
        ReadOnlySpan<byte> zero = stackalloc byte[1];
        int firstZero = charArray.IndexOf(zero);
        if (firstZero >= 0)
            datLen = int.Min(datLen, firstZero);
        int totalLen = datLen + 2;// "content"
        EnsureCapacity(totalLen);
        var dst = GetEmptyBuffer();
        dst[0] = 34; // "
        if (datLen > 0)
            charArray.Slice(0, datLen).CopyTo(dst.Slice(1));
        dst[1 + datLen] = 34; // "
        _state.Writed += totalLen;
        WriteSepAndMoveNext(Separator.VarEnd);
        EnsureValueToken();
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
    private int WriteSepAndMoveNext(ReadOnlySpan<byte> val)
    {
        var len = val.Length;
        EnsureCapacity(len);
        val.CopyTo(GetEmptyBuffer());
        _state.Writed += len;
        _state.Enum.MoveNext();
        return len;
    }
}
