namespace EdfNet.Interfaces;

public interface IWriter
{
    public Config Cfg { get; }
    void WriteConfig(Config cfg);
    void WriteSchema(Schema sch);
    //EdfErr Write(object obj);
    public EdfErr WriteValue<T>(in T val);
    public EdfErr WriteInfData<T>(ushort id, PoType pt, string name, T d);
    void Flush();
}
