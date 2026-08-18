namespace EdfNet.Interfaces;

public interface IBufReader
{
    T Read<T>() where T : struct; // + void ReadVarEnd();
    string? ReadString(); // + void ReadVarEnd();
    byte[] ReadCharArray(int len); // + void ReadVarEnd();
    EdfType? GetCurrentType();
    bool ReadRecBegin();
    bool ReadRecEnd();
    bool ReadBeginStruct();
    bool ReadEndStruct();
    bool ReadBeginArray();
    bool ReadEndArray();
    bool ReadVarEnd();
}
