namespace EdfNet.Core;

public class EdfException : Exception
{
    public EdfException() : base() { }
    public EdfException(string? msg) : base(msg) { }
}
public class EdfWrongTypeException : Exception { }
public class EdfSrcDataRequredException : Exception { }
public class EdfDstBufOverflowException : Exception { }

public interface IPrimitiveIo
{
    void Primitive(EdfType inf);
    void SepRecBegin();
    void SepRecEnd();
    void SepBeginStruct();
    void SepEndStruct();
    void SepBeginArray();
    void SepEndArray();
    void SepVarEnd();

    //int PrimitivesWritted { get; }
    //int BytesWritted { get; }
    //int Skip { get; set; }
}

public static class EdfTypeWalker
{
    public static void Process(EdfType et, IPrimitiveIo io)
    {
        WriteObj(et, ref io);//boxing
    }
    public static void Process<T>(EdfType et, ref T io)
        where T : IPrimitiveIo, allows ref struct
    {
        io.SepRecBegin();
        WriteObj(et, ref io);
        io.SepRecEnd();
    }
    private static void WriteObj<T>(EdfType inf, ref T io)
        where T : IPrimitiveIo, allows ref struct
    {
        if (PoType.Char == inf.Type)
        {
            io.Primitive(inf);
            io.SepVarEnd();
            return;
        }
        uint totalElement = inf.GetTotalElements();
        if (1 < totalElement)
            io.SepBeginArray();
        for (int i = 0; i < totalElement; i++)
            WriteObjElement(inf, ref io);
        if (1 < totalElement)
            io.SepEndArray();
    }
    private static void WriteObjElement<T>(EdfType inf, ref T io)
        where T : IPrimitiveIo, allows ref struct
    {
        if (PoType.Struct == inf.Type)
        {
            if (inf.Childs != null && 0 != inf.Childs.Length)
            {
                io.SepBeginStruct();
                for (int childIndex = 0; childIndex < inf.Childs.Length; childIndex++)
                    WriteObj(inf.Childs[childIndex], ref io);
                io.SepEndStruct();
            }
        }
        else
        {
            io.Primitive(inf);
            io.SepVarEnd();
        }
    }
}

