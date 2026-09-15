namespace EdfNet.Interfaces;

public interface IEdfReader
{
    EdfSchema? CurrentSchema { get; }
    bool ReadBlock();
    EdfBlockType GetBlockType();
    public T ReadValue<T>();
}
