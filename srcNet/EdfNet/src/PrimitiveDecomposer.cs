using System.Collections;

namespace NetEdf.src;

public class PrimitiveDecomposer : IEnumerable<object>, IEnumerable
{
    private readonly object _source;

    public PrimitiveDecomposer(object source)
    {
        _source = source;
    }

    public IEnumerator<object> GetEnumerator() => Decompose(_source).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private IEnumerable<object> Decompose(object obj)
    {
        if (obj == null) yield break;

        Type type = obj.GetType();

        // 1. Если это "простой" тип — отдаем сразу
        if (IsSimpleType(type))
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
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                // Чтобы избежать ошибок с индексаторами (например, у строк или списков)
                if (prop.GetIndexParameters().Length > 0) continue;

                object value = prop.GetValue(obj);
                foreach (var subItem in Decompose(value))
                    yield return subItem;
            }
        }
    }

    private bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal) ||
               type == typeof(DateTime) ||
               type == typeof(Guid);
    }
}
