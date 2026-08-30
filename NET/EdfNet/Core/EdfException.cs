using EdfNet.Core.Text;

namespace EdfNet.Core;

public class EdfException : Exception
{
    public EdfException() : base() { }
    public EdfException(string? msg) : base(msg) { }
}
public class EdfWrongTypeException : Exception
{
    public EdfWrongTypeException() : base() { }
    public EdfWrongTypeException(string? msg) : base(msg) { }

}
public class EdfSrcDataRequredException : Exception { }
public class EdfDstBufOverflowException : Exception { }

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
}

public class EdfFormatterNotRegistredException : EdfException
{
    public EdfFormatterNotRegistredException(string? msg) : base(msg) { }
    public EdfFormatterNotRegistredException(Type type)
        : base($"Formatter for type {type.FullName} not registred")
    {
    }

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
public class IncomatiblePrimitiveAndValueException : EdfException
{
    public IncomatiblePrimitiveAndValueException(EdfPrimitiveType expected, Type got)
        : base($"EdfPrimitiveType: {expected} not compatible to value {got.Name}")
    { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfNotComatible(EdfPrimitiveType expected, Type got)
    {
        if (!expected.IsSame(got))
            throw new IncomatiblePrimitiveAndValueException(expected, got);
    }
}
