namespace EdfNet.Interfaces;

public interface IEdfByteEnumerator
{
    // Продвижение к следующему примитиву
    bool MoveNext(EdfType et = default);
    // Получение порядкового номера текущего примитива 
    int CurrentIndex { get; }
    // Получение Тип текущего примитива 
    PoType CurrentPoType { get; }
    // Получение Длина текущего примитива
    int CurrentPoLen { get; }
    // Запись сырых байт примитива напрямую в предоставленный Span
    int Write(Span<byte> destination);
    // Чтение сырых байт из Span в примитив напрямую
    int Read(ReadOnlySpan<byte> src);
}

public interface IEdfByteEnumerator<T> : IEdfByteEnumerator
{
    T Result { get; }
}
