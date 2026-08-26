using System.Buffers.Text;

namespace EdfNet.Core.Text;

public readonly ref struct BufReaderTxt : IBufReader
{
    private readonly EdfTokenReader _tokenizer;
    public readonly CircularEdfTypeEnumeratorTxt Enum;

    public EdfType CurrentType => Enum.CurrentType;

    public BufReaderTxt(EdfTokenReader tokenizer, CircularEdfTypeEnumeratorTxt circularEdfType)
    {
        _tokenizer = tokenizer;
        Enum = circularEdfType;
    }


    private void SkipNonValueItem()
    {
        var token = Enum.CurrentToken;
        switch (token)
        {
            case TypeTokenType.BeginRecord: _tokenizer.ExpectAdvance(TextTokenType.RecBegin); break;
            case TypeTokenType.EndRecord: _tokenizer.ExpectAdvance(TextTokenType.BlockEnd); break;
            case TypeTokenType.BeginStruct: _tokenizer.ExpectAdvance(TextTokenType.StructBegin); break;
            case TypeTokenType.EndStruct: _tokenizer.ExpectAdvance(TextTokenType.StructEnd); break;
            case TypeTokenType.BeginArray: _tokenizer.ExpectAdvance(TextTokenType.ArrayBegin); break;
            case TypeTokenType.EndArray: _tokenizer.ExpectAdvance(TextTokenType.ArrayEnd); break;
            default: throw new NotSupportedException($"Token {token} not supported.");
        }
        Enum.MoveNext();
    }
    private void EnsureSchemaAndToken(PoType pot, TextTokenType tokenType)
    {
        // skip non value Enum.CurrentToken
        while (Enum.CurrentToken != TypeTokenType.Value)
            SkipNonValueItem();

        if (CurrentType.Type != pot)
            throw new EdfWrongTypeException($"expected from schema {CurrentType.Type} got {pot}");
        if (!_tokenizer.MoveNext() || tokenType != _tokenizer.TokenType)
        {
            throw new EdfParseException(
                $"Expected {EdfTokenReader.Describe(tokenType)} but got {EdfTokenReader.Describe(_tokenizer.TokenType)}",
                _tokenizer.TokenLine, _tokenizer.TokenColumn);
        }
    }
    void EnsureNextValueOrBlockEnd()
    {
        _tokenizer.Advance(); // skip current value
        _tokenizer.ExpectAdvance(TextTokenType.VarEnd); // skip ';'
        Enum.MoveNext();
        while (Enum.CurrentToken != TypeTokenType.Value && Enum.CurrentToken != TypeTokenType.EndRecord)
            SkipNonValueItem();
    }

    public T Read<T>() where T : struct
    {
        EnsureSchemaAndToken(typeof(T).GetPoType(), TextTokenType.Number);
        var buf = _tokenizer.TokenValue;
        var code = Type.GetTypeCode(typeof(T));
        switch (code)
        {
            default: break;
            case TypeCode.Byte:
                if (Utf8Parser.TryParse(buf, out byte b, out _))
                {
                    EnsureNextValueOrBlockEnd();
                    return Unsafe.As<byte, T>(ref b);
                }
                break;
            case TypeCode.SByte:
                if (Utf8Parser.TryParse(buf, out sbyte sb, out _))
                {
                    EnsureNextValueOrBlockEnd();
                    return Unsafe.As<sbyte, T>(ref sb);
                }
                break;
            case TypeCode.UInt16:
                if (Utf8Parser.TryParse(buf, out ushort us, out _))
                {
                    EnsureNextValueOrBlockEnd();
                    return Unsafe.As<ushort, T>(ref us);
                }
                break;
            case TypeCode.Int16:
                if (Utf8Parser.TryParse(buf, out short s, out _))
                {
                    EnsureNextValueOrBlockEnd();
                    return Unsafe.As<short, T>(ref s);
                }
                break;
            case TypeCode.UInt32:
                if (Utf8Parser.TryParse(buf, out uint ui, out _))
                {
                    EnsureNextValueOrBlockEnd();
                    return Unsafe.As<uint, T>(ref ui);
                }
                break;
            case TypeCode.Int32:
                if (Utf8Parser.TryParse(buf, out int i, out _))
                {
                    EnsureNextValueOrBlockEnd();
                    return Unsafe.As<int, T>(ref i);
                }
                break;
            case TypeCode.UInt64:
                if (Utf8Parser.TryParse(buf, out ulong ul, out _))
                {
                    EnsureNextValueOrBlockEnd();
                    return Unsafe.As<ulong, T>(ref ul);
                }
                break;
            case TypeCode.Int64:
                if (Utf8Parser.TryParse(buf, out long l, out _))
                {
                    EnsureNextValueOrBlockEnd();
                    return Unsafe.As<long, T>(ref l);
                }
                break;
            case TypeCode.Single:
                if (Utf8Parser.TryParse(buf, out float f, out _))
                {
                    EnsureNextValueOrBlockEnd();
                    return Unsafe.As<float, T>(ref f);
                }
                break;
            case TypeCode.Double:
                if (Utf8Parser.TryParse(buf, out double d, out _))
                {
                    EnsureNextValueOrBlockEnd();
                    return Unsafe.As<double, T>(ref d);
                }
                break;
        }
        throw new NotSupportedException($"Type {typeof(T).Name} not supported.");
    }
    public string? ReadString()
    {
        EnsureSchemaAndToken(PoType.String, TextTokenType.StringLiteral);
        var str = _tokenizer.GetString();
        EnsureNextValueOrBlockEnd();
        return str;
    }
    public byte[] ReadCharArray()
    {
        EnsureSchemaAndToken(PoType.String, TextTokenType.StringLiteral);
        var content = _tokenizer.TokenValue;
        int len = (int)CurrentType.GetTotalElements();
        var result = new byte[len];
        content.Slice(0, int.Min(len, content.Length)).CopyTo(result);
        EnsureNextValueOrBlockEnd();
        return result;
    }


}
