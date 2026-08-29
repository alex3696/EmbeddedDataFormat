namespace EdfNet.Interfaces;

public interface IBufWriter
{
    void WriteSpan(ReadOnlySpan<byte> src, EdfPrimitiveType pt);
    void Write(byte val);
    void Write(sbyte val);
    void Write(ushort val);
    void Write(short val);
    void Write(uint val);
    void Write(int val);
    void Write(ulong val);
    void Write(long val);
    void Write(double val);
    void Write(float val);
    void Write<T>(T val) where T : struct; // + void VarEnd();
    void Write(string? val); // + void VarEnd();
    void WriteCharArray(ReadOnlySpan<byte> charArray); // + void VarEnd();
    EdfType CurrentType { get; }
}
