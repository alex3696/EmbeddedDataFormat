namespace EdfNet.Interfaces;

public interface IEdfReader
{
    bool ReadBlock();
    EdfBlockType GetBlockType();
    public T ReadValue<T>();
}
