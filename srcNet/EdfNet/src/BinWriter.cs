namespace NetEdf.src;

public class BinWriter : BaseWriter
{
    public ushort CurrentQty => _blkQty; // количество байт в текущем блоке
    private readonly Stream _bw; // поток для записи
    protected byte _blkSeq; // номер текущего блока

    // метод для преобразования данных в бинарный формат
    protected override EdfErr TrySrcToX(PoType t, object obj, Span<byte> dst, out int w) 
        => Primitives.TrySrcToBin(t, obj, dst, out w); 
    protected override EdfErr WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int skip, ref int wqty, ref int writed)
        => EdfErr.IsOk;

    // конструктор, принимает поток для записи и необязательную конфигурацию заголовка
    public BinWriter(Stream stream, Header? cfg = default)
        : base(cfg ?? Header.Default)
    {
        _bw = stream;
        if (0 == stream.Position) // если поток пустой, то записываем заголовок
            Write(Cfg);
    }
    // освобождение ресурсов, при этом записываем все оставшиеся данные в блоке и очищаем буфер
    protected override void Dispose(bool disposing)
    {
        Flush(); // записываем все оставшиеся данные в блоке
        _bw.Flush(); // очищаем буфер потока
        base.Dispose(disposing); 
    }

    // метод для записи блока данных, принимает данные и тип блока,
    // формирует заголовок блока, вычисляет контрольную сумму и записывает все в поток
    private void WriteBlock(ReadOnlySpan<byte> data, BlockType blkType)
    {
        var blkQty = (ushort)data.Length; 
        _bw.WriteByte((byte)blkType); // записываем тип блока
        _bw.WriteByte(_blkSeq); // записываем номер блока
        _bw.Write(BitConverter.GetBytes(blkQty)); // записываем количество байт в блоке
        _bw.Write(data); // записываем данные блока
        ushort crc = ModbusCRC.Calc([(byte)blkType]); // вычисляем контрольную сумму
        crc = ModbusCRC.Calc([_blkSeq], crc); // обновляем контрольную сумму с учетом номера блока
        crc = ModbusCRC.Calc(BitConverter.GetBytes(blkQty), crc); // обновляем контрольную сумму с учетом количества байт
        crc = ModbusCRC.Calc(data, crc); // обновляем контрольную сумму с учетом данных блока
        _bw.Write(BitConverter.GetBytes(crc)); // записываем контрольную сумму
        _blkSeq++; // увеличиваем номер блока для следующей записи
        _blkQty = 0; // сбрасываем количество байт в текущем блоке
    }
    
    public override void Flush()
    {
        // если текущий блок данных не пустой, то записываем его в поток
        if (null == _currDataType || 0 == _blkQty)
            return;
        WriteBlock(_blkData.AsSpan(0, _blkQty), BlockType.VarData); // записываем блок данных в поток
    }
    //запись заголовка.
    public override void Write(Header h)
    {
        Flush(); // записываем данные перед записью заголовка
        _currDataType = null; // сбрасываем текущий тип данных, так как мы записываем новый заголовок
        var dst = _blkData.AsSpan(0, 16); // выделяем 16 байт для заголовка
        dst.Clear(); // очищаем выделенный буфер
        dst.SrcToBinRef(PoType.UInt8, h.VersMajor); 
        dst.SrcToBinRef(PoType.UInt8, h.VersMinor);
        dst.SrcToBinRef(PoType.UInt16, h.Encoding);
        dst.SrcToBinRef(PoType.UInt16, h.Blocksize);
        dst.SrcToBinRef(PoType.UInt32, h.Flags);
        WriteBlock(_blkData.AsSpan(0, 16), BlockType.Header); // записываем заголовок в поток
    }

    // запись описания типа данных
    public override void Write(TypeRec t)
    {
        Flush(); // записываем данные перед записью описания типа данных
        var dst = _blkData.AsSpan(); // выделяем весь буфер для описания типа данных
        dst.SrcToBinRef(PoType.UInt32, t.Id); // записываем идентификатор типа данных
        Write(ref dst, t.Inf); // записываем описание типа данных
        dst.SrcToBinRef(PoType.String, t.Name ?? string.Empty); 
        dst.SrcToBinRef(PoType.String, t.Desc ?? string.Empty);
        _currDataType = t.Inf;
        WriteBlock(_blkData.AsSpan(0, _blkData.Length - dst.Length), BlockType.VarInfo);
    }
    // запись данных, принимает данные и их описание, формирует блок данных и записывает его в поток
    private static long Write(ref Span<byte> dst, TypeInf inf)
    {
        var begin = dst.Length; // сохраняем начальную позицию в буфере для вычисления количества записанных байт
        dst.SrcToBinRef(PoType.UInt8, inf.Type); // записываем тип данных
        if (null != inf.Dims && 0 < inf.Dims.Length) // если есть измерения, то записываем их
        {
            dst.SrcToBinRef(PoType.UInt8, (byte)inf.Dims.Length); // записываем количество измерений
            for (int i = 0; i < inf.Dims.Length; i++) // записываем каждое измерение
                dst.SrcToBinRef(PoType.UInt32, inf.Dims[i]); // записываем размер измерения
        }
        else // если измерений нет, то записываем ноль
        {
            dst.SrcToBinRef(PoType.UInt8, (byte)0);
        }
        dst.SrcToBinRef(PoType.String, inf.Name ?? string.Empty);// записываем имя типа данных, если оно есть, иначе записываем пустую строку

        if (PoType.Struct == inf.Type && null != inf.Childs && 0 < inf.Childs.Length) // если тип данных - структура и есть поля, то записываем их
        {
            dst.SrcToBinRef(PoType.UInt8, (byte)inf.Childs.Length); // записываем количество полей в структуре
            for (int i = 0; i < inf.Childs.Length; i++)
            {
                Write(ref dst, inf.Childs[i]); //записываем описание каждого поля в структуре
            }
        }
        return begin - dst.Length; // возвращаем количество записанных байт, вычисляя разницу между начальной и текущей позицией в буфере
    }
}
