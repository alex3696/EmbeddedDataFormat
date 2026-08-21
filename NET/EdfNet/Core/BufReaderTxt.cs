using System.Buffers.Text;

namespace EdfNet.Core;

public readonly ref struct BufReaderTxt : IBufReader
{
    private readonly BufStateTxt _state;
    public EdfType CurrentType => _state.Enum.CurrentType;

    public BufReaderTxt(BufStateTxt state)
    {
        _state = state;
    }

    public T Read<T>() where T : struct
    {
        EnsureFull();
        EnsureValueToken();
        if (CurrentType.Type != typeof(T).GetPoType())
            throw new EdfWrongTypeException();
        Span<byte> buf = _state.Buf.Slice(_state.Readed);
        int len = 0;
        var code = Type.GetTypeCode(typeof(T));
        switch (code)
        {
            default: break;
            case TypeCode.Byte:
                if (Utf8Parser.TryParse(buf, out byte b, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<byte, T>(ref b);
                }
                break;
            case TypeCode.SByte:
                if (Utf8Parser.TryParse(buf, out sbyte sb, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<sbyte, T>(ref sb);
                }
                break;
            case TypeCode.UInt16:
                if (Utf8Parser.TryParse(buf, out ushort us, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<ushort, T>(ref us);
                }
                break;
            case TypeCode.Int16:
                if (Utf8Parser.TryParse(buf, out short s, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<short, T>(ref s);
                }
                break;
            case TypeCode.UInt32:
                if (Utf8Parser.TryParse(buf, out uint ui, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<uint, T>(ref ui);
                }
                break;
            case TypeCode.Int32:
                if (Utf8Parser.TryParse(buf, out int i, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<int, T>(ref i);
                }
                break;
            case TypeCode.UInt64:
                if (Utf8Parser.TryParse(buf, out ulong ul, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<ulong, T>(ref ul);
                }
                break;
            case TypeCode.Int64:
                if (Utf8Parser.TryParse(buf, out long l, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<long, T>(ref l);
                }
                break;
            case TypeCode.Single:
                if (Utf8Parser.TryParse(buf, out float f, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<float, T>(ref f);
                }
                break;
            case TypeCode.Double:
                if (Utf8Parser.TryParse(buf, out double d, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<double, T>(ref d);
                }
                break;
        }
        throw new NotSupportedException($"Type {typeof(T).Name} not supported.");
    }
    public string? ReadString()
    {
        EnsureValueToken();
        if (CurrentType.Type != PoType.String)
            throw new EdfWrongTypeException();
        Ensure(1);
        if (_state.Buf[_state.Readed] != 34) throw new FormatException("Expected opening quote");
        _state.Readed++;
        int start = _state.Readed;
        while (true)
        {
            Ensure(1);
            if (_state.Buf[_state.Readed] == 34) break;
            _state.Readed++;
        }
        var content = _state.Buf.Slice(start, _state.Readed - start);
        _state.Readed++; // skip closing quote
        Match(Separator.VarEnd);
        EnsureValueToken();
        return Encoding.UTF8.GetString(content);
    }
    public byte[] ReadCharArray()
    {
        EnsureValueToken();
        if (CurrentType.Type != PoType.Char)
            throw new EdfWrongTypeException();
        Ensure(1);
        if (_state.Buf[_state.Readed] != 34) throw new FormatException("Expected opening quote");
        _state.Readed++;
        int len = (int)CurrentType.GetTotalElements();
        int start = _state.Readed;
        while (true)
        {
            Ensure(1);
            if (_state.Buf[_state.Readed] == 34) break;
            _state.Readed++;
        }
        var content = _state.Buf.Slice(start, _state.Readed - start);
        _state.Readed++; // skip closing quote
        Match(Separator.VarEnd);
        var result = new byte[len];
        content.CopyTo(result);
        EnsureValueToken();
        return result;
    }

    private bool Match(ReadOnlySpan<byte> expected)
    {
        Ensure(expected.Length);
        if (_state.Buf.Slice(_state.Readed, expected.Length).SequenceEqual(expected))
        {
            _state.Readed += expected.Length;
            return true;
        }
        throw new FormatException($"expected {expected.ToString()}");
        //return false;
    }
    private int ReadSepAndMoveNext(ReadOnlySpan<byte> val)
    {
        var len = val.Length;
        Ensure(len);
        Match(val);
        _state.Enum.MoveNext();
        return len;
    }
    private void EnsureValueToken()
    {
        while (_state.Enum.CurrentToken != Token.Value)
        {
            var token = _state.Enum.CurrentToken;
            switch (token)
            {
                case Token.BeginRecord: ReadSepAndMoveNext(Separator.RecBegin); break;
                case Token.EndRecord: ReadSepAndMoveNext(Separator.RecEnd); return;
                case Token.BeginStruct: ReadSepAndMoveNext(Separator.StructBegin); break;
                case Token.EndStruct: ReadSepAndMoveNext(Separator.StructEnd); break;
                case Token.BeginArray: ReadSepAndMoveNext(Separator.ArrayBegin); break;
                case Token.EndArray: ReadSepAndMoveNext(Separator.ArrayEnd); break;
                default: throw new NotSupportedException($"Token {token} not supported.");
            }
            //_state.Enum.MoveNext();
        }
    }
    private void EnsureFull()
    {
        var available = _state.Writed - _state.Readed;
        if (available > 0)
            _state.Buf.Slice(_state.Readed, available).CopyTo(_state.Buf);
        _state.Writed = available;
        _state.Readed = 0;
        var read = _state.Stream.Read(_state.Buf.Slice(_state.Writed));
        _state.Writed += read;
    }
    private void Ensure(int len)
    {
        var available = _state.Writed - _state.Readed;
        if (len > available)
        {
            if (available > 0)
                _state.Buf.Slice(_state.Readed, available).CopyTo(_state.Buf);
            _state.Writed = available;
            _state.Readed = 0;
            var read = _state.Stream.Read(_state.Buf.Slice(_state.Writed));
            _state.Writed += read;
            if (_state.Writed < len)
                throw new EndOfStreamException();
        }
    }
}
