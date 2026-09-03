using EdfNet.Core.Binary;
using EdfNet.Core.Text;

namespace EdfNet.Core;

public class EdfException : Exception
{
    public EdfException() : base() { }
    public EdfException(string? msg) : base(msg) { }
}
public class EdfWrongTypeException : EdfException
{
    public EdfWrongTypeException() : base() { }
    public EdfWrongTypeException(string? msg) : base(msg) { }

}
public class EdfSrcDataRequiredException : EdfException { }
public class EdfDstBufOverflowException : EdfException { }

public class ConvertException(string msg) : Exception(msg) { }

public class EdfParseException : EdfException
{
    public int Line { get; }
    public int Column { get; }

    public EdfParseException(string message, int line, int column)
        : base($"[{line}:{column}] {message}")
    {
        Line = line;
        Column = column;
    }
    public static void ThrowExpected(TextTokenType expected, TextTokenType got, EdfTokenReader reader)
            => throw new EdfParseException($"Expected {EdfTokenReader.Describe(expected)} " +
                $"but got {EdfTokenReader.Describe(got)}", reader.TokenLine, reader.TokenColumn);
    public static void ThrowIfNotEqual(TextTokenType expected, TextTokenType got, EdfTokenReader reader)
    {
        if (expected != got)
            ThrowExpected(expected, got, reader);
    }
}

public class EdfFormatterNotRegistredException(Type type)
    : EdfException($"Formatter for type {type.FullName} not registred")
{
    public static void ThrowIfNull<T>(T obj)
    {
        if (obj is null)
            throw new EdfFormatterNotRegistredException(typeof(T));
    }
}

public class EdfTokenNotSupportedException(TypeTokenType got)
    : EdfException($"EdfTypeToken {got} not supported here")
{ }
public class PrimitiveNotSupportedException(EdfPrimitiveType got)
    : EdfException($"EdfPrimitiveType {got} not supported")
{ }
public class NetTypeNotSupportedException(Type got)
    : EdfException($".NET Type {got} not supported")
{ }

public class WrongPrimitiveException : EdfException
{
    public WrongPrimitiveException(EdfPrimitiveType expected, EdfPrimitiveType got)
        : base($"EdfPrimitiveType: Expected {expected} but got {got}")
    { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotEqual(EdfPrimitiveType expected, EdfPrimitiveType got)
    {
        if (expected != got)
            throw new WrongPrimitiveException(expected, got);
    }
}
public class IncompatiblePrimitiveAndValueException : EdfException
{
    public IncompatiblePrimitiveAndValueException(EdfPrimitiveType expected, Type got)
        : base($"EdfPrimitiveType: {expected} not compatible to value {got.Name}")
    { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotCompatible(EdfPrimitiveType expected, Type got)
    {
        if (!expected.IsSame(got))
            throw new IncompatiblePrimitiveAndValueException(expected, got);
    }
}

public class BinaryBlockIntegrityException : EdfException
{
    public BinaryBlockIntegrityException(ushort expected, ushort got)
        : base($"Crc expected {expected} got {got}")
    { }
    public static void ThrowIfCrcWrong(BinBlock block)
    {
        var calculatedCrc = block.CalcCrc();
        if (calculatedCrc != block.Crc)
            throw new BinaryBlockIntegrityException(calculatedCrc, block.Crc);
    }
}
public class BinaryBlockSequenceException : EdfException
{
    public BinaryBlockSequenceException(string what)
        : base("Wrong Sequence " + what)
    {
    }
    private static void ThrowIfNotEqual<T>(string what, T expected, T got)
    {
        if (0 == Comparer<T>.Default.Compare(expected, got))
            return;
        var msg = $"expected {what} {expected} got {got}";
        throw new BinaryBlockSequenceException(msg);
    }
    public static void ThrowIfNotEqualSchemaId(ushort expected, ushort got)
        => ThrowIfNotEqual("SchemaId", expected, got);
    public static void ThrowIfNotEqualRecordId(uint expected, uint got)
        => ThrowIfNotEqual("RecordId", expected, got);
    public static void ThrowIfNotEqualPrimOffset(ushort expected, ushort got)
        => ThrowIfNotEqual("PrimOffset", expected, got);
    public static void ThrowIfBlockTypeNotEqual(EdfBlockType expected, EdfBlockType got)
        => ThrowIfNotEqual("EdfBlockType", expected, got);
}

public class BinaryBlockWrongLengthException : EdfException
{
    public BinaryBlockWrongLengthException(long expected, long got)
        : base($"Lenght expected {expected} got {got}")
    { }
    public static void ThrowIfLess(long expected, long got)
    {
        if (expected > got)
            throw new BinaryBlockWrongLengthException(expected, got);
    }
}

public class BinaryBlockHasTrashException : EdfException
{
    public BinaryBlockHasTrashException(int available)
        : base($"not consumed bytes {available}")
    { }

}
