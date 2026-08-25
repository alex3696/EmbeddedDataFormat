namespace EdfNet.Interfaces;

public interface IBufferedReader
{
    /// <summary>
    /// Возвращает текущий доступный буфер. Гарантирует minimumLength байт, если данные ещё есть.
    /// </summary>
    ReadOnlySpan<byte> GetSpan(int minimumLength = 0);

    /// <summary>
    /// Продвинуть позицию чтения на count байт.
    /// </summary>
    void Advance(int count);

    /// <summary>
    /// Общее количество прочитанных/пропущенных байт.
    /// </summary>
    long Consumed { get; }
}
