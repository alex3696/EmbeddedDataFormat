using System.Collections;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;

namespace EdfNet.Ref;

public class PrimitiveDecomposer : IEnumerable<object>, IEnumerable
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = [];

    private readonly object _source;
    public EdfType? DstType { get; set; }

    public PrimitiveDecomposer(object source)
    {
        _source = source;
    }

    public IEnumerator<object> GetEnumerator() => Decompose(_source).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    private IEnumerable<object> Decompose(object? obj)
    {
        if (obj == null) yield break;

        Type type = obj.GetType();

        // 1. Если это "простой" тип — отдаем сразу
        if (IsSimpleType(type))
        {
            yield return obj;
        }
        else if (obj is Array
            && type.GetElementType() == typeof(byte)
            && 1 == type.GetArrayRank()
            && PoType.Char == DstType?.Type)
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
            /*
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
            */
            var accessors = _propertyAccessorsCache.GetOrAdd(type, CompileAccessors);
            // Вызов accessor(obj) работает со скоростью нативного C# кода
            foreach (var accessor in accessors)
            {
                object? value = accessor(obj);
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



    private static readonly ConcurrentDictionary<Type, Func<object, object?>[]> _propertyAccessorsCache = new();

    // Метод генерации и компиляции выражений (вызывается 1 раз для каждого типа)
    private static Func<object, object?>[] CompileAccessors(Type type)
    {
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.GetIndexParameters().Length == 0);
        var accessors = new List<Func<object, object?>>();
        // Общий параметр для всех лямбд: (object instance) => ...
        var instanceParam = Expression.Parameter(typeof(object), "instance");
        foreach (var prop in props)
        {
            // 1. Приведение типа: ((YourTargetType)instance)
            var castedInstance = Expression.Convert(instanceParam, type);
            // 2. Обращение к свойству: ((YourTargetType)instance).YourProperty
            var propertyAccess = Expression.Property(castedInstance, prop);
            // 3. Приведение результата к object: (object)(((YourTargetType)instance).YourProperty)
            var castedResult = Expression.Convert(propertyAccess, typeof(object));
            // 4. Создание лямбды: instance => (object)((TargetType)instance).Property
            var lambda = Expression.Lambda<Func<object, object?>>(castedResult, instanceParam);
            // 5. Компиляция в делегат
            accessors.Add(lambda.Compile());
        }
        return accessors.ToArray();
    }
}
