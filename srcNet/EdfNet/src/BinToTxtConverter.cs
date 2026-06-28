using NetEdf.Base;

namespace NetEdf.src;


public class BinToTxtConverter : BaseDisposable
{
    readonly Stream _srcFile; // поток для чтения бинарного файла
    readonly Stream _dstFile; // поток для записи текстового файла
    readonly BinReader _reader; // объект для чтения данных из бинарного файла, который использует поток _srcFile
    readonly TxtWriter _writer; // объект для записи данных в текстовый файл, который использует поток _dstFile

    // конструктор, который принимает пути к исходному бинарному файлу и целевому текстовому файлу,
    // открывает эти файлы и инициализирует объекты для чтения и записи
    public BinToTxtConverter(string srcBin, string dstTxt) 
    {
        _srcFile = new FileStream(srcBin, FileMode.Open); 
        _dstFile = new FileStream(dstTxt, FileMode.Create);
        _reader = new BinReader(_srcFile);
        _writer = new TxtWriter(_dstFile);
    }
    // метод для освобождкения ресурсов
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _reader.Dispose();
            _writer.Dispose();
            _srcFile.Dispose();
            _dstFile.Dispose();
        }
    }
    public void Execute() // основной метод для выполнения конвертации
    {
        try
        {
            while (_reader.ReadBlock()) // цикл для чтения блоков
            {
                switch (_reader.GetBlockType()) // определение типа блока
                {
                    default: break;
                    case BlockType.Header: // блок заголовка 
                        var header = _reader.ReadHeader(); // чтение заголовка
                        if (header != null) // если заголовок не пустой
                            _writer.Write(header); // запись заголовка в текст
                        break;
                    case BlockType.VarInfo: // блок схема
                        var rec = _reader.ReadInfo(); //чтение схемы
                        if (rec != null) // схема не пуста
                            _writer.Write(rec); // запись схемы в текст
                        break;
                    case BlockType.VarData: // блок данных
                        EdfErr err = TryReadPrimitives(out var arr, _reader.GetBlockData()); // читаем примитивы из блока
                        if (0 < arr.Count) // если в масстве есть данные
                        {
                            _writer.Write(arr); // записываем
                            _writer.Flush(); // сброс буфера
                        }
                        break;
                }
            }
        }
        catch (EndOfStreamException ex) // исключение если достигнут конец потока
        {

        }
        _writer.Flush(); // сброс буфера после завершения чтения всех блоков
    }

    int _skip = 0;
    int _readed = 0;
    // метод для чтения примитивов из блока данных,
    // который использует объект PrimitiveListReader для последовательного чтения примитивов,
    EdfErr TryReadPrimitives(out List<object> ret, ReadOnlySpan<byte> src)
    {
        ret = []; // список для хранения прочитанных примитивов
        ArgumentNullException.ThrowIfNull(_writer.CurrDataType); //текущий тип данных не null
        EdfErr err;
        do
        {
            int qty = 0; //количество примитивов для чтения
            int skip = _skip; // количество примитивов для пропуска
            int readed = 0; // количество уже прочитанных примитивов
            //метод чтения примитивов из блока данных
            err = PrimitiveListReader.ReadObjects(_writer.CurrDataType, src, ref skip, ref qty, ref readed, ret);
            src = src.Slice(readed); // сдвигаем исходные данные на количество прочитанных байт
            switch (err)
            {
                default:
                case EdfErr.WrongType: return err; //тип не соответствует возвращаем ошибку
                case EdfErr.DstBufOverflow: return err; //буфер назначения переполнен возвращаем ошибку
                case EdfErr.SrcDataRequred: // данные не дочитаны
                    _skip += qty; // увеличиваем количество примитивов для пропуска
                    _readed = 0; // сбрасываем количество прочитанных примитивов
                    break;
                case EdfErr.IsOk: // данные успешно прочитаны
                    _readed += readed; // увеличиваем количество прочитанных примитивов
                    _skip = 0; // сбрасываем количество примитивов для пропуска
                    break;
            }
        }
        while (err != EdfErr.SrcDataRequred);
        return err;
    }
}
