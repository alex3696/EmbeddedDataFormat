using System.Collections;
using System.Collections.Concurrent;
using System.Linq;

namespace EdfNet.Ref;

public class PrimitiveDecomposer : IEnumerable<object>, IEnumerable
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = [];

    private readonly object _source;
    public EdfType? DstType { get; set; }

    public PrimitiveDecomposer(object source = default!)
    {
        _source = source;
    }

    public IEnumerator<object> GetEnumerator() => Decompose(_source).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public IEnumerable<object> Decompose(object? obj)
    {
        if (obj == null) yield break;

        Type type = obj.GetType();

        // 1. Если это "простой" тип — отдаем сразу
        if (IsSimpleType(type))
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
            var props = _propertyCache.GetOrAdd(type, t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0)
                .ToArray());

            foreach (var prop in props)
            {
                object? value = prop.GetValue(obj);
                foreach (var subItem in Decompose(value))
                    yield return subItem;
            }
        }
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal);
    }

}
