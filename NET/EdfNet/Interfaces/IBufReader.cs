namespace EdfNet.Interfaces;

public interface IBufReader
{
    void ReadToSpan(Span<byte> dst, out EdfPrimitiveType pt, out int len);
    T Read<T>() where T : struct; // + void ReadVarEnd();
    byte ReadUInt8();
    sbyte ReadInt8();
    ushort ReadUInt16();
    short ReadInt16();
    uint ReadUInt32();
    int ReadInt32();
    ulong ReadUInt64();
    long ReadInt64();
    float ReadSingle();
    double ReadDouble();
    string? ReadString(); // + void ReadVarEnd();
    byte[] ReadCharArray(); // + void ReadVarEnd();
    EdfType CurrentType { get; }
}
