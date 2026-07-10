namespace EdfNet.Interfaces;

public interface IWriter
{
    void Write(Config cfg);
    void Write(Schema sch);
    EdfErr Write(object obj);
    void Flush();

    EdfErr WriteEnumerator<TEnumerator>(ref TEnumerator enumerator)
        where TEnumerator : struct, IEdfByteEnumerator
    {
        return EdfErr.WrongType;
    }
}
