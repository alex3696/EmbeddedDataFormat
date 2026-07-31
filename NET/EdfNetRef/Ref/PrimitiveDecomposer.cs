using EdfNet.Interfaces;
using System.Collections;

namespace EdfNet.Ref;

public class PrimitiveDecomposer : IEdfByteEnumerator
{
    private IEnumerator<object> _enumerator;
    public EdfType? DstType { get; set; }
    public int CurrentIndex { get; private set; }
    public PoType CurrentPoType => DstType.Type;
    public int CurrentPoLen => CurrentPoType.GetSizeOf();

    public PrimitiveDecomposer()
    {
    }
    public void Reset(object? source)
    {
        CurrentIndex = -1;
        if (source != null)
        {
            _enumerator = Decompose(source).GetEnumerator();
        }
        else
            _enumerator?.Reset();
    }
    public bool MoveNext(EdfType? et = null)
    {
        DstType = et;
        return _enumerator.MoveNext();
    }

    public int WriteTxt(Stream dst)
    {
        var obj = _enumerator.Current;
        ArgumentNullException.ThrowIfNull(obj, nameof(obj));
        ArgumentNullException.ThrowIfNull(DstType, nameof(DstType));
        return PrimitiveWritersTxt.TryWrite(dst, DstType, obj);
    }
    public int Write(Span<byte> dst)
    {
        var obj = _enumerator.Current;
        ArgumentNullException.ThrowIfNull(obj, nameof(obj));
        ArgumentNullException.ThrowIfNull(DstType, nameof(DstType));
        return PrimitiveWritersBin.TryWrite(dst, DstType, obj);
    }
    public int Read(ReadOnlySpan<byte> src)
    {
        throw new NotImplementedException();
    }

    private IEnumerable<object> Decompose(object? obj)
    {
        if (obj == null) yield break;

        Type type = obj.GetType();

        // 1. Если это "простой" тип — отдаем сразу
        if (AccessorExt.IsSimpleType(type))
        {
            yield return obj;
        }
        else if (obj is byte[] && PoType.Char == DstType?.Type)
        {
            yield return obj;
        }
        // 2. Если это коллекция (массив, список) — рекурсивно раскладываем каждый элемент
        else if (obj is IEnumerable enumerable)
        {
            foreach (var item in enumerable)
            {
                foreach (var subItem in Decompose(item))
                    yield return subItem;
            }
        }
        // 3. Если это сложный объект — рекурсивно раскладываем каждое свойство
        else
        {
            var props = AccessorExt.GetProperties(type);

            foreach (var prop in props)
            {
                object? value = prop.GetValue(obj);
                foreach (var subItem in Decompose(value))
                    yield return subItem;
            }
        }
    }
}
