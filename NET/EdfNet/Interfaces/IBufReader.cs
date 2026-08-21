namespace EdfNet.Interfaces;

public interface IBufReader
{
    T Read<T>() where T : struct; // + void ReadVarEnd();
    string? ReadString(); // + void ReadVarEnd();
    byte[] ReadCharArray(); // + void ReadVarEnd();
    EdfType CurrentType { get; }
}
