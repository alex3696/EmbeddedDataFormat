namespace EdfNet.Interfaces;

public interface IBufReader
{
    int Read<T>(out T val) where T : struct;
    int Read(out string? val);
    int Write(out ReadOnlySpan<byte> val);
    bool ReadRecBegin();
    bool ReadRecEnd();
    bool ReadBeginStruct();
    bool ReadEndStruct();
    bool ReadBeginArray();
    bool ReadEndArray();
    bool ReadVarEnd();
}
