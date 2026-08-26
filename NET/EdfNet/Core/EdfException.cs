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
