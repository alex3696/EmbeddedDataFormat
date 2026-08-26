namespace EdfNet.Interfaces;

public interface IWriter
{
    public EdfConfig Cfg { get; }
    void WriteConfig(EdfConfig cfg);
    void WriteSchema(EdfSchema sch);
    //EdfErr Write(object obj);
    public EdfErr WriteValue<T>(in T val);
    public EdfErr WriteInfData<T>(ushort id, PoType pt, string name, T d);
    void Flush();
}
