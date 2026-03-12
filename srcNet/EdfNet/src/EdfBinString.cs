namespace NetEdf;

public static class EdfBinString
{
    // Получение количества байт, необходимых для хранения строки в бинарном формате.
    public static int SizeOf(string? str)
    {
        if (string.IsNullOrEmpty(str))
            return 1;
        return (byte)int.Min(0xFE, Encoding.UTF8.GetByteCount(str));
    }
    // Запись строки в бинарном формате в поток. Возвращает количество записанных байт или -1 в случае ошибки.
    public static int WriteBin(string? str, Stream dst)
    {
        Span<byte> buf = stackalloc byte[256]; // Буфер для хранения бинарного представления строки
        var ret = WriteBin(str, buf); // Записываем строку в буфер и получаем количество байт, необходимых для хранения строки
        if (0 < ret) // Если запись прошла успешно, то записываем данные из буфера в поток
        {
            dst.Write(buf.Slice(0, ret)); // Записываем данные из буфера в поток
        }
        return ret; // Возвращаем количество записанных байт
    }
    // Запись строки в бинарный формат
    public static int WriteBin(string? str, Span<byte> dst)
    {
        if (1 > dst.Length) // Если буфер недостаточно велик для хранения строки, возвращаем -1
            return -1;
        if (string.IsNullOrEmpty(str)) // Если строка пустая или null, записываем 0 в первый байт и возвращаем 1
        {
            dst[0] = 0;
            return 1;
        }
        var len = (byte)int.Min(0xFE, Encoding.UTF8.GetByteCount(str)); // Вычисляем количество байт, необходимых для хранения строки, ограничивая его 0xFE
        if (len > dst.Length) // Если буфер недостаточно велик для хранения строки, возвращаем -1
            return dst.Length - len;
        Encoding.UTF8.GetBytes(str, dst.Slice(1, len)); // Записываем строку в буфер, начиная со второго байта
        dst[0] = len; // Записываем количество байт, необходимых для хранения строки, в первый байт
        return 1 + len; // Возвращаем общее количество байт, записанных в буфер (1 байт для длины + байты для строки)
    }
    // Чтение строки из бинарного формата. Возвращает количество прочитанных байт или -1 в случае ошибки.
    public static int ReadBin(ReadOnlySpan<byte> b, out string? str)
    {
        if (1 > b.Length) // Если входные данные недостаточно велики для хранения строки, возвращаем -1
        {
            str = null;
            return -1;
        }
        var len = b[0]; // Читаем количество байт, необходимых для хранения строки, из первого байта
        if (byte.MaxValue == len) // Если длина строки равна 0xFF, это означает переполнение
            throw new ArgumentException("BString overflow");
        if (len > b.Length) // Если входные данные недостаточно велики для хранения строки, возвращаем -1
        {
            str = null;
            return -1;
        }
        str = Encoding.UTF8.GetString(b.Slice(1, len)); //Преобразуем байты в строку
        return 1 + len; // Возвращаем общее количество байт, прочитанных из входных данных 
    }
}
