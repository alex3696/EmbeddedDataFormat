using System.Globalization;

namespace EdfNet.Core.Text;

public readonly ref struct BufReaderTxt : IBufReader
{
    private readonly EdfTokenReader _tokenizer;
    public readonly TextCircularEdfTypeEnumerator Enum;

    public EdfType CurrentType => Enum.CurrentType;

    public BufReaderTxt(EdfTokenReader tokenizer, TextCircularEdfTypeEnumerator circularEdfType)
    {
        _tokenizer = tokenizer;
        Enum = circularEdfType;
    }


    private void SkipNonValueItem()
    {
        switch (Enum.CurrentToken)
        {
            case TypeTokenType.BeginRecord: _tokenizer.ExpectAdvance(TextTokenType.RecBegin); break;
            case TypeTokenType.EndRecord: _tokenizer.ExpectAdvance(TextTokenType.BlockEnd); break;
            case TypeTokenType.BeginStruct: _tokenizer.ExpectAdvance(TextTokenType.StructBegin); break;
            case TypeTokenType.EndStruct: _tokenizer.ExpectAdvance(TextTokenType.StructEnd); break;
            case TypeTokenType.BeginArray: _tokenizer.ExpectAdvance(TextTokenType.ArrayBegin); break;
            case TypeTokenType.EndArray: _tokenizer.ExpectAdvance(TextTokenType.ArrayEnd); break;
            default: throw new NotSupportedException($"Token {Enum.CurrentToken} not supported.");
        }
        Enum.MoveNext();
    }
    private void EnsureSchemaAndToken(EdfPrimitiveType pot, TextTokenType tokenType)
    {
        // skip non value Enum.CurrentToken
        while (Enum.CurrentToken != TypeTokenType.Value)
            SkipNonValueItem();

        if (CurrentType.Type != pot)
            throw new EdfWrongTypeException($"expected from schema {CurrentType.Type} got {pot}");
        if (!_tokenizer.HasValidToken)
        {
            if (_tokenizer.MoveNext())
                return;
        }
        if (tokenType != _tokenizer.TokenType)
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
        {
            SkipNonValueItem();
        }
        if (Enum.CurrentToken == TypeTokenType.EndRecord) // set to first value of next record
            SkipNonValueItem();
    }

    public byte ReadUInt8()
    {
        EnsureSchemaAndToken(EdfPrimitiveType.UInt8, TextTokenType.Number);
        if (!byte.TryParse(_tokenizer.TokenValue, CultureInfo.InvariantCulture, out var b))
            throw new NotSupportedException($"Can`t parse byte");
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public sbyte ReadInt8()
    {
        EnsureSchemaAndToken(EdfPrimitiveType.Int8, TextTokenType.Number);
        if (!sbyte.TryParse(_tokenizer.TokenValue, CultureInfo.InvariantCulture, out var b))
            throw new NotSupportedException($"Can`t parse byte");
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public ushort ReadUInt16()
    {
        EnsureSchemaAndToken(EdfPrimitiveType.UInt16, TextTokenType.Number);
        if (!ushort.TryParse(_tokenizer.TokenValue, CultureInfo.InvariantCulture, out var b))
            throw new NotSupportedException($"Can`t parse byte");
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public short ReadInt16()
    {
        EnsureSchemaAndToken(EdfPrimitiveType.Int16, TextTokenType.Number);
        if (!short.TryParse(_tokenizer.TokenValue, CultureInfo.InvariantCulture, out var b))
            throw new NotSupportedException($"Can`t parse byte");
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public uint ReadUInt32()
    {
        EnsureSchemaAndToken(EdfPrimitiveType.UInt32, TextTokenType.Number);
        if (!uint.TryParse(_tokenizer.TokenValue, CultureInfo.InvariantCulture, out var b))
            throw new NotSupportedException($"Can`t parse byte");
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public int ReadInt32()
    {
        EnsureSchemaAndToken(EdfPrimitiveType.Int32, TextTokenType.Number);
        if (!int.TryParse(_tokenizer.TokenValue, CultureInfo.InvariantCulture, out var b))
            throw new NotSupportedException($"Can`t parse byte");
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public ulong ReadUInt64()
    {
        EnsureSchemaAndToken(EdfPrimitiveType.UInt64, TextTokenType.Number);
        if (!ulong.TryParse(_tokenizer.TokenValue, CultureInfo.InvariantCulture, out var b))
            throw new NotSupportedException($"Can`t parse byte");
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public long ReadInt64()
    {
        EnsureSchemaAndToken(EdfPrimitiveType.Int64, TextTokenType.Number);
        if (!long.TryParse(_tokenizer.TokenValue, CultureInfo.InvariantCulture, out var b))
            throw new NotSupportedException($"Can`t parse byte");
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public float ReadSingle()
    {
        EnsureSchemaAndToken(EdfPrimitiveType.Single, TextTokenType.Number);
        if (!float.TryParse(_tokenizer.TokenValue, CultureInfo.InvariantCulture, out var b))
            throw new NotSupportedException($"Can`t parse byte");
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public double ReadDouble()
    {
        EnsureSchemaAndToken(EdfPrimitiveType.Double, TextTokenType.Number);
        if (!double.TryParse(_tokenizer.TokenValue, CultureInfo.InvariantCulture, out var b))
            throw new NotSupportedException($"Can`t parse byte");
        EnsureNextValueOrBlockEnd();
        return b;
    }

    public T Read<T>() where T : struct
    {
        switch (Type.GetTypeCode(typeof(T)))
        {
            default: break;
            case TypeCode.Byte: { var b = ReadUInt8(); return Unsafe.As<byte, T>(ref b); }
            case TypeCode.SByte: { var b = ReadInt8(); return Unsafe.As<sbyte, T>(ref b); }
            case TypeCode.UInt16: { var b = ReadUInt16(); return Unsafe.As<ushort, T>(ref b); }
            case TypeCode.Int16: { var b = ReadInt16(); return Unsafe.As<short, T>(ref b); }
            case TypeCode.UInt32: { var b = ReadUInt32(); return Unsafe.As<uint, T>(ref b); }
            case TypeCode.Int32: { var b = ReadInt32(); return Unsafe.As<int, T>(ref b); }
            case TypeCode.UInt64: { var b = ReadUInt64(); return Unsafe.As<ulong, T>(ref b); }
            case TypeCode.Int64: { var b = ReadInt64(); return Unsafe.As<long, T>(ref b); }
            case TypeCode.Single: { var b = ReadSingle(); return Unsafe.As<float, T>(ref b); }
            case TypeCode.Double: { var b = ReadDouble(); return Unsafe.As<double, T>(ref b); }
        }
        throw new NotSupportedException($"Type {typeof(T).Name} not supported.");
    }
    public void ReadToSpan(Span<byte> dst, out EdfPrimitiveType pt, out int len)
    {
        // skip non value Enum.CurrentToken
        while (Enum.CurrentToken != TypeTokenType.Value)
            SkipNonValueItem();
        pt = Enum.CurrentType.Type;
        switch (pt)
        {
            default: throw new EdfWrongTypeException();
            case EdfPrimitiveType.UInt8: MemoryMarshal.Write(dst, ReadUInt8()); len = 1; return;
            case EdfPrimitiveType.Int8: MemoryMarshal.Write(dst, ReadInt8()); len = 1; return;
            case EdfPrimitiveType.UInt16: MemoryMarshal.Write(dst, ReadUInt16()); len = 2; return;
            case EdfPrimitiveType.Int16: MemoryMarshal.Write(dst, ReadInt16()); len = 2; return;
            case EdfPrimitiveType.UInt32: MemoryMarshal.Write(dst, ReadUInt32()); len = 4; return;
            case EdfPrimitiveType.Int32: MemoryMarshal.Write(dst, ReadInt32()); len = 4; return;
            case EdfPrimitiveType.UInt64: MemoryMarshal.Write(dst, ReadUInt64()); len = 8; return;
            case EdfPrimitiveType.Int64: MemoryMarshal.Write(dst, ReadInt64()); len = 8; return;
            case EdfPrimitiveType.Single: MemoryMarshal.Write(dst, ReadSingle()); len = 4; return;
            case EdfPrimitiveType.Double: MemoryMarshal.Write(dst, ReadDouble()); len = 8; return;
            case EdfPrimitiveType.Char:
                EnsureSchemaAndToken(EdfPrimitiveType.Char, TextTokenType.StringLiteral);
                var content = _tokenizer.TokenValue;
                len = (int)CurrentType.GetTotalElements();
                content.Slice(0, int.Min(len, content.Length)).CopyTo(dst);
                if (content.Length < len)
                    dst.Slice(content.Length, len - content.Length).Clear();
                EnsureNextValueOrBlockEnd();
                return;
            case EdfPrimitiveType.String:
                EnsureSchemaAndToken(EdfPrimitiveType.String, TextTokenType.StringLiteral);
                len = _tokenizer.TokenValue.Length;
                _tokenizer.TokenValue.CopyTo(dst);
                EnsureNextValueOrBlockEnd();
                return;
        }
    }
    public string? ReadString()
    {
        EnsureSchemaAndToken(EdfPrimitiveType.String, TextTokenType.StringLiteral);
        var str = _tokenizer.GetString();
        EnsureNextValueOrBlockEnd();
        return str;
    }
    public byte[] ReadCharArray()
    {
        EnsureSchemaAndToken(EdfPrimitiveType.Char, TextTokenType.StringLiteral);
        var content = _tokenizer.TokenValue;
        int len = (int)CurrentType.GetTotalElements();
        var result = new byte[len];
        content.Slice(0, int.Min(len, content.Length)).CopyTo(result);
        EnsureNextValueOrBlockEnd();
        return result;
    }


}
