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

public class EdfTypeWalker
{
    public void Process(EdfType et, IPrimitiveIo io)
    {
        io.SepRecBegin();
        WriteObj(et, io);
        io.SepRecEnd();
    }
    private void WriteObj(EdfType inf, IPrimitiveIo io)
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
            WriteObjElement(inf, io);
        if (1 < totalElement)
            io.SepEndArray();
    }
    private void WriteObjElement(EdfType inf, IPrimitiveIo io)
    {
        if (PoType.Struct == inf.Type)
        {
            if (inf.Childs != null && 0 != inf.Childs.Length)
            {
                io.SepBeginStruct();
                for (int childIndex = 0; childIndex < inf.Childs.Length; childIndex++)
                    WriteObj(inf.Childs[childIndex], io);
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

