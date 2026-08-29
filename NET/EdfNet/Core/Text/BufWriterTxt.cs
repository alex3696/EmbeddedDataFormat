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
    public void Write(byte val)
    {
        EnsureSchemaAndToken(EdfPrimitiveType.UInt8, typeof(byte));
        _writer.WriteNumber(val);
        EnsureNextValueOrBlockEnd();
    }
    public void Write(sbyte val)
    {
        EnsureSchemaAndToken(EdfPrimitiveType.Int8, typeof(sbyte));
        _writer.WriteNumber(val);
        EnsureNextValueOrBlockEnd();
    }
    public void Write(ushort val)
    {
        EnsureSchemaAndToken(EdfPrimitiveType.UInt16, typeof(ushort));
        _writer.WriteNumber(val);
        EnsureNextValueOrBlockEnd();
    }
    public void Write(short val)
    {
        EnsureSchemaAndToken(EdfPrimitiveType.Int16, typeof(short));
        _writer.WriteNumber(val);
        EnsureNextValueOrBlockEnd();
    }
    public void Write(uint val)
    {
        EnsureSchemaAndToken(EdfPrimitiveType.UInt32, typeof(uint));
        _writer.WriteNumber(val);
        EnsureNextValueOrBlockEnd();
    }
    public void Write(int val)
    {
        EnsureSchemaAndToken(EdfPrimitiveType.Int32, typeof(int));
        _writer.WriteNumber(val);
        EnsureNextValueOrBlockEnd();
    }
    public void Write(ulong val)
    {
        EnsureSchemaAndToken(EdfPrimitiveType.UInt64, typeof(ulong));
        _writer.WriteNumber(val);
        EnsureNextValueOrBlockEnd();
    }
    public void Write(long val)
    {
        EnsureSchemaAndToken(EdfPrimitiveType.Int64, typeof(long));
        _writer.WriteNumber(val);
        EnsureNextValueOrBlockEnd();
    }
    public void Write(double val)
    {
        EnsureSchemaAndToken(EdfPrimitiveType.Double, typeof(double));
        _writer.WriteNumber(val);
        EnsureNextValueOrBlockEnd();
    }
    public void Write(float val)
    {
        EnsureSchemaAndToken(EdfPrimitiveType.Single, typeof(float));
        _writer.WriteNumber(val);
        EnsureNextValueOrBlockEnd();
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
            case EdfPrimitiveType.Int8: Write(Unsafe.As<byte, sbyte>(ref MemoryMarshal.GetReference(src))); break;
            case EdfPrimitiveType.UInt8: Write(Unsafe.As<byte, byte>(ref MemoryMarshal.GetReference(src))); break;
            case EdfPrimitiveType.Int16: Write(Unsafe.As<byte, short>(ref MemoryMarshal.GetReference(src))); break;
            case EdfPrimitiveType.UInt16: Write(Unsafe.As<byte, ushort>(ref MemoryMarshal.GetReference(src))); break;
            case EdfPrimitiveType.Int32: Write(Unsafe.As<byte, int>(ref MemoryMarshal.GetReference(src))); break;
            case EdfPrimitiveType.UInt32: Write(Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(src))); break;
            case EdfPrimitiveType.Int64: Write(Unsafe.As<byte, long>(ref MemoryMarshal.GetReference(src))); break;
            case EdfPrimitiveType.UInt64: Write(Unsafe.As<byte, ulong>(ref MemoryMarshal.GetReference(src))); break;
            case EdfPrimitiveType.Single: Write(Unsafe.As<byte, float>(ref MemoryMarshal.GetReference(src))); break;
            case EdfPrimitiveType.Double: Write(Unsafe.As<byte, double>(ref MemoryMarshal.GetReference(src))); break;
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
