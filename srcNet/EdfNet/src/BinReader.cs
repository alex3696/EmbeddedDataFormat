namespace NetEdf.src;

// предназначен для чтения данных из бинарного формата,
// который может содержать заголовок, информацию о типах данных и сами данные в виде блоков.
public class BinReader : BaseReader
{
    public readonly Header Cfg; // Заголовок
    readonly BinaryReader _br; // Поток для чтения данных
    private readonly BinBlock _current; // Текущий блок данных
    public UInt16 EqQty; // Количество байт, прочитанных в текущем блоке
    public byte Seq; // Номер текущего блока

    public UInt16 Pos; // ??
    protected TypeInf? _currDataType; // Информация о типе данных, который мы ожидаем прочитать из текущего блока.

    // Конструктор, принимает поток для чтения и необязательный заголовок. Если заголовок не передан, используется заголовок по умолчанию.
    public BinReader(Stream stream, Header? header = default)
    {
        _br = new BinaryReader(stream); // Инициализируем BinaryReader для чтения данных из потока
        Cfg = Header.Default; 
        _current = new BinBlock(0, new byte[Cfg.Blocksize], 0); // Инициализируем текущий блок данных с нулевым типом,
                                                                // выделенным буфером и нулевой длиной
        if (ReadBlock()) // Пытаемся прочитать первый блок данных, если он существует
        {
            var newCfg = ReadHeader(); //
            if (newCfg != null)
                _current = new BinBlock(0, new byte[Cfg.Blocksize], 0); // Если первый блок данных является заголовком,
                                                                        // то мы обновляем текущий блок данных с новым заголовком
        }
    }
    // Метод для чтения блока данных из потока
    public bool ReadBlock()
    {
        BlockType t; // Переменная для хранения типа блока данных
        do
        {
            t = (BlockType)_br.ReadByte(); // Пытаемся получить тип блока
        }
        while (!Enum.IsDefined(t));

        var seq = _br.ReadByte(); // Читаем следдующий байт последовательности
        var len = _br.ReadUInt16(); // Читаем следующие два байта длины данных в блоке 

        if (0 < len) // Если длина данных больше нуля, то мы можем прочитать данные из блока
        {
            _current.Type = t; // Устанавливаем тип текущего блока данных
            _current.Seq = seq; // Устанавливаем номер текущего блока данных
            _current.Qty = len; // Устанавливаем количество байт данных в текущем блоке
            _br.Read(_current._data, 0, len); // Читаем данные из потока и сохраняем их в буфер текущего блока данных

            if (Cfg.Flags.HasFlag(Options.UseCrc)) 
            {
                ushort fileCrc = _br.ReadUInt16(); // Читаем следующие два байта CRC16
                ushort crc = ModbusCRC.Calc([(byte)_current.Type]); // Вычисляем CRC контрольную сумму для типа блока данных
                crc = ModbusCRC.Calc([_current.Seq], crc); // Обновляем контрольную сумму для номера блока данных
                crc = ModbusCRC.Calc(BitConverter.GetBytes(_current.Qty), crc); // Обновляем контрольную сумму для длины данных
                crc = ModbusCRC.Calc(_current.Data, crc); // Обновляем контрольную сумму для данных в блоке
                if (crc != fileCrc) // Если вычисленная контрольная сумма не совпадает с контрольной суммой, прочитанной из потока,
                    throw new Exception($"Wrong CRC block {_current.Seq}"); // выбрасываем исключение
            }
            if (_current.Type != BlockType.VarData) // Если тип текущего блока данных не является VarData, то мы пытаемся прочитать информацию о типе данных из блока
                _currDataType = ReadInfo()?.Inf; 
            Pos = 0; //??
            return true; // Успешно прочитали блок данных, возвращаем true
        }

        return false; 
    }
    public BlockType GetBlockType() => _current.Type; // Получаем тип текущего блока данных
    public byte GetBlockSeq() => _current.Seq; // Получаем номер текущего блока данных (последовательность)
    public ushort GetBlockLen() => _current.Qty; // Получаем количество байт данных в текущем блоке данных
    public Span<byte> GetBlockData() => _current._data.AsSpan(0, _current.Qty); // Получаем данные текущего блока данных в виде Span<byte>,
                                                                                // ограниченного длиной данных в блоке

    // Метод для чтения заголовка
    public Header? ReadHeader()
    {
        if (_current.Type == BlockType.Header) //Если тип блока это заголовок 
            return Header.Parse(_current.Data); // Парсим данные блока данных как заголовок и возвращаем результат
        return null;
    }

    // Метод для чтения информации о типе данных из блока данных
    public TypeRec? ReadInfo()
    {
        if (_current.Type == BlockType.VarInfo)
        {
            TypeRec rec = new(); 
            EdfErr err; 
            if (EdfErr.IsOk != (err = Primitives.TryBinToSrc(PoType.UInt32, _current._data, out var r, out var retObj))) 
                return null;
            rec.Id = (uint)(retObj ?? 0); // Читаем идентификатор типа данных из блока данных и сохраняем его в rec.Id.
                                          // Если retObj равен null, то используем значение 0 
            rec.Inf = ParseInf(_current._data.AsSpan(r), out var rest); // Читаем информацию о типе данных из блока данных, начиная с позиции r,
                                                                        // и сохраняем ее в rec.Inf. Остаток данных сохраняем в переменной rest

            if (EdfErr.IsOk != (err = Primitives.TryBinToSrc(PoType.String, rest, out r, out retObj)))
                return null;
            rec.Name = (string?)retObj; // Читаем имя типа данных из блока данных, начиная с позиции r в переменной rest, и сохраняем его в rec.Name.
                                        // Если retObj равен null, то rec.Name будет null
            rest = rest.Slice(r); // Обновляем переменную rest, чтобы она указывала на оставшуюся часть данных после чтения имени типа данных. 
                                  // Теперь rest указывает на массив байтов, начиная с позиции после прочитанных байтов для имени типа данных.
            if (EdfErr.IsOk != (err = Primitives.TryBinToSrc(PoType.String, rest, out r, out retObj)))
                return null;
            rec.Desc = (string?)retObj; // Читаем описание типа данных из блока данных,
                                        // начиная с позиции r в переменной rest, и сохраняем его в rec.Desc.

            return rec;
        }
        return null;
    }

    // Методы для чтения данных из блока данных
    public static EdfErr ReadObject(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        uint totalElement = t.GetTotalElements(); // Получаем общее количество элементов для типа данных t
        if (1 < totalElement)
            return ReadArray(t, src, totalElement, ref skip, ref qty, ref readed, ref ret); // Пытаемя прочитать массив
        return ReadElement(t, src, ref skip, ref qty, ref readed, ref ret); // Пытаемя прочитать одиночный элемент
    }
    // Метод для чтения одиночного элемента данных из блока данных
    public static EdfErr ReadElement(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        if (PoType.Struct == t.Type) 
            return ReadStruct(t, src, ref skip, ref qty, ref readed, ref ret); // пытаемя прочитать структуру
        return ReadPrimitive(t, src, ref skip, ref qty, ref readed, ref ret); // пытаемя прочитать примитивный тип данных
    }
    // Метод для чтения массива данных из блока данных
    static EdfErr ReadArray(TypeInf t, ReadOnlySpan<byte> src, uint totalElement, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        EdfErr err = EdfErr.IsOk;
        Type csType = ret.GetType(); // Получаем тип данных в C#, который соответствует объекту ret,
                                     // в который мы хотим сохранить прочитанные данные
        if (!csType.IsArray)
            throw new ArrayTypeMismatchException();
        var elementType = csType.GetElementType(); // Получаем тип элементов массива в C#, который соответствует типу данных
        ArgumentNullException.ThrowIfNull(elementType);
        var arr = ret as Array; // Приводим объект ret к типу Array, чтобы мы могли работать с ним как с массивом в C#
        ArgumentNullException.ThrowIfNull(arr);
        for (int i = 0; i < totalElement; i++)
        {
            var r = readed; // Сохраняем текущее количество прочитанных байт в переменной r,
                            // чтобы мы могли вычислить, сколько байт было прочитано после попытки чтения элемента данных
            if (EdfErr.IsOk != (err = ReadElement(t, src, elementType, ref skip, ref qty, ref readed, out var arrItem)))
                return err;
            if (0 < readed) 
            {
                arr.SetValue(arrItem, i); // Если были прочитаны байты данных, то мы сохраняем прочитанное значение в массиве ret на позиции i 
                src = src.Slice(readed - r); // Обновляем переменную src, чтобы она указывала на оставшуюся часть данных после чтения элемента данных
            }
        }
        return err;
    }
    // Метод для чтения структуры данных из блока данных
    static EdfErr ReadStruct(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        EdfErr err = EdfErr.IsOk;
        if (null == t.Childs || 0 == t.Childs.Length)
            return EdfErr.IsOk;
        Type csType = ret.GetType(); // Получаем тип данных в C#, который соответствует объекту ret
        var fields = csType.GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? []; // Получаем список публичных свойств экземпляра типа данных в C#
                                                                                              // для доступа к полям структуры, которую мы хотим прочитать из блока данных
        int fieldId = 0; // Инициализируем счетчик для отслеживания текущего поля, которое мы читаем из структуры.
                         // Этот счетчик будет использоваться для доступа к соответствующему полю в списке fields
        foreach (var child in t.Childs)
        {
            var r = readed; // Сохраняем текущее количество прочитанных байт в переменной r,
                            // чтобы мы могли вычислить, сколько байт было прочитано после попытки чтения поля структуры
            var field = fields[fieldId++]; // Получаем текущее поле из списка fields, используя счетчик fieldId,
            if (EdfErr.IsOk != (err = ReadObject(child, src, field.PropertyType, ref skip, ref qty, ref readed, out var childVal)))
                return err;
            field.SetValue(ret, childVal); // Если были прочитаны байты данных для текущего поля структуры,
                                           // то мы сохраняем прочитанное значение в соответствующем
                                           // свойстве экземпляра типа данных в C#, используя метод SetValue
            src = src.Slice(readed - r); // Обновляем переменную src, чтобы она указывала на оставшуюся часть данных после чтения поля структуры. 
                                         // Мы вычисляем, сколько байт было прочитано для текущего поля, вычитая значение r (количество байт до чтения)
                                         // из текущего значения readed (количество байт после чтения).
        }
        return err;
    }
    // Метод для чтения примитивного типа данных из блока данных
    static EdfErr ReadPrimitive(TypeInf t, ReadOnlySpan<byte> src, ref int skip, ref int qty, ref int readed, ref object ret)
    {
        if (0 < skip)
        {
            skip--; // уменьшаем skip на 1 и возвращая EdfErr.IsOk, чтобы указать, что чтение прошло успешно, но элемент данных был пропущен
            return EdfErr.IsOk;
        }
        EdfErr err = EdfErr.IsOk;
        if (0 != (err = Primitives.TryBinToSrc(t.Type, src, out var r, out ret)))
            return err;
        readed += r; // Увеличиваем количество прочитанных байт на r, чтобы отразить
                     // количество байт, которые были прочитаны для текущего примитивного типа данных
        qty++; // Увеличиваем количество прочитанных элементов на 1, чтобы отразить, что мы успешно прочитали один элемент данных
        return err;
    }
    // Метод для чтения данных из блока данных, с указанием типа данных в C#
    public static EdfErr ReadObject(TypeInf t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        uint totalElement = t.GetTotalElements(); 
        if (1 < totalElement)
            return ReadArray(t, src, csType, totalElement, ref skip, ref qty, ref readed, out ret); // Пытаемя прочитать массив
        return ReadElement(t, src, csType, ref skip, ref qty, ref readed, out ret); // Пытаемя прочитать одиночный элемент
    }
    // Метод для чтения одиночного элемента данных из блока данных, с указанием типа данных в C#, в который мы хотим сохранить прочитанные данные
    public static EdfErr ReadElement(TypeInf t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        if (PoType.Struct == t.Type)
            return ReadStruct(t, src, csType, ref skip, ref qty, ref readed, out ret); // пытаемя прочитать структуру, с указанием типа данных в C#, 
        return ReadPrimitive(t, src, csType, ref skip, ref qty, ref readed, out ret); // пытаемя прочитать примитивный тип данных, с указанием типа данных в C#
    }
    // Метод для чтения массива данных из блока данных, с указанием типа данных в C#
    static EdfErr ReadArray(TypeInf t, ReadOnlySpan<byte> src, Type csType, uint totalElement, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        EdfErr err = EdfErr.IsOk;
        if (!csType.IsArray) 
            throw new ArrayTypeMismatchException();
        var elementType = csType.GetElementType(); // Получаем тип элементов массива в C#,
        ArgumentNullException.ThrowIfNull(elementType);
        var arr = Array.CreateInstance(elementType, totalElement); // Создаем новый массив в C#, который соответствует типу элементов и количеству элементов, указанному в totalElement.
        ret = arr; // Сохраняем созданный массив в переменной ret, которая будет использоваться для возврата результата чтения данных из блока данных
        for (int i = 0; i < totalElement; i++)
        {
            var r = readed; // Сохраняем текущее количество прочитанных байт в переменной r,
                            // чтобы мы могли вычислить, сколько байт было прочитано после попытки чтения элемента данных
            if (EdfErr.IsOk != (err = ReadElement(t, src, elementType, ref skip, ref qty, ref readed, out var arrItem)))
                return err;
            arr.SetValue(arrItem, i); // Если были прочитаны байты данных, то мы сохраняем прочитанное значение в массиве ret на позиции i
            src = src.Slice(readed - r); // Обновляем переменную src, чтобы она указывала на оставшуюся часть данных после чтения элемента данных
        }
        return err;
    }
    // Метод для чтения структуры данных из блока данных, с указанием типа данных в C#,
    static EdfErr ReadStruct(TypeInf t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        EdfErr err = EdfErr.IsOk;
        ret = default;
        if (null == t.Childs || 0 == t.Childs.Length)
            return EdfErr.IsOk;
        ret = Activator.CreateInstance(csType); // Создаем новый экземпляр типа данных в C#, который соответствует типу структуры,
                                                // которую мы хотим прочитать из блока данных
        var fields = csType.GetProperties(BindingFlags.Public | BindingFlags.Instance) ?? []; // Получаем список свойств структуры
        int fieldId = 0; // Инициализируем счетчик для отслеживания текущего поля, которое мы читаем из структуры.
                         // Этот счетчик будет использоваться для доступа к соответствующему полю в списке fields
        foreach (var child in t.Childs) 
        {
            var r = readed; // Сохраняем текущее количество прочитанных байт в переменной r,
                            // чтобы мы могли вычислить, сколько байт было прочитано после попытки чтения поля структуры
            var field = fields[fieldId++]; // Получаем текущее поле из списка fields, используя счетчик fieldId,
            if (EdfErr.IsOk != (err = ReadObject(child, src, field.PropertyType, ref skip, ref qty, ref readed, out var childVal)))
                return err;
            field.SetValue(ret, childVal); // Если были прочитаны байты данных для текущего поля структуры,
                                           // то мы сохраняем прочитанное значение в соответствующем
                                           // свойстве экземпляра типа данных в C#, используя метод SetValue
            src = src.Slice(readed - r); // Обновляем переменную src, чтобы она указывала на оставшуюся часть данных после чтения поля структуры. 
                                         // Мы вычисляем, сколько байт было прочитано для текущего поля, вычитая значение r (количество байт до чтения)
                                         // из текущего значения readed (количество байт после чтения).
        }
        return err;
    }
    // Метод для чтения примитивного типа данных из блока данных, с указанием типа данных в C#
    static EdfErr ReadPrimitive(TypeInf t, ReadOnlySpan<byte> src, Type csType, ref int skip, ref int qty, ref int readed, out object? ret)
    {
        if (0 < skip)
        {
            skip--; // уменьшаем skip на 1 и возвращая EdfErr.IsOk, чтобы указать, что чтение прошло успешно, но элемент данных был пропущен
            ret = null; // Устанавливаем ret в null
            return EdfErr.IsOk;
        }
        EdfErr err = EdfErr.IsOk;
        if (0 != (err = Primitives.TryBinToSrc(t.Type, src, out var r, out ret)))
            return err;
        readed += r; // Увеличиваем количество прочитанных байт на r, чтобы отразить
                     // количество байт, которые были прочитаны для текущего примитивного типа данных
        qty++; // Увеличиваем количество прочитанных элементов на 1, чтобы отразить, что мы успешно прочитали один элемент данных
        return err;
    }

    int _skip = 0; // Количество элементов данных, которые нужно пропустить перед чтением следующего элемента данных
    int _readed = 0; // Количество байт данных, которые уже были прочитаны из текущего блока данных
    object? _ret; // Промежуточное значение, которое может быть использовано для хранения прочитанных данных из блока данных

    // Метод для чтения данных из блока данных, с указанием типа данных в C#, и сохранением результата в переменной ret, которая может быть null
    public EdfErr TryRead<T>(out T? ret)
    {
        ArgumentNullException.ThrowIfNull(_currDataType); // Проверяем, что информация о типе данных для текущего блока данных
        EdfErr err;
        ret = default;
        Span<byte> src = _current._data.AsSpan(_readed, _current.Qty - _readed); // Получаем оставшуюся часть данных в текущем блоке данных,
                                                                                 // начиная с позиции _readed,
                                                                                 // которая указывает на количество байт, уже прочитанных из блока данных.
        do
        {
            int qty = 0; // количество прочитанных элементов данных
            int skip = _skip; // количество элементов которое нужно пропустить перед чтением
            int readed = 0; // количество прочитанных байт данных в текущей попытке чтения

            if (null != _ret) //?? 
                err = ReadObject(_currDataType, src, ref skip, ref qty, ref readed, ref _ret); // Пытаемя прочитать данные из блока данных
            else
                err = ReadObject(_currDataType, src, typeof(T), ref skip, ref qty, ref readed, out _ret); // Пытаемя прочитать данные из блока данных, с указанием типа данных в C#,
            src = src.Slice(readed); // обновляем src сдвигая на количество байт, которые были прочитаны в текущей попытке чтения
            switch (err)
            {
                default:
                case EdfErr.WrongType: return err;
                case EdfErr.DstBufOverflow: return err;
                case EdfErr.SrcDataRequred:
                    _skip += qty; // Увеличиваем _skip на количество прочитанных элементов данных,
                    _readed = 0; // Сбрасываем _readed в 0
                    break;
                case EdfErr.IsOk:
                    ret = (T?)Convert.ChangeType(_ret, typeof(T)); // Если чтение прошло успешно,
                                                                   // то мы сохраняем прочитанное значение в переменной ret,
                                                                   // используя Convert.ChangeType для преобразования типа данных к типу T
                    _readed += readed; // Увеличиваем _readed на количество байт, которые были прочитаны в текущей попытке чтения,
                    _skip = 0; // Сбрасываем _skip в 0, так как мы успешно прочитали элемент данных и больше не нужно пропускать элементы
                    _ret = null; // Сбрасываем _ret в null, так как мы успешно прочитали элемент данных и больше не нужно сохранять промежуточное значение
                    return err;
            }
        }
        while (err != EdfErr.SrcDataRequred);
        return err;
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing)
            return;
    }

    // Метод для парсинга информации о типе данных из блока данных
    static TypeInf ParseInf(ReadOnlySpan<byte> b, out ReadOnlySpan<byte> rest)
    {
        rest = b; // В переменной rest отслеживаются оставшииеся байты после чтения информации о типе данных
        if (2 > rest.Length) // Проверяем, что в массиве байтов достаточно данных для чтения информации о типе данных.
            throw new ArgumentException($"array is too small {b.Length}");
        if (!Enum.IsDefined(typeof(PoType), b[0])) // Проверяем, что первый байт в массиве байтов соответствует определенному типу данных (PoType). 
            throw new ArgumentException("type mismatch");
        // type
        var type = (PoType)b[0]; // Читаем первый байт из массива байтов и интерпретируем его как тип данных (PoType).
        rest = rest.Slice(1); // Обновляем переменную rest, чтобы она указывала на оставшуюся часть массива байтов после чтения типа данных.
        // dim
        var dimsCount = rest[0]; // Читаем следующий байт, который представляет собой количество измерений (dimsCount) для типа данных.
        rest = rest.Slice(1); // Обновляем переменную rest, чтобы она указывала на оставшуюся часть массива байтов после чтения количества измерений.
        uint[]? dims = null; // Инициализируем массив dims, который будет использоваться для хранения размеров измерений. Изначально он равен null.
        if (0 < dimsCount) 
        {
            dims = new uint[dimsCount]; // указываем размер массива, который соответствует количеству измерений (dimsCount)
            for (int i = 0; i < dimsCount; i++)
            {
                dims[i] = BinaryPrimitives.ReadUInt32LittleEndian(rest); //Читаем 4 байта из массива байтов rest которые представляют размер текущего измерения
                rest = rest.Slice(sizeof(UInt32)); // Обновляем переменную rest, чтобы она указывала на оставшуюся часть массива байтов после чтения размера текущего измерения. 
            }
        }
        // name
        byte bNameSize = rest[0]; // Читаем следующий байт из массива байтов rest, который представляет собой размер имени (bNameSize)
        rest = rest.Slice(1); // Обновляем переменную rest, чтобы она указывала на оставшуюся часть массива байтов после чтения размера имени. 
        if (255 < bNameSize)
            throw new ArgumentException("name len mismatch");
        var name = Encoding.UTF8.GetString(rest.Slice(0, bNameSize)); //Преобразуем массив байтов в строку с 0 индекса по bNameSize
        rest = rest.Slice(bNameSize); // Обновляем переменную rest, чтобы она указывала на оставшуюся часть массива байтов после чтения имени. 
        // childs
        List<TypeInf>? childs = null; 
        if (PoType.Struct == type && 0 < rest.Length)
        {
            byte childsCount = rest[0]; // Читаем следующий байт, который представляет собой количество дочерних элементов (childsCount)
            rest = rest.Slice(1); // Обновляем переменную rest, чтобы она указывала на оставшуюся часть массива байтов после чтения количества дочерних элементов. 
            childs = new List<TypeInf>(childsCount); // Выделяем список для хранения информации о дочерних элементах,
            for (int i = 0; i < childsCount; i++)
                childs.Add(ParseInf(rest, out rest)); // Для каждого дочернего элемента, вызываем рекурсивно функцию ParseInf,
        }
        return new TypeInf(name, type, dims, childs?.ToArray()); // Возвращаем новый объект TypeInf
    }
}
