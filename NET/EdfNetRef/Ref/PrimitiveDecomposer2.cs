using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

namespace EdfNet.Ref;

public class MyArrayBufferWriter : IBufferWriter<byte>
{
    private byte[] _buffer;
    private int _index; // Текущая позиция записи

    public MyArrayBufferWriter(int initialCapacity = 256)
    {
        _buffer = new byte[initialCapacity];
        _index = 0;
    }

    // Возвращает Span, указывающий на Свободную часть массива
    public Span<byte> GetSpan(int sizeHint = 0)
    {
        if (sizeHint == 0) sizeHint = 1; // Минимально просим 1 байт

        // Если оставшегося места не хватает, увеличиваем массив
        if (_index + sizeHint > _buffer.Length)
        {
            ResizeBuffer(sizeHint);
        }

        // Возвращаем срез массива от текущего индекса до конца
        return _buffer.AsSpan(_index);
    }

    // Метод продвижения указателя после реальной записи данных
    public void Advance(int count)
    {
        if (count < 0 || _index + count > _buffer.Length)
            throw new ArgumentOutOfRangeException(nameof(count));

        _index += count; // Просто сдвигаем индекс вперед
    }

    public Memory<byte> GetMemory(int sizeHint = 0)
        => _buffer.AsMemory(_index);

    // Дополнительные удобные свойства, которых нет в интерфейсе, но есть в классе
    public ReadOnlySpan<byte> WrittenSpan => _buffer.AsSpan(0, _index);
    public int WrittenCount => _index;
    public void Clear() => _index = 0;

    private void ResizeBuffer(int sizeHint)
    {
        int newSize = Math.Max(_buffer.Length * 2, _index + sizeHint);
        byte[] newBuffer = new byte[newSize];
        Array.Copy(_buffer, 0, newBuffer, 0, _index);
        _buffer = newBuffer;
    }
}
public class PrimitiveDecomposerZeroAlloc
{
    // Главный generic-метод для примитивов (структур)
    // Ограничение `where T : struct` гарантирует отсутствие boxing!
    public static void WriteStruct<T>(T value, IBufferWriter<byte> writer)
        where T : struct
    {
        // Получаем размер типа прямо на этапе компиляции (0 аллокаций)
        int size = Unsafe.SizeOf<T>();

        // Запрашиваем Span нужного размера
        Span<byte> span = writer.GetSpan(size);

        // Копируем память структуры напрямую в Span (Zero-Allocation магия)
        MemoryMarshal.Write(span, in value);

        // Продвигаем писатель на размер структуры
        writer.Advance(size);
    }
}

public class AotPrimitiveDecomposer
{
    private interface IPropertyAccessor
    {
        void WriteValue(object target, IBufferWriter<byte> writer);
    }

    private class PropertyAccessor<TTarget, TProperty> : IPropertyAccessor
        where TTarget : class
        where TProperty : struct
    {
        private readonly Func<TTarget, TProperty> _getter;

        public PropertyAccessor(MethodInfo getMethod)
        {
            // Использован System.Delegate вместо delegate с маленькой буквы
            _getter = (Func<TTarget, TProperty>)System.Delegate.CreateDelegate(typeof(Func<TTarget, TProperty>), getMethod);
        }

        public void WriteValue(object target, IBufferWriter<byte> writer)
        {
            TProperty value = _getter((TTarget)target);
            PrimitiveDecomposerZeroAlloc.WriteStruct(value, writer);
        }
    }

    private static readonly ConcurrentDictionary<Type, IPropertyAccessor[]> _accessorCache = new();

    public void Decompose(object? obj, IBufferWriter<byte> writer)
    {
        if (obj == null) return;
        Type type = obj.GetType();

        if (obj is System.Collections.IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                Decompose(item, writer);
            }
            return;
        }

        var accessors = _accessorCache.GetOrAdd(type, CreateAccessors);
        foreach (var accessor in accessors)
        {
            accessor.WriteValue(obj, writer);
        }
    }

    private static IPropertyAccessor[] CreateAccessors(Type type)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
        var list = new System.Collections.Generic.List<IPropertyAccessor>();

        foreach (var prop in props)
        {
            if (prop.GetIndexParameters().Length > 0) continue;

            var getMethod = prop.GetMethod;
            if (getMethod == null) continue;

            Type accessorGenericType = typeof(PropertyAccessor<,>).MakeGenericType(type, prop.PropertyType);
            var accessor = (IPropertyAccessor)Activator.CreateInstance(accessorGenericType, getMethod)!;
            list.Add(accessor);
        }

        return list.ToArray();
    }
}
