namespace NetEdf.src;

// заменить на IEnumerable<object>
// Класс для чтения примитивов из бинарного представления, учитывая структуру данных и их вложенность
public static class PrimitiveListReader
{
    // Главный метод для чтения объектов, который определяет, является ли текущий тип массивом или структурой, и вызывает соответствующий метод для чтения
    public static EdfErr ReadObjects(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, List<object> ret)
    {
        uint totalElement = t.GetTotalElements();
        if (1 < totalElement)
            return ReadArray(t, src, totalElement, ref skip, ref qty, ref readed, ret);
        return ReadElement(t, src, ref skip, ref qty, ref readed, ret);
    }
    // Метод для чтения одного элемента, который определяет, является ли элемент структурой или примитивом, и вызывает соответствующий метод для чтения
    static EdfErr ReadElement(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, List<object> ret)
    {
        if (PoType.Struct == t.Type)
            return ReadStruct(t, src, ref skip, ref qty, ref readed, ret);
        return ReadPrimitive(t, src, ref skip, ref qty, ref readed, ret);
    }
    // Метод для чтения массива, который последовательно читает каждый элемент массива, обновляя позицию в исходных данных и количество прочитанных элементов
    static EdfErr ReadArray(TypeInf t, ReadOnlySpan<byte> src, uint totalElement, ref int skip, ref int qty, ref int readed, List<object> ret)
    {
        EdfErr err = EdfErr.IsOk;
        for (int i = 0; i < totalElement; i++)
        {
            var r = readed;
            if (EdfErr.IsOk != (err = ReadElement(t, src, ref skip, ref qty, ref readed, ret)))
                return err;
            src = src.Slice(readed - r);
        }
        return err;
    }
    // Метод для чтения структуры, который последовательно читает каждый элемент структуры, обновляя позицию в исходных данных и количество прочитанных элементов
    static EdfErr ReadStruct(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, List<object> ret)
    {
        EdfErr err = EdfErr.IsOk;
        if (null == t.Childs || 0 == t.Childs.Length)
            return EdfErr.IsOk;
        foreach (var child in t.Childs)
        {
            var r = readed;
            if (EdfErr.IsOk != (err = ReadObjects(child, src, ref skip, ref qty, ref readed, ret)))
                return err;
            src = src.Slice(readed - r);
        }
        return err;
    }
    // Метод для чтения примитивного типа, который проверяет, нужно ли пропустить элемент,
    // и если нет, то пытается преобразовать бинарные данные в исходное значение и добавляет его в результат
    static EdfErr ReadPrimitive(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, List<object> ret)
    {
        if (0 < skip)
        {
            skip--;
            return EdfErr.IsOk;
        }
        EdfErr err = EdfErr.IsOk;
        if (0 != (err = Primitives.TryBinToSrc(t.Type, src, out var r, out var retVal)))
            return err;
        if(null != retVal)
            ret.Add(retVal);
        readed += r; // Увеличиваем количество прочитанных байт
        qty++; // Увеличиваем количество прочитанных элементов
        return err;
    }
}
