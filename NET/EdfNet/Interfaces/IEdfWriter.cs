namespace EdfNet.Interfaces;

public interface IEdfWriter
{
    public EdfConfig Cfg { get; }
    void WriteConfig(EdfConfig cfg);
    void WriteSchema(EdfSchema sch);
    //EdfErr Write(object obj);
    public EdfErrorCode WriteValue<T>(in T val);
    void Flush();
}

public static class IEdfWriterExt
{
    public static EdfErrorCode WriteInfData<T>(this IEdfWriter writer,
        ushort id, string? name, string? desc, EdfPrimitiveType pt, T value)
    {
        var sch = new EdfSchema() { Id = id, Name = name, Desc = desc, Type = new(pt) };
        return writer.WriteInfData(sch, value);
    }
    public static EdfErrorCode WriteInfData<T>(this IEdfWriter writer, EdfSchema sch, T value)
    {
        writer.WriteSchema(sch);
        return writer.WriteValue(value);
    }
}
