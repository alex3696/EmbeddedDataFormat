namespace EdfNet.Interfaces;

public interface IBufReader
{
    void ReadToSpan(Span<byte> dst, out EdfPrimitiveType pt, out int len);
    T Read<T>() where T : struct; // + void ReadVarEnd();
    string? ReadString(); // + void ReadVarEnd();
    byte[] ReadCharArray(); // + void ReadVarEnd();
    EdfType CurrentType { get; }
}
