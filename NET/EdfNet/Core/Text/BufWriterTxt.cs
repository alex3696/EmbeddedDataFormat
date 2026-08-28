using EdfNet.Core.Binary;

namespace EdfNet.Core.Text;

public readonly ref struct BufWriterTxt : IBufWriter
{
    private readonly BufferedTextWriter _writer;
    public readonly TextCircularEdfTypeEnumerator Enum;

    public EdfType CurrentType => Enum.CurrentType;

    public BufWriterTxt(BufferedTextWriter writer, TextCircularEdfTypeEnumerator enm)
    {
        _writer = writer;
        Enum = enm;
    }
    public void Write<T>(T val) where T : struct
    {
        EnsureSchemaAndToken(typeof(T).GetPoType(), typeof(T));
        _writer.WriteNumber(val);
        EnsureNextValueOrBlockEnd();
    }
    public void Write(string? str)
    {
        EnsureSchemaAndToken(EdfPrimitiveType.String, typeof(string));
        _writer.WriteString(str, quoted: true, EdfBinString.MaxLen);
        EnsureNextValueOrBlockEnd();
    }
    public void WriteCharArray(ReadOnlySpan<byte> charArray)
    {
        EnsureSchemaAndToken(EdfPrimitiveType.Char, typeof(byte[]));
        int len = (int)CurrentType.GetTotalElements();
        ArgumentOutOfRangeException.ThrowIfNegative(len);
        int datLen = int.Min(len, charArray.Length);
        int firstZero = charArray.IndexOf(EdfTokenLiterals.Zero);
        if (firstZero >= 0)
            datLen = int.Min(datLen, firstZero);
        _writer.Write(EdfTokenLiterals.Quote);
        _writer.Write(charArray.Slice(0, datLen));
        _writer.Write(EdfTokenLiterals.Quote);
        EnsureNextValueOrBlockEnd();
    }
    public void WriteSpan(ReadOnlySpan<byte> src, EdfPrimitiveType pt)
    {
        switch (pt)
        {
            default: throw new EdfWrongTypeException();
            case EdfPrimitiveType.Int8: Write(MemoryMarshal.Read<sbyte>(src)); break;
            case EdfPrimitiveType.UInt8: Write(MemoryMarshal.Read<byte>(src)); break;
            case EdfPrimitiveType.Int16: Write(MemoryMarshal.Read<short>(src)); break;
            case EdfPrimitiveType.UInt16: Write(MemoryMarshal.Read<ushort>(src)); break;
            case EdfPrimitiveType.Int32: Write(MemoryMarshal.Read<int>(src)); break;
            case EdfPrimitiveType.UInt32: Write(MemoryMarshal.Read<uint>(src)); break;
            case EdfPrimitiveType.Int64: Write(MemoryMarshal.Read<long>(src)); break;
            case EdfPrimitiveType.UInt64: Write(MemoryMarshal.Read<ulong>(src)); break;
            case EdfPrimitiveType.Single: Write(MemoryMarshal.Read<float>(src)); break;
            case EdfPrimitiveType.Double: Write(MemoryMarshal.Read<double>(src)); break;
            case EdfPrimitiveType.String:
                EnsureSchemaAndToken(EdfPrimitiveType.String, typeof(string));
                _writer.Write(EdfTokenLiterals.Quote);
                _writer.Write(src);
                _writer.Write(EdfTokenLiterals.Quote);
                EnsureNextValueOrBlockEnd();
                break;
            case EdfPrimitiveType.Char:
                WriteCharArray(src);
                break;
        }
    }
    private void EnsureSchemaAndToken(EdfPrimitiveType pot, Type valueType)
    {
        while (Enum.CurrentToken != TypeTokenType.Value)
            SkipNonValueItem();
        if (CurrentType.Type != pot)
            throw new EdfWrongTypeException($"expected from schema {CurrentType.Type} got {pot}");

        if (EdfPrimitiveType.Char == pot)
        {
            if (!valueType.IsArray || valueType.GetElementType() != typeof(byte))
                throw new EdfWrongTypeException($"Expected {pot} but got {valueType.Name}");
        }
        else if (valueType.GetPoType() != pot)
            throw new EdfWrongTypeException($"Expected {pot} but got {valueType.Name}");

    }
    private void EnsureNextValueOrBlockEnd()
    {
        _writer.Write(EdfTokenLiterals.VarEnd);
        Enum.MoveNext();
        while (Enum.CurrentToken != TypeTokenType.Value && Enum.CurrentToken != TypeTokenType.EndRecord)
        {
            SkipNonValueItem();
        }
        if (Enum.CurrentToken == TypeTokenType.EndRecord) // set to first value of next record
            SkipNonValueItem();
    }
    private void SkipNonValueItem()
    {
        var token = Enum.CurrentToken;
        switch (token)
        {
            case TypeTokenType.BeginRecord:
                _writer.Write(EdfTokenLiterals.RecBegin);
                _writer.Write(EdfTokenLiterals.Space);
                break;
            case TypeTokenType.EndRecord:
                _writer.Write(EdfTokenLiterals.BlockEnd);
                _writer.Write(EdfTokenLiterals.EndLine);
                break;
            case TypeTokenType.BeginStruct: _writer.Write(EdfTokenLiterals.StructBegin); break;
            case TypeTokenType.EndStruct: _writer.Write(EdfTokenLiterals.StructEnd); break;
            case TypeTokenType.BeginArray: _writer.Write(EdfTokenLiterals.ArrayBegin); break;
            case TypeTokenType.EndArray: _writer.Write(EdfTokenLiterals.ArrayEnd); break;
            default: throw new NotSupportedException($"Token {token} not supported.");
        }
        Enum.MoveNext();
    }
}
