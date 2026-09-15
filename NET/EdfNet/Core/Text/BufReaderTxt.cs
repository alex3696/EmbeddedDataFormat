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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
            default: throw new EdfTokenNotSupportedException(Enum.CurrentToken);
        }
        Enum.MoveNext();
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureToken(TextTokenType tokenType)
    {
        if (!_tokenizer.HasValidToken)
        {
            if (!_tokenizer.MoveNext())
                EdfParseException.ThrowIfNotEqual(tokenType, TextTokenType.EOF, _tokenizer);
        }
        EdfParseException.ThrowIfNotEqual(tokenType, _tokenizer.TokenType, _tokenizer);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void ValidatePrimitiveAndTextToken(EdfPrimitiveType got, TextTokenType tokenType)
    {
        while (TypeTokenType.Value != Enum.CurrentToken)// skip non value Enum.CurrentToken
            SkipNonValueItem();
        WrongPrimitiveException.ThrowIfNotEqual(CurrentType.Type, got);
        EnsureToken(tokenType);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
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
        ValidatePrimitiveAndTextToken(EdfPrimitiveType.UInt8, TextTokenType.Number);
        var b = byte.Parse(_tokenizer.TokenValue, CultureInfo.InvariantCulture);
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public sbyte ReadInt8()
    {
        ValidatePrimitiveAndTextToken(EdfPrimitiveType.Int8, TextTokenType.Number);
        var b = sbyte.Parse(_tokenizer.TokenValue, CultureInfo.InvariantCulture);
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public ushort ReadUInt16()
    {
        ValidatePrimitiveAndTextToken(EdfPrimitiveType.UInt16, TextTokenType.Number);
        var b = ushort.Parse(_tokenizer.TokenValue, CultureInfo.InvariantCulture);
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public short ReadInt16()
    {
        ValidatePrimitiveAndTextToken(EdfPrimitiveType.Int16, TextTokenType.Number);
        var b = short.Parse(_tokenizer.TokenValue, CultureInfo.InvariantCulture);
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public uint ReadUInt32()
    {
        ValidatePrimitiveAndTextToken(EdfPrimitiveType.UInt32, TextTokenType.Number);
        var b = uint.Parse(_tokenizer.TokenValue, CultureInfo.InvariantCulture);
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public int ReadInt32()
    {
        ValidatePrimitiveAndTextToken(EdfPrimitiveType.Int32, TextTokenType.Number);
        var b = int.Parse(_tokenizer.TokenValue, CultureInfo.InvariantCulture);
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public ulong ReadUInt64()
    {
        ValidatePrimitiveAndTextToken(EdfPrimitiveType.UInt64, TextTokenType.Number);
        var b = ulong.Parse(_tokenizer.TokenValue, CultureInfo.InvariantCulture);
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public long ReadInt64()
    {
        ValidatePrimitiveAndTextToken(EdfPrimitiveType.Int64, TextTokenType.Number);
        var b = long.Parse(_tokenizer.TokenValue, CultureInfo.InvariantCulture);
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public float ReadSingle()
    {
        ValidatePrimitiveAndTextToken(EdfPrimitiveType.Single, TextTokenType.Number);
        var b = float.Parse(_tokenizer.TokenValue, CultureInfo.InvariantCulture);
        EnsureNextValueOrBlockEnd();
        return b;
    }
    public double ReadDouble()
    {
        ValidatePrimitiveAndTextToken(EdfPrimitiveType.Double, TextTokenType.Number);
        var b = double.Parse(_tokenizer.TokenValue, CultureInfo.InvariantCulture);
        EnsureNextValueOrBlockEnd();
        return b;
    }

    public T Read<T>() where T : struct
    {
        switch (Type.GetTypeCode(typeof(T)))
        {
            default: throw new NetTypeNotSupportedException(typeof(T));
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
    }
    public void ReadTo<TWriter>(ref TWriter writer) where TWriter : IBufWriter, allows ref struct
    {
        while (Enum.CurrentToken != TypeTokenType.Value)
            SkipNonValueItem();
        EdfPrimitiveType pt = Enum.CurrentType.Type;
        switch (pt)
        {
            default: throw new PrimitiveNotSupportedException(pt);
            case EdfPrimitiveType.UInt8: writer.Write(ReadUInt8()); return;
            case EdfPrimitiveType.Int8: writer.Write(ReadInt8()); return;
            case EdfPrimitiveType.UInt16: writer.Write(ReadUInt16()); return;
            case EdfPrimitiveType.Int16: writer.Write(ReadInt16()); return;
            case EdfPrimitiveType.UInt32: writer.Write(ReadUInt32()); return;
            case EdfPrimitiveType.Int32: writer.Write(ReadInt32()); return;
            case EdfPrimitiveType.UInt64: writer.Write(ReadUInt64()); return;
            case EdfPrimitiveType.Int64: writer.Write(ReadInt64()); return;
            case EdfPrimitiveType.Single: writer.Write(ReadSingle()); return;
            case EdfPrimitiveType.Double: writer.Write(ReadDouble()); return;
            case EdfPrimitiveType.Char:
                EnsureToken(TextTokenType.StringLiteral);
                writer.WriteSpan(_tokenizer.TokenValue, EdfPrimitiveType.Char);
                EnsureNextValueOrBlockEnd();
                return;
            case EdfPrimitiveType.String:
                EnsureToken(TextTokenType.StringLiteral);
                writer.WriteSpan(_tokenizer.TokenValue, EdfPrimitiveType.String);
                EnsureNextValueOrBlockEnd();
                return;
        }
    }
    public void ReadToSpan(Span<byte> dst, out EdfPrimitiveType pt, out int len)
    {
        while (Enum.CurrentToken != TypeTokenType.Value)// skip non value Enum.CurrentToken
            SkipNonValueItem();
        pt = Enum.CurrentType.Type;
        switch (pt)
        {
            default: throw new PrimitiveNotSupportedException(pt);
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
                ValidatePrimitiveAndTextToken(EdfPrimitiveType.Char, TextTokenType.StringLiteral);
                var content = _tokenizer.TokenValue;
                len = (int)CurrentType.GetTotalElements();
                content.Slice(0, int.Min(len, content.Length)).CopyTo(dst);
                if (content.Length < len)
                    dst.Slice(content.Length, len - content.Length).Clear();
                EnsureNextValueOrBlockEnd();
                return;
            case EdfPrimitiveType.String:
                ValidatePrimitiveAndTextToken(EdfPrimitiveType.String, TextTokenType.StringLiteral);
                len = _tokenizer.TokenValue.Length;
                _tokenizer.TokenValue.CopyTo(dst);
                EnsureNextValueOrBlockEnd();
                return;
        }
    }
    public string? ReadString()
    {
        ValidatePrimitiveAndTextToken(EdfPrimitiveType.String, TextTokenType.StringLiteral);
        var str = _tokenizer.GetString();
        EnsureNextValueOrBlockEnd();
        return str;
    }
    public byte[] ReadCharArray()
    {
        ValidatePrimitiveAndTextToken(EdfPrimitiveType.Char, TextTokenType.StringLiteral);
        var content = _tokenizer.TokenValue;
        int len = (int)CurrentType.GetTotalElements();
        var result = new byte[len];
        content.Slice(0, int.Min(len, content.Length)).CopyTo(result);
        EnsureNextValueOrBlockEnd();
        return result;
    }
}
