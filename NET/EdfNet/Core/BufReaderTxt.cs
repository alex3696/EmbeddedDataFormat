using System.Buffers.Text;

namespace EdfNet.Core;

public readonly ref struct BufReaderTxt : IBufReader
{
    private readonly IBufferedReader _bufferedReader;
    public readonly CircularEdfTypeEnumeratorTxt Enum;

    public EdfType CurrentType => Enum.CurrentType;

    public BufReaderTxt(IBufferedReader bufReader, CircularEdfTypeEnumeratorTxt circularEdfType)
    {
        _bufferedReader = bufReader;
        Enum = circularEdfType;
    }

    public T Read<T>() where T : struct
    {
        EnsureValueToken();
        if (CurrentType.Type != typeof(T).GetPoType())
            throw new EdfWrongTypeException();
        SkipWhitespace();
        var buf = _bufferedReader.GetSpan(EdfTokenizer.NumberTokenMaxLength);
        int len = 0;
        var code = Type.GetTypeCode(typeof(T));
        switch (code)
        {
            default: break;
            case TypeCode.Byte:
                if (Utf8Parser.TryParse(buf, out byte b, out len))
                {
                    _bufferedReader.Advance(len);
                    MatchAndMoveNext(EdfTokenLiterals.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<byte, T>(ref b);
                }
                break;
            case TypeCode.SByte:
                if (Utf8Parser.TryParse(buf, out sbyte sb, out len))
                {
                    _bufferedReader.Advance(len);
                    MatchAndMoveNext(EdfTokenLiterals.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<sbyte, T>(ref sb);
                }
                break;
            case TypeCode.UInt16:
                if (Utf8Parser.TryParse(buf, out ushort us, out len))
                {
                    _bufferedReader.Advance(len);
                    MatchAndMoveNext(EdfTokenLiterals.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<ushort, T>(ref us);
                }
                break;
            case TypeCode.Int16:
                if (Utf8Parser.TryParse(buf, out short s, out len))
                {
                    _bufferedReader.Advance(len);
                    MatchAndMoveNext(EdfTokenLiterals.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<short, T>(ref s);
                }
                break;
            case TypeCode.UInt32:
                if (Utf8Parser.TryParse(buf, out uint ui, out len))
                {
                    _bufferedReader.Advance(len);
                    MatchAndMoveNext(EdfTokenLiterals.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<uint, T>(ref ui);
                }
                break;
            case TypeCode.Int32:
                if (Utf8Parser.TryParse(buf, out int i, out len))
                {
                    _bufferedReader.Advance(len);
                    MatchAndMoveNext(EdfTokenLiterals.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<int, T>(ref i);
                }
                break;
            case TypeCode.UInt64:
                if (Utf8Parser.TryParse(buf, out ulong ul, out len))
                {
                    _bufferedReader.Advance(len);
                    MatchAndMoveNext(EdfTokenLiterals.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<ulong, T>(ref ul);
                }
                break;
            case TypeCode.Int64:
                if (Utf8Parser.TryParse(buf, out long l, out len))
                {
                    _bufferedReader.Advance(len);
                    MatchAndMoveNext(EdfTokenLiterals.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<long, T>(ref l);
                }
                break;
            case TypeCode.Single:
                if (Utf8Parser.TryParse(buf, out float f, out len))
                {
                    _bufferedReader.Advance(len);
                    MatchAndMoveNext(EdfTokenLiterals.VarEnd);
                    EnsureValueToken();
                    return Unsafe.As<float, T>(ref f);
                }
                break;
            case TypeCode.Double:
                if (Utf8Parser.TryParse(buf, out double d, out len))
                {
                    _bufferedReader.Advance(len);
                    MatchAndMoveNext(EdfTokenLiterals.VarEnd);
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
        Match("\""u8);
        var buf = _bufferedReader.GetSpan(EdfTokenizer.StringTokenMaxLength);
        int quoteIdx = buf.IndexOf((byte)'"');
        if (quoteIdx < 0)
            throw new EdfParseException("Unterminated string literal", 0, 0);
        var content = buf.Slice(0, quoteIdx);
        var str = Encoding.UTF8.GetString(content);
        _bufferedReader.Advance(quoteIdx);
        Match("\""u8);
        MatchAndMoveNext(EdfTokenLiterals.VarEnd);
        EnsureValueToken();
        return str;
    }
    public byte[] ReadCharArray()
    {
        EnsureValueToken();
        if (CurrentType.Type != PoType.Char)
            throw new EdfWrongTypeException();
        Match("\""u8);
        var buf = _bufferedReader.GetSpan(EdfTokenizer.StringTokenMaxLength);
        int quoteIdx = buf.IndexOf((byte)'"');
        if (quoteIdx < 0)
            throw new EdfParseException("Unterminated string literal", 0, 0);
        var content = buf.Slice(0, quoteIdx);
        var str = Encoding.UTF8.GetString(content);
        _bufferedReader.Advance(quoteIdx);
        Match("\""u8);
        Match(EdfTokenLiterals.VarEnd);
        EnsureValueToken();
        int len = (int)CurrentType.GetTotalElements();
        var result = new byte[len];
        content.CopyTo(result);
        return result;
    }
    private void SkipWhitespace()
    {
        while (true)
        {
            var buf = _bufferedReader.GetSpan();
            if (!EdfTokenizer.IsAsciiWhitespace(buf[0]))
                break;
            _bufferedReader.Advance(1);
        }
    }
    private bool Match(ReadOnlySpan<byte> expected)
    {
        SkipWhitespace();
        var len = expected.Length;
        var buf = _bufferedReader.GetSpan(len);
        if (buf.Length < len)
            throw new EndOfStreamException();
        if (!buf.Slice(0, len).SequenceEqual(expected))
            throw new FormatException($"expected {Encoding.UTF8.GetString(expected)}");
        _bufferedReader.Advance(len);
        return true;
    }
    private void MatchAndMoveNext(ReadOnlySpan<byte> sep)
    {
        Match(sep);
        Enum.MoveNext();
    }
    private void EnsureValueToken()
    {
        while (Enum.CurrentToken != Token.Value)
        {
            var token = Enum.CurrentToken;
            switch (token)
            {
                case Token.BeginRecord: Enum.MoveNext(); break;
                case Token.EndRecord: Enum.MoveNext(); return;
                case Token.BeginStruct: MatchAndMoveNext(EdfTokenLiterals.StructBegin); break;
                case Token.EndStruct: MatchAndMoveNext(EdfTokenLiterals.StructEnd); break;
                case Token.BeginArray: MatchAndMoveNext(EdfTokenLiterals.ArrayBegin); break;
                case Token.EndArray: MatchAndMoveNext(EdfTokenLiterals.ArrayEnd); break;
                default: throw new NotSupportedException($"Token {token} not supported.");
            }
            //_state.Enum.MoveNext();
        }
    }

}
