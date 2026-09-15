## .NET API Reference

### Writer API: EdfBinaryWriter

**Класс:** `public class EdfBinaryWriter : BaseDisposable, IWriter`

**Конструктор:**
```csharp
public EdfBinaryWriter(Stream stream, EdfConfig? cfg = default)
```
- `stream` — выходной поток (файл или память)
- `cfg` — конфигурация (размер блока, версия и т.д.). По умолчанию используется `EdfConfig.Default`
- Если `stream.Position == 0`, конфиг автоматически пишется в начало

**Основные методы:**

| Метод | Описание |
|-------|---------|
| `void WriteConfig(EdfConfig cfg)` | Записывает блок конфигурации |
| `void WriteSchema(EdfSchema sch)` | Записывает блок схемы. **После этого контекст готов к записи данных.** |
| `EdfErrorCode WriteValue<T>(in T val)` | Записывает одно значение типа T. Возвращает код ошибки. |
| `EdfErrorCode WriteInfData<T>(ushort id, EdfPrimitiveType pt, string name, T d)` | Shortcut: пишет схему + данные за один вызов для примитивного типа |
| `void Flush()` | Закрывает текущий блок данных и записывает его в поток |
| `void Dispose()` | Вызывает `Flush()` и закрывает поток |

**Свойства:**

| Свойство | Тип | Описание |
|----------|-----|---------|
| `Cfg` | `EdfConfig` (чтение) | Конфигурация файла |
| `CurrentSchema` | `EdfSchema?` | Текущая активная схема (установляется после `WriteSchema`) |
| `CurrentDataLen` | `ushort` | Размер текущего блока данных в байтах |

**Пример:**
```csharp
using (var stream = File.Create("output.bdf"))
using (var writer = new EdfBinaryWriter(stream))
{
    var schema = new EdfSchema { Id = 0, Name = "MySchema", Type = ... };
    writer.WriteSchema(schema);
    
    var data = new MyData { ... };
    EdfErrorCode result = writer.WriteValue(data);
    if (result != EdfErrorCode.IsOk)
        throw new Exception($"Write failed: {result}");
}  // Flush() вызывается автоматически при Dispose()
```

---

### Reader API: EdfBinaryReader

**Класс:** `public class EdfBinaryReader : BaseDisposable`

**Конструктор:**
```csharp
public EdfBinaryReader(Stream stream, EdfConfig? cfg = default)
```
- `stream` — входной поток
- `cfg` — опциональная конфигурация по умолчанию
- Автоматически читает первый Config блок и обновляет `Cfg`

**Основные методы:**

| Метод | Описание |
|-------|---------|
| `bool ReadBlock()` | Читает следующий блок. Возвращает `true` если успешно, `false` если EOF. Выбрасывает исключение при ошибке. |
| `EdfBlockType GetBlockType()` | Возвращает тип текущего блока (Config, Schema или Data) |
| `ushort GetBlockLen()` | Возвращает общую длину текущего блока (включая заголовок и CRC) |
| `ReadOnlySpan<byte> GetBlockData()` | Возвращает данные текущего блока (без заголовка) |
| `EdfConfig? ReadConfig()` | Читает конфиг из текущего блока (если тип Config) |
| `EdfSchema? ReadSchema()` | Читает схему из текущего блока (если тип Schema) и устанавливает `CurrentSchema` |
| `T ReadValue<T>()` | Читает значение типа T из текущего Data блока. **Требует, чтобы перед этим была вызвана `ReadSchema()`** |
| `void Dispose()` | Освобождает внутренние буферы |

**Свойства:**

| Свойство | Тип | Описание |
|----------|-----|---------|
| `Cfg` | `EdfConfig` | Конфигурация (читается из первого блока) |
| `CurrentSchema` | `EdfSchema?` | Текущая активная схема (установляется после `ReadSchema()`) |
| `DataAvailable` | `int` | Количество оставшихся байт в текущем Data блоке |

**Пример:**
```csharp
using (var stream = File.OpenRead("output.bdf"))
using (var reader = new EdfBinaryReader(stream))
{
    // Читаем блоки последовательно
    if (!reader.ReadBlock())
        throw new Exception("No config block");
    
    if (!reader.ReadBlock())
        throw new Exception("No schema block");
    reader.ReadSchema();
    
    while (reader.ReadBlock() && reader.GetBlockType() == EdfBlockType.Data)
    {
        MyData data = reader.ReadValue<MyData>();
        Console.WriteLine($"Read: {data}");
    }
}
```

---

### Text Format: EdfTextWriter

**Класс:** `public class EdfTextWriter : BaseDisposable`

**Конструктор:**
```csharp
public EdfTextWriter(Stream stream)
```

**Основные методы:**

| Метод | Описание |
|-------|---------|
| `void WriteSchema(EdfSchema sch)` | Записывает схему в текстовом формате |
| `EdfErrorCode WriteValue<T>(in T val)` | Записывает значение в текстовом формате |
| `void Flush()` | Вспомогательное сбрасывание |

**Примечание:** Текстовый формат идентичен бинарному по структуре данных, но использует ASCII представление вместо CRC.

**Пример:**
```csharp
using (var stream = File.Create("output.tdf"))
using (var writer = new EdfTextWriter(stream))
{
    writer.WriteSchema(schema);
    writer.WriteValue(data);
}
```

---

### Core Interfaces

#### IBufWriter

**Интерфейс:** `public interface IBufWriter`

Используется внутри форматеров для записи примитивных типов.

| Метод | Описание |
|-------|---------|
| `void Write(byte val)` | Запись byte |
| `void Write(int val)` | Запись int32 |
| `void Write(uint val)` | Запись uint32 |
| `void Write(long val)` | Запись int64 |
| `void Write(ulong val)` | Запись uint64 |
| `void Write(float val)` | Запись float |
| `void Write(double val)` | Запись double |
| `void Write(string? val)` | Запись String (с префиксом длины) |
| `void WriteCharArray(ReadOnlySpan<byte> charArray)` | Запись Char массива |
| `void WriteSpan(ReadOnlySpan<byte> src, EdfPrimitiveType pt)` | Запись сырых данных с типом |

**Свойство:**
- `EdfType CurrentType` — текущий тип, который пишется

#### IBufReader

**Интерфейс:** `public interface IBufReader`

Используется внутри форматеров для чтения примитивных типов.

| Метод | Описание |
|-------|---------|
| `byte ReadUInt8()` | Чтение byte |
| `int ReadInt32()` | Чтение int32 |
| `uint ReadUInt32()` | Чтение uint32 |
| `long ReadInt64()` | Чтение int64 |
| `ulong ReadUInt64()` | Чтение uint64 |
| `float ReadSingle()` | Чтение float |
| `double ReadDouble()` | Чтение double |
| `string? ReadString()` | Чтение String |
| `byte[] ReadCharArray()` | Чтение Char массива |
| `void ReadToSpan(Span<byte> dst, out EdfPrimitiveType pt, out int len)` | Чтение сырых данных |

**Свойство:**
- `EdfType CurrentType` — текущий тип, который читается

#### IFormatter\<T\>

**Интерфейс:** `public interface IFormatter<T>`

Должен быть реализован для каждого типа, который вы хотите сериализовать/десериализовать.

```csharp
public interface IFormatter<T>
{
    void Serialize<TWriter>(ref TWriter writer, in T value, EdfFormatterOptions options)
        where TWriter : struct, IBufWriter;
    
    T Deserialize<TReader>(ref TReader reader, EdfFormatterOptions options)
        where TReader : struct, IBufReader;
}
```

---

### Configuration and Exceptions

#### EdfConfig

**Класс:** `public class EdfConfig : IEquatable<EdfConfig>`

**Конструкторы и фабрики:**
```csharp
public EdfConfig()  // Конструктор по умолчанию
public EdfConfig(ushort blocksize)  // С указанным размером блока
public static readonly EdfConfig Default  // Предустановка
```

**Свойства:**

| Свойство | Тип | Описание |
|----------|-----|---------|
| `VersMajor` | `byte` | Главная версия (текущая: 0) |
| `VersMinor` | `byte` | Минорная версия (текущая: 3) |
| `Encoding` | `ushort` | Кодировка (всегда 65001 = UTF-8) |
| `BlockSize` | `ushort` | Размер блока (256–4096) |
| `Flags` | `EdfConfigOptions` | Резервная для будущего использования |

#### EdfException Hierarchy

| Исключение | Базовый класс | Описание |
|-----------|---------------|---------|
| `EdfException` | `Exception` | Базовое исключение всех EDF ошибок |
| `EdfWrongTypeException` | `EdfException` | Неверный тип данных |
| `EdfSrcDataRequiredException` | `EdfException` | Требуется больше данных |
| `EdfDstBufOverflowException` | `EdfException` | Недостаточно места в буфере |
| `EdfFormatterNotRegistredException` | `EdfException` | Форматер для типа не зарегистрирован |
| `WrongPrimitiveException` | `EdfException` | Ожидался другой примитивный тип |
| `BinaryBlockIntegrityException` | `EdfException` | Ошибка CRC блока |
| `BinaryBlockSequenceException` | `EdfException` | Нарушена последовательность блоков (SchemaId, RecordId, PrimOffset) |

---

### Attribute-Based Serialization

#### EdfSerializable Attribute

**Атрибут:** `[EdfSerializable]`

Применяется к классам для автоматической генерации EdfSchema.

```csharp
[EdfSerializable]
public class MySensorData
{
    public uint Timestamp { get; set; }
    public float Temperature { get; set; }
    public string? Location { get; set; }
}

// Автоматически генерируется схема:
var schema = MySensorData.GetEdfSchema();
```

#### EdfArray Attribute

**Атрибут:** `[EdfArray(int[] dimensions)]`

Указывает размерности многомерного массива.

```csharp
[EdfSerializable]
public class MatrixData
{
    [EdfArray([3, 4])]
    public int[,] Matrix { get; set; } = new int[3, 4];
}
```

---

### Error Codes (EdfErrorCode Enum)

| Значение | Имя | Описание |
|----------|-----|---------|
| 0 | `IsOk` | Успешно |
| 1001 | `SrcDataRequred` | Требуется больше исходных данных |
| 1002 | `DstBufOverflow` | Переполнение целевого буфера |

---

### Usage Example: Complete Workflow

```csharp
// Определение типа с атрибутами
[EdfSerializable]
public class SensorReading
{
    public ulong Timestamp { get; set; }
    public float Temperature { get; set; }
    public float Humidity { get; set; }
    public string? Unit { get; set; }
}

// Запись
var readings = new[]
{
    new SensorReading { Timestamp = 1000, Temperature = 25.5f, Humidity = 60f, Unit = "°C" },
    new SensorReading { Timestamp = 2000, Temperature = 26.0f, Humidity = 61f, Unit = "°C" }
};

using (var file = File.Create("sensor_data.bdf"))
using (var writer = new EdfBinaryWriter(file))
{
    writer.WriteSchema(SensorReading.GetEdfSchema());
    
    foreach (var reading in readings)
    {
        var result = writer.WriteValue(reading);
        if (result != EdfErrorCode.IsOk)
            Console.WriteLine($"Warning: {result}");
    }
}

// Чтение
using (var file = File.OpenRead("sensor_data.bdf"))
using (var reader = new EdfBinaryReader(file))
{
    reader.ReadBlock();  // Config
    reader.ReadBlock();  // Schema
    reader.ReadSchema();
    
    while (reader.ReadBlock() && reader.GetBlockType() == EdfBlockType.Data)
    {
        var reading = reader.ReadValue<SensorReading>();
        Console.WriteLine($"{reading.Timestamp}: {reading.Temperature}{reading.Unit}");
    }
}
```

---

### Thread Safety

⚠️ **Не thread-safe!** - по определению, т.к. последовательная запись/чтение примитивов и блоков
