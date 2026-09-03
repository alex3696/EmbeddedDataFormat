namespace EdfNet.Interfaces;

public interface IBufReader
{
    public void ReadTo<TWriter>(ref TWriter writer) where TWriter : IBufWriter, allows ref struct;
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
