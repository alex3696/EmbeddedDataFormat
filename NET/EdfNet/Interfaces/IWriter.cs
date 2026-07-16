namespace EdfNet.Interfaces;

public interface IWriter
{
    public Config Cfg { get; }
    void Write(Config cfg);
    void Write(Schema sch);
    EdfErr Write(object obj);
    EdfErr WriteEnumerator<TEnumerator>(ref TEnumerator enumerator)
        where TEnumerator : struct, IEdfByteEnumerator;
    void Flush();
}


public static class BaseWriterExt
{
    public static EdfErr WriteInfData(this IWriter dw, ushort id, PoType pt, string name, object d)
    {
        dw.Write(new Schema() { Id = id, Type = new(pt), Name = name, });
        return dw.Write(d);
    }
}
