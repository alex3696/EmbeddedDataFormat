namespace EdfNet.Interfaces;

public interface IBufWriter
{
    void Write<T>(T val) where T : struct; // + void VarEnd();
    void Write(string? val); // + void VarEnd();
    void WriteCharArray(ReadOnlySpan<byte> charArray, int len); // + void VarEnd();
    EdfType? GetCurrentType();
    void RecBegin();
    void RecEnd();
    void BeginStruct();
    void EndStruct();
    void BeginArray();
    void EndArray();
    void VarEnd();
}
