namespace EdfNet.Interfaces;

public interface IEdfReader
{
    EdfSchema? CurrentSchema { get; }
    bool ReadBlock();
    EdfBlockType GetBlockType();
    public T ReadValue<T>() where T : new();
    public void ReadValue<T>(ref T result);
}
