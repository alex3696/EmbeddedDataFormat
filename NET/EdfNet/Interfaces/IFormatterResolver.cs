namespace EdfNet.Interfaces;

public interface IFormatterResolver
{
    // Возвращает форматтер для конкретного типа T
    IFormatter<T>? GetFormatter<T>();
}

