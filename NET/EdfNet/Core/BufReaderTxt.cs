using System.Buffers.Text;

namespace EdfNet.Core;

public readonly ref struct BufReaderTxt : IBufReader
{
    public bool ReadRecBegin() => Match(Separator.RecBegin);
    public bool ReadRecEnd() => Match(Separator.RecEnd);
    public bool ReadBeginStruct() => Match(Separator.BeginStruct);
    public bool ReadEndStruct() => Match(Separator.EndStruct);
    public bool ReadBeginArray() => Match(Separator.BeginArray);
    public bool ReadEndArray() => Match(Separator.EndArray);
    public bool ReadVarEnd() => Match(Separator.VarEnd);

    private readonly BufStateTxt _state;
    private readonly EdfType? _rootType;

    public BufReaderTxt(BufStateTxt state, EdfType? rootType)
    {
        _state = state;
        _rootType = rootType;
    }

    public T Read<T>() where T : struct
    {
        EnsureFull();
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
                    return Unsafe.As<byte, T>(ref b);
                }
                break;
            case TypeCode.SByte:
                if (Utf8Parser.TryParse(buf, out sbyte sb, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    return Unsafe.As<sbyte, T>(ref sb);
                }
                break;
            case TypeCode.UInt16:
                if (Utf8Parser.TryParse(buf, out ushort us, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    return Unsafe.As<ushort, T>(ref us);
                }
                break;
            case TypeCode.Int16:
                if (Utf8Parser.TryParse(buf, out short s, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    return Unsafe.As<short, T>(ref s);
                }
                break;
            case TypeCode.UInt32:
                if (Utf8Parser.TryParse(buf, out uint ui, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    return Unsafe.As<uint, T>(ref ui);
                }
                break;
            case TypeCode.Int32:
                if (Utf8Parser.TryParse(buf, out int i, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    return Unsafe.As<int, T>(ref i);
                }
                break;
            case TypeCode.UInt64:
                if (Utf8Parser.TryParse(buf, out ulong ul, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    return Unsafe.As<ulong, T>(ref ul);
                }
                break;
            case TypeCode.Int64:
                if (Utf8Parser.TryParse(buf, out long l, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    return Unsafe.As<long, T>(ref l);
                }
                break;
            case TypeCode.Single:
                if (Utf8Parser.TryParse(buf, out float f, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    return Unsafe.As<float, T>(ref f);
                }
                break;
            case TypeCode.Double:
                if (Utf8Parser.TryParse(buf, out double d, out len))
                {
                    _state.Readed += len + Separator.VarEnd.Length;
                    Match(Separator.VarEnd);
                    return Unsafe.As<double, T>(ref d);
                }
                break;
        }
        throw new NotSupportedException($"Type {typeof(T).Name} not supported.");
    }

    public string? ReadString()
    {
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
        return Encoding.UTF8.GetString(content);
    }
    public byte[] ReadCharArray(int len)
    {
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
        var result = new byte[len];
        content.CopyTo(result);
        return result;
    }
    public EdfType? GetCurrentType()
    {
        return _rootType;
    }
    public int Read(Span<byte> dst)
    {
        var len = dst.Length;
        Ensure(len);
        _state.Buf.Slice(_state.Readed, len).CopyTo(dst);
        _state.Readed += len;
        return len;
    }

    private bool Match(ReadOnlySpan<byte> expected)
    {
        if (StartsWith(expected))
        {
            _state.Readed += expected.Length;
            return true;
        }
        throw new FormatException($"expected {expected.ToString()}");
        //return false;
    }

    private bool StartsWith(ReadOnlySpan<byte> expected)
    {
        Ensure(expected.Length);
        return _state.Buf.Slice(_state.Readed, expected.Length).SequenceEqual(expected);
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
