namespace EdfNet.Interfaces;

public interface IBufWriter
{
    int Write<T>(T val) where T : struct; // + void VarEnd();
    int Write(string? val); // + void VarEnd();
    int WriteCharArray(ReadOnlySpan<byte> charArray, int len); // + void VarEnd();
    int Write(ReadOnlySpan<byte> val);
    void RecBegin();
    void RecEnd();
    void BeginStruct();
    void EndStruct();
    void BeginArray();
    void EndArray();
    void VarEnd();
}
