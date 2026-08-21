namespace EdfNet.Interfaces;

public interface IBufWriter
{
    void Write<T>(T val) where T : struct; // + void VarEnd();
    void Write(string? val); // + void VarEnd();
    void WriteCharArray(ReadOnlySpan<byte> charArray); // + void VarEnd();
    EdfType CurrentType { get; }
}
