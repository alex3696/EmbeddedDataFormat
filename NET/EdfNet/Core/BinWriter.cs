using EdfNet.Core.Gen;
using System.Collections.Concurrent;

namespace EdfNet.Core;

public class BinWriter : BaseWriter
{
    private readonly Stream _bw;
    private readonly BinBlock _blk;
    public ushort CurrentDataLen => _blk.DataLen;
    protected override ushort _DataLen
    {
        get => _blk.DataLen;
        set => _blk.DataLen = value;
    }
    protected override Span<byte> _DataBuffer => _blk.DataBuffer;

    protected override EdfErr TrySrcToX(PoType t, object obj, Span<byte> dst, out int w)
        => Primitives.TrySrcToBin(t, obj, dst, out w);
    protected override EdfErr WriteSep(ReadOnlySpan<byte> src, ref Span<byte> dst, ref int skip, ref int wqty, ref int writed)
        => EdfErr.IsOk;

    public BinWriter(Stream stream, Config? cfg = default)
        : base(cfg ?? Config.Default)
    {
        _bw = stream;
        _blk = new(Cfg.Blocksize);
        if (0 == stream.Position)
            Write(Cfg);
    }
    protected override void Dispose(bool disposing)
    {
        Flush();
        _bw.Flush();
        base.Dispose(disposing);
    }
    public override void Flush()
    {
        if (null != CurrentSchema && 0 != _blk.DataLen)
        {
            _bw.Write(_blk);
        }
        _blk.Clear();
    }
    public override void Write(Config h)
    {
        Flush();
        _blk.Type = BlockType.Config;
        _blk.Append(h.VersMajor);
        _blk.Append(h.VersMinor);
        _blk.Append(h.Encoding);
        _blk.Append(h.Blocksize);
        _blk.Append((ushort)0);
        _blk.Append(h.Flags);
        ArgumentOutOfRangeException.ThrowIfNotEqual(_blk.DataLen, 12);
        _bw.Write(_blk);
        _blk.Reset();
    }
    public override void Write(Schema sch)
    {
        Flush();
        _blk.Type = BlockType.Schema;
        _blk.Append(sch.Id);
        _blk.Append(sch.Name);
        _blk.Append(sch.Desc);
        Append(_blk, sch.Type);
        _bw.Write(_blk);
        _blk.Reset();
        CurrentSchema = sch;
        _blk.Type = BlockType.Data;
        _recId = 0;
        _prmOffset = 0;
    }

    private static void Append(BinBlock blk, EdfType t)
    {
        blk.Append(t.Type);
        if (null != t.Dims && 0 < t.Dims.Length)
        {
            blk.Append((byte)t.Dims.Length);
            for (int i = 0; i < t.Dims.Length; i++)
                blk.Append(t.Dims[i]);
        }
        else
            blk.Append((byte)0);

        blk.Append(t.Name);

        if (PoType.Struct == t.Type && null != t.Childs && 0 < t.Childs.Length)
        {
            blk.Append((byte)t.Childs.Length);
            for (int i = 0; i < t.Childs.Length; i++)
            {
                Append(blk, t.Childs[i]);
            }
        }
    }

    // Потокобезопасный кэш для хранения скомпилированных методов записи под каждый тип
    private static readonly ConcurrentDictionary<Type, Action<BinWriter, object>> _writerCache = new();

    /// <summary>
    /// Универсальная точка входа для записи любой структуры, помеченной [EdfSerializable]
    /// </summary>
    public EdfErr WriteGen(object obj)
    {
        if (obj == null) return EdfErr.WrongType;

        Type type = obj.GetType();

        // Достаем из кэша (или создаем при первом обращении) быстрый делегат для этого типа
        var writeAction = _writerCache.GetOrAdd(type, CreateWriterDelegate);

        // Вызываем скомпилированный метод. Внутри произойдет магия без боксинга энумератора!
        writeAction(this, obj);
        return EdfErr.IsOk;
    }

    /// <summary>
    /// Высокопроизводительный метод, куда в итоге проваливается запись.
    /// Сюда передается конкретный СГЕНЕРИРОВАННЫЙ энумератор-структура.
    /// </summary>
    public EdfErr WriteData<TEnumerator>(TEnumerator enumerator)
        where TEnumerator : struct, IEdfByteEnumerator
    {
        // Берем срез свободного места в текущем буфере блока
        // BinBlock.HeaderLen + _blk.DataLen — это указатель на первый свободный байт контента
        Span<byte> _blockDataBuffer = _blk.DataBuffer.Slice(_blk.DataLen);
        while (enumerator.MoveNext())
        {
            // Пробуем записать примитив в доступный остаток блока
            int bytesWritten = enumerator.Write(_blockDataBuffer);
            if (0 >= bytesWritten)// Если вернулся меньше 0, значит примитив целиком не поместился (попримитивный разрыв).
            {
                Flush();// Сбрасываем (Flush) текущий блок на диск/в поток и очищаем буфер
                // Подготавливаем новый блок, записывая в заголовок SchId, RecId и тип текущего примитива
                PrepareNewBlock(enumerator.CurrentPoType);
                // Пересчитываем срез свободного места для абсолютно нового, чистого блока
                _blockDataBuffer = _blk.DataBuffer.Slice(BinBlock.HeaderLen + _blk.DataLen);
                // Пробуем записать примитив еще раз, теперь уже в начало нового блока
                bytesWritten = enumerator.Write(_blockDataBuffer);
                if (0 >= bytesWritten) // Защита от бесконечного цикла (если примитив физически больше размера блока)
                    return EdfErr.DstBufOverflow;
            }
            _prmOffset++;
            // Фиксируем, сколько байт реально записал энумератор в буфер блока
            _blk.DataLen += (ushort)bytesWritten;

            // Сдвигаем наш Span вперед, отрезая уже заполненную часть памяти
            _blockDataBuffer = _blockDataBuffer.Slice(bytesWritten);
        }
        _recId++;
        return EdfErr.IsOk;
    }

    /// <summary>
    /// Этот метод вызывается ВСЕГО ОДИН РАЗ для каждого типа структуры при первом вызове.
    /// Он находит сгенерированный энумератор и строит быструю схему вызова через Expression Trees.
    /// </summary>
    private static Action<BinWriter, object> CreateWriterDelegate(Type type)
    {
        // 1. Ищем сгенерированный энумератор по имени
        string enumeratorTypeName = $"{type.FullName}ByteEnumerator";
        Type enumeratorType = type.Assembly.GetType(enumeratorTypeName)
            ?? throw new InvalidOperationException($"Enumerator for type {type.Name} not found. Did you forget [EdfSerializable]?");

        // 2. Ищем метод WriteData в BinWriter
        MethodInfo writeDataMethod = typeof(BinWriter)
            .GetMethod(nameof(BinWriter.WriteData))!
            .MakeGenericMethod(enumeratorType);

        ConstructorInfo ctor = enumeratorType.GetConstructor(
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            types: new[] { type.MakeByRefType() },
            modifiers: null)!;

        // 3. Строим Expression Tree
        var writerParam = System.Linq.Expressions.Expression.Parameter(typeof(BinWriter), "writer");
        var objParam = System.Linq.Expressions.Expression.Parameter(typeof(object), "obj");

        // Приведение типа: (Position)obj
        var castObj = System.Linq.Expressions.Expression.Convert(objParam, type);

        // Создание энумератора: new PositionByteEnumerator(castObj)
        // Expression.New сам разберется, как передать структуру в ByRef конструктор
        var createEnumerator = System.Linq.Expressions.Expression.New(ctor, castObj);

        // Вызов метода: writer.WriteData(...)
        var callWriteData = System.Linq.Expressions.Expression.Call(writerParam, writeDataMethod, createEnumerator);

        var lambda = System.Linq.Expressions.Expression.Lambda<Action<BinWriter, object>>(callWriteData, writerParam, objParam);
        return lambda.Compile();
    }


    uint _recId = 0;
    ushort _prmOffset = 0;
    /* Запись заголовков SchId, RecId, PrmOffset */
    private void PrepareNewBlock(PoType type)
    {
        ArgumentNullException.ThrowIfNull(CurrentSchema);
        _blk.Reset();
        //_blk.Append(CurrentSchema.Id);
        //_blk.Append(_recId);
        //_blk.Append(_prmOffset);
    }
}
