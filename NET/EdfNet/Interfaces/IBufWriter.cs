namespace EdfNet.Interfaces;

public interface IBufWriter
{
    int Write<T>(T val) where T : struct;
    int Write(string? val);
    int Write(ReadOnlySpan<byte> val);
    void RecBegin();
    void RecEnd();
    void BeginStruct();
    void EndStruct();
    void BeginArray();
    void EndArray();
    void VarEnd();
}
