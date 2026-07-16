namespace EdfNet.Gen;

public class WriterBin : BaseWriterBin
{
    public WriterBin(Stream stream, Config? cfg = default)
        : base(stream, cfg)
    {

    }

    public override EdfErr Write(object obj)
    {
        throw new NotImplementedException();
    }

    public override EdfErr WriteEnumerator<TEnumerator>(ref TEnumerator enumerator)
        where TEnumerator : struct
    {
        // Берем срез свободного места в текущем буфере блока
        Span<byte> _blockDataBuffer = _blkData.DataBuffer.Slice(_blkData.DataLen);
        while (enumerator.MoveNext())
        {
            // Пробуем записать примитив в доступный остаток блока
            int bytesWritten = enumerator.Write(_blockDataBuffer);
            if (0 >= bytesWritten)// Если вернулся меньше 0, значит примитив целиком не поместился (попримитивный разрыв).
            {
                Flush();// Сбрасываем (Flush) текущий блок на диск/в поток и очищаем буфер

                // Пересчитываем срез свободного места для абсолютно нового, чистого блока
                _blockDataBuffer = _blkData.DataBuffer;
                // Пробуем записать примитив еще раз, теперь уже в начало нового блока
                bytesWritten = enumerator.Write(_blockDataBuffer);
                if (0 >= bytesWritten) // Защита от бесконечного цикла (если примитив физически больше размера блока)
                    return EdfErr.DstBufOverflow;
            }
            _blkData.PrimOffset++;
            // Фиксируем, сколько байт реально записал энумератор в буфер блока
            _blkData.DataLen += (ushort)bytesWritten;

            // Сдвигаем наш Span вперед, отрезая уже заполненную часть памяти
            _blockDataBuffer = _blockDataBuffer.Slice(bytesWritten);
        }
        _blkData.RecordId++;
        _blkData.PrimOffset = 0;
        return EdfErr.IsOk;
    }
}
