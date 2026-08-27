namespace EdfNet.Interfaces;

public interface IWriter
{
    public EdfConfig Cfg { get; }
    void WriteConfig(EdfConfig cfg);
    void WriteSchema(EdfSchema sch);
    //EdfErr Write(object obj);
    public EdfErrorCode WriteValue<T>(in T val);
    public EdfErrorCode WriteInfData<T>(ushort id, EdfPrimitiveType pt, string name, T d);
    void Flush();
}
