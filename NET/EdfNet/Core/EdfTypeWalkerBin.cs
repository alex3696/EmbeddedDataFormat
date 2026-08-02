namespace EdfNet.Core;

public static class EdfTypeWalkerBin
{
    public static void Process(EdfType et, IPrimitiveIo io)
    {
        WriteObj(et, ref io);//boxing
    }
    public static void Process<T>(EdfType et, ref T io)
        where T : IPrimitiveIo, allows ref struct
    {
        WriteObj(et, ref io);
    }
    private static void WriteObj<T>(EdfType et, ref T io)
        where T : IPrimitiveIo, allows ref struct
    {
        if (PoType.Char == et.Type)
        {
            io.Primitive(et);
            return;
        }
        uint totalElement = et.GetTotalElements();
        for (int i = 0; i < totalElement; i++)
            WriteObjElement(et, ref io);
    }
    private static void WriteObjElement<T>(EdfType et, ref T io)
        where T : IPrimitiveIo, allows ref struct
    {
        if (PoType.Struct == et.Type)
        {
            if (et.Childs != null && 0 != et.Childs.Length)
            {
                for (int childIndex = 0; childIndex < et.Childs.Length; childIndex++)
                    WriteObj(et.Childs[childIndex], ref io);
            }
        }
        else
        {
            io.Primitive(et);
        }
    }
}
