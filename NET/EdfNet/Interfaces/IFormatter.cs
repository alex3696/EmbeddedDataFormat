namespace EdfNet.Interfaces;

public interface IFormatter<T>
{
    void Serialize<TWriter>(ref TWriter writer, in T value, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter, allows ref struct;
    T Deserialize<TReader>(ref TReader reader, EdfFormatterOptions options)
        where TReader : struct, IBufReader, allows ref struct;
}
