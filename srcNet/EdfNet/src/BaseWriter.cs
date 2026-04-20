using NetEdf.Base;

namespace NetEdf.src;

public abstract class BaseWriter : BaseDisposable
{
    public readonly Header Cfg;
    protected TypeInf? _currDataType; // текущая схема данных, которая должна быть записана в блок данных.

    public TypeInf? CurrDataType => _currDataType;

    public BaseWriter(Header header) // конструктор, принимает конфигурацию заголовка и инициализирует буфер для данных
    {
        Cfg = header;
        _blkData = new byte[Cfg.Blocksize];
    }
    //protected override void Dispose(bool disposing) => base.Dispose(disposing);

    protected abstract EdfErr TrySrcToX(PoType t, object obj, Span<byte> dst, out int w);
    protected abstract EdfErr WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int skip, ref int wqty, ref int writed);
    protected byte[]? SepBeginStruct = null;
    protected byte[]? SepEndStruct = null;
    protected byte[]? SepBeginArray = null;
    protected byte[]? SepEndArray = null;
    protected byte[]? SepVarEnd = null;
    protected byte[]? SepRecBegin = null;
    protected byte[]? SepRecEnd = null;

    protected ushort _blkQty; // количество байт в текущем блоке данных
    protected readonly byte[] _blkData; // буфер для хранения данных текущего блока
    protected Span<byte> _EmptySpan => _blkData.AsSpan(_blkQty); // пустой span для записи данных,
                                                                 // который начинается с текущей позиции в блоке данных и имеет длину,
                                                                 // равную оставшемуся месту в блоке

    protected int _skip = 0; // количество элементов, которые нужно пропустить при записи данных
    protected object? _currObj = null; // текущий объект данных, который должен быть записан в блок данных.
                                       // Он используется для хранения промежуточного состояния при записи сложных объектов,
                                       // таких как структуры и массивы.

    public abstract void Write(Header v);
    public abstract void Write(TypeRec t);
    public abstract void Flush();

    public delegate IEnumerator<object> CreateEnumeratorFunc(object obj);
    public CreateEnumeratorFunc CreateEnumerator = (obj) => new PrimitiveDecomposer(obj).GetEnumerator();

    // метод для записи данных, принимает объект данных и записывает его в блок данных,
    // используя текущую схему данных.
    public virtual EdfErr Write(object obj)
    {
        ArgumentNullException.ThrowIfNull(_currDataType); // проверка, что текущая схема данных не null
        IEnumerator<object> flatObj = CreateEnumerator(obj); // создание перечислителя для

        // разложения объекта на примитивные элементы
        Span<byte> dst = _blkData.AsSpan(_blkQty); // создание буфера с размером, равным оставшемуся месту в блоке данных
        EdfErr err;
        do
        {
            int skip = _skip;
            int wqty = 0; // количество элементов, которые были записаны в текущей итерации
            int writed = 0; // количество байт, которые были записаны в текущей итерации
            err = WriteSingleValue(_currDataType, ref dst, flatObj, ref skip, ref wqty, ref writed); // попытка записать один элемент данных,
                                                                                                     // используя текущую схему данных
            _blkQty += (ushort)writed; // обновляем количество байт в текущем блоке данных, так как данные были записаны в буфер
            switch (err)
            {
                default:
                case EdfErr.WrongType: return err;
                case EdfErr.SrcDataRequred: // если требуется больше данных для записи, то пропускаем уже записанные элементы и продолжаем попытку записи
                    _skip += wqty; // пропускаем уже записанные элементы, так как они были записаны в буфер, и продолжаем попытку записи
                    break;
                case EdfErr.IsOk:
                    _skip = 0;
                    if (null == _currObj && !flatObj.MoveNext()) // если текущий объект данных null и перечислитель не может перейти к следующему элементу, то запись завершена
                    {
                        return (int)EdfErr.IsOk;
                    }
                    _currObj = flatObj.Current; // обновляем текущий объект данных для следующей итерации
                    break;
                case EdfErr.DstBufOverflow: // если произошла ошибка переполнения буфера, то записываем текущий блок данных и начинаем новый блок
                    Flush();
                    dst = _blkData; // сбрасываем буфер для записи нового блока данных
                    _skip += wqty; // пропускаем уже записанные элементы, так как они были записаны в предыдущем блоке данных
                    err = EdfErr.IsOk;
                    break;
            }
        }
        while (EdfErr.SrcDataRequred != err); // продолжаем попытку записи, пока не будет записано все данные, и не потребуется больше данных для записи
        return err;
    }
    // метод для записи одного элемента данных, принимает схему данных, буфер для записи,
    // перечислитель для разложения объекта на примитивные элементы
    private EdfErr WriteSingleValue(TypeInf inf, ref Span<byte> dst, IEnumerator<object> flatObj, ref int skip, ref int wqty, ref int writed)
    {
        EdfErr err;
        if (EdfErr.IsOk != (err = WriteSep(SepRecBegin, ref dst, ref skip, ref wqty, ref writed)))
            return err;
        if (EdfErr.IsOk != (err = WriteObj(inf, ref dst, flatObj, ref skip, ref wqty, ref writed)))
            return err;
        if (EdfErr.IsOk != (err = WriteSep(SepRecEnd, ref dst, ref skip, ref wqty, ref writed)))
            return err;
        return err;
    }
    // метод для записи объекта данных, принимает схему данных, буфер для записи,
    private EdfErr WriteObj(TypeInf inf, ref Span<byte> dst, IEnumerator<object> flatObj, ref int skip, ref int wqty, ref int writed)
    {
        EdfErr err = EdfErr.IsOk;
        uint totalElement = inf.GetTotalElements();

        if (PoType.Char == inf.Type)
        {
            if (0 < skip)
            {
                skip--;
                return EdfErr.IsOk;
            }
            var charArr = new byte[totalElement];
            for (int i = 0; i < totalElement; ++i)
            {
                if (null == _currObj)
                {
                    if (!flatObj.MoveNext())
                        return EdfErr.WrongType;
                    _currObj = flatObj.Current;
                }
                charArr[i] = (byte)_currObj;
                _currObj = null;
            }
            if (EdfErr.IsOk != (err = TrySrcToX(inf.Type, charArr, dst, out var w)))
            {
                if (EdfErr.DstBufOverflow != err)
                    return err;
                _blkQty += (ushort)writed;
                Flush();
                _blkQty = 0;
                writed = 0;
                dst = _blkData;
                if (EdfErr.IsOk != (err = TrySrcToX(inf.Type, charArr, dst, out w)))
                    return err;
            }
            writed += w;
            wqty++;
            dst = dst.Slice(w);

            if (EdfErr.IsOk != (err = WriteSep(SepVarEnd, ref dst, ref skip, ref wqty, ref writed)))
                return err;
            return err;
        }

        if (1 < totalElement)
            if (EdfErr.IsOk != (err = WriteSep(SepBeginArray, ref dst, ref skip, ref wqty, ref writed)))
                return err;
        for (int i = 0; i < totalElement; i++)
        {
            if (EdfErr.IsOk != (err = WriteObjElement(inf, ref dst, flatObj, ref skip, ref wqty, ref writed)))
                return err;
        }
        if (1 < totalElement)
            if (EdfErr.IsOk != (err = WriteSep(SepEndArray, ref dst, ref skip, ref wqty, ref writed)))
                return err;
        return err;
    }
    // метод для записи элемента данных
    private EdfErr WritePrimitive(TypeInf inf, ref Span<byte> dst, IEnumerator<object> flatObj, ref int skip, ref int wqty, ref int writed)
    {
        EdfErr err = EdfErr.IsOk;
        if (0 < skip)
            skip--;
        else
        {
            if (null == _currObj)
            {
                if (!flatObj.MoveNext())
                    return EdfErr.SrcDataRequred;
                _currObj = flatObj.Current;
            }
            if (EdfErr.IsOk != (err = TrySrcToX(inf.Type, _currObj, dst, out var w)))
            {
                if (EdfErr.DstBufOverflow != err)
                    return err;
                _blkQty += (ushort)writed;
                Flush();
                _blkQty = 0;
                writed = 0;
                dst = _blkData;
                if (EdfErr.IsOk != (err = TrySrcToX(inf.Type, _currObj, dst, out w)))
                    return err;
            }
            _currObj = null;
            writed += w;
            wqty++;
            dst = dst.Slice(w);
        }
        return err;
    }
    private EdfErr WriteObjElement(TypeInf inf, ref Span<byte> dst, IEnumerator<object> flatObj, ref int skip, ref int wqty, ref int writed)
    {
        EdfErr err = EdfErr.IsOk;
        if (PoType.Struct == inf.Type)
        {
            if (inf.Childs != null && 0 != inf.Childs.Length)
            {
                if (EdfErr.IsOk != (err = WriteSep(SepBeginStruct, ref dst, ref skip, ref wqty, ref writed)))
                    return err;
                for (int childIndex = 0; childIndex < inf.Childs.Length; childIndex++)
                {
                    // рекурсивно запи сываем каждый элемент структуры, используя схему данных для каждого элемента
                    err = WriteObj(inf.Childs[childIndex], ref dst, flatObj, ref skip, ref wqty, ref writed);
                    if (EdfErr.IsOk != err)
                        return err;
                }
                if (EdfErr.IsOk != (err = WriteSep(SepEndStruct, ref dst, ref skip, ref wqty, ref writed)))
                    return err;
            }
        }
        else
        {
            if (EdfErr.IsOk != (err = WritePrimitive(inf, ref dst, flatObj, ref skip, ref wqty, ref writed)))
                return err;
            if (EdfErr.IsOk != (err = WriteSep(SepVarEnd, ref dst, ref skip, ref wqty, ref writed)))
                return err;
        }
        return err;
    }
}
// расширение для класса BaseWriter, добавляет метод для записи данных с указанием схемы данных и имени элемента данных
public static class BaseWriterExt
{
    // метод для записи данных, принимает идентификатор элемента данных, тип данных, имя элемента данных и объект данных для записи
    public static EdfErr WriteInfData(this BaseWriter dw, UInt32 id, PoType pt, string name, object d)
    {
        dw.Write(new TypeRec() { Id = id, Inf = new(pt), Name = name, });
        ArgumentNullException.ThrowIfNull(dw.CurrDataType);
        return dw.Write(d);
    }
}


