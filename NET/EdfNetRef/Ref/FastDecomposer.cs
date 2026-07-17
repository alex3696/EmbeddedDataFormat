using System.Buffers;
using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;

namespace EdfNet.Ref;

public interface IPropertyAccessor
{
    void WriteValue(object target, IBufferWriter<byte> writer);
    void ReadValue(object target, IBufferWriter<byte> writer);
}

public class PropertyAccessor<TTarget, TProperty> : IPropertyAccessor
    where TTarget : class
    where TProperty : struct
{
    private readonly Func<TTarget, TProperty> _getter;
    private readonly Action<TTarget, TProperty> _setter;
    public PropertyAccessor(MethodInfo getMethod, MethodInfo setMethod)
    {
        _getter = (Func<TTarget, TProperty>)Delegate.CreateDelegate(typeof(Func<TTarget, TProperty>), getMethod);
        _setter = (Action<TTarget, TProperty>)Delegate.CreateDelegate(typeof(Action<TTarget, TProperty>), setMethod);
    }
    public void WriteValue(object target, IBufferWriter<byte> writer)
    {
        TProperty value = _getter((TTarget)target);
        Write(value, writer);
    }
    public void ReadValue(object target, IBufferWriter<byte> buf)
    {
        _setter.Invoke((TTarget)target, Read<TProperty>(buf));
    }
    public static void Write<T>(T value, IBufferWriter<byte> writer)
        where T : struct
    {
        int size = Unsafe.SizeOf<T>();
        Span<byte> span = writer.GetSpan(size);
        MemoryMarshal.Write(span, in value);
        writer.Advance(size);
    }
    public static T Read<T>(IBufferWriter<byte> buf)
        where T : struct
    {
        int size = Unsafe.SizeOf<T>();
        Span<byte> span = buf.GetSpan(size);
        var val = MemoryMarshal.Read<T>(span);
        buf.Advance(size);
        return val;
    }
}

public class FastDecomposer
{
    private static readonly ConcurrentDictionary<Type, List<IPropertyAccessor>> _accessorCache = new();
    public EdfType? DstType { get; set; }

    public FastDecomposer()
    {
    }
    public void Serialize(object src, IBufferWriter<byte> writer)
    {
        var accessors = GetOrBuildAccessors(src.GetType());
        foreach (var accessor in accessors)
        {
            accessor.WriteValue(src, writer);
        }
    }
    public void Deserialize(object dst, IBufferWriter<byte> buf)
    {
        var accessors = GetOrBuildAccessors(dst.GetType());
        foreach (var accessor in accessors)
        {
            accessor.ReadValue(dst, buf);
        }
    }
    private static List<IPropertyAccessor> GetOrBuildAccessors(Type type)
    {
        return _accessorCache.GetOrAdd(type, t => BuildAccessorsFlat(t).ToList());
    }
    private static IEnumerable<IPropertyAccessor> BuildAccessorsFlat(Type type)
    {
        if (IsSimpleType(type))
        {
            // Напрямую примитивы без родительского объекта через PropertyAccessor обработать нельзя,
            // так как у них нет PropertyInfo (нет Getter/Setter). 
            // Для них обычно пишется отдельная ветка в Serialize, либо они заворачиваются в DTO.
            yield break;
        }

        // Базовые свойства текущего уровня объекта
        var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                        .Where(p => p.GetIndexParameters().Length == 0 && p.CanRead && p.CanWrite);

        foreach (var prop in props)
        {
            Type propType = prop.PropertyType;

            if (propType.IsValueType && !propType.IsEnum && !IsSimpleType(propType))
            {
                // Если свойство — это вложенная пользовательская структура (Сложный тип), 
                // рекурсивно вытаскиваем её свойства
                foreach (var subAccessor in BuildAccessorsFlat(propType))
                {
                    yield return subAccessor;
                }
            }
            else if (propType.IsValueType) // Элементарные struct (int, float, bool, custom enums)
            {
                yield return MakeAccessor(type, prop);
            }
            // Дополнительные ветки для массивов/коллекций (пропущены для лаконичности)
        }
    }
    private static IPropertyAccessor MakeAccessor(Type targetType, PropertyInfo prop)
    {
        Type accessorGenericType = typeof(PropertyAccessor<,>).MakeGenericType(targetType, prop.PropertyType);
        return (IPropertyAccessor)Activator.CreateInstance(
            accessorGenericType,
            prop.GetMethod!,
            prop.SetMethod!
        )!;
    }
    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal);
    }
}
