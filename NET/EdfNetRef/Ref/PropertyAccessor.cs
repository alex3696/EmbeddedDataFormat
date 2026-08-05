using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;

namespace EdfNet.Ref;

public interface IPropertyAccessor
{
    Type GetPropertyType();
    int WriteValue(object target, Span<byte> dst);
    int ReadValue(object target, ReadOnlySpan<byte> src);
    object? GetValue(object target);
    void SetValue(object target, object? value);
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
    public int WriteValue(object target, Span<byte> dst)
    {
        TProperty value = _getter((TTarget)target);
        MemoryMarshal.Write(dst, in value);
        return Unsafe.SizeOf<TTarget>();
    }
    public int ReadValue(object target, ReadOnlySpan<byte> src)
    {
        _setter.Invoke((TTarget)target, MemoryMarshal.Read<TProperty>(src));
        return Unsafe.SizeOf<TProperty>();
    }
    public object? GetValue(object target) => _getter((TTarget)target);
    public void SetValue(object target, object? val) => _setter.Invoke((TTarget)target, (TProperty)val!);
    public Type GetPropertyType() => typeof(TProperty);
}

public class PropertyAccessorString<TTarget> : IPropertyAccessor
//where TTarget : class
{
    private readonly Func<TTarget, string?> _getter;
    private readonly Action<TTarget, string?> _setter;
    public PropertyAccessorString(MethodInfo getMethod, MethodInfo setMethod)
    {
        _getter = (Func<TTarget, string?>)Delegate.CreateDelegate(typeof(Func<TTarget, string?>), getMethod);
        _setter = (Action<TTarget, string?>)Delegate.CreateDelegate(typeof(Action<TTarget, string?>), setMethod);
    }
    public int WriteValue(object target, Span<byte> dst)
    {
        string? value = _getter((TTarget)target);
        return EdfBinString.WriteBin(value, dst);
    }
    public int ReadValue(object target, ReadOnlySpan<byte> src)
    {
        var len = EdfBinString.ReadBin(src, out var str);
        _setter.Invoke((TTarget)target, str);
        return len;
    }
    public object? GetValue(object target) => _getter((TTarget)target);
    public void SetValue(object target, object? val) => _setter.Invoke((TTarget)target, (string?)val!);
    public Type GetPropertyType() => typeof(string);
}


public class PropertyAccessorArray<TTarget, TProperty> : IPropertyAccessor
//where TTarget : class
{
    private readonly Func<TTarget, TProperty> _getter;
    private readonly Action<TTarget, TProperty> _setter;
    public PropertyAccessorArray(MethodInfo getMethod, MethodInfo setMethod)
    {
        _getter = (Func<TTarget, TProperty>)Delegate.CreateDelegate(typeof(Func<TTarget, TProperty>), getMethod);
        _setter = (Action<TTarget, TProperty>)Delegate.CreateDelegate(typeof(Action<TTarget, TProperty>), setMethod);
    }
    public int WriteValue(object target, Span<byte> dst)
    {
        throw new NotImplementedException();
    }
    public int ReadValue(object target, ReadOnlySpan<byte> src)
    {
        throw new NotImplementedException();
    }
    public object? GetValue(object target) => _getter((TTarget)target);
    public void SetValue(object target, object? val) => _setter.Invoke((TTarget)target, (TProperty)val!);
    public Type GetPropertyType() => typeof(TProperty);
}

public static class AccessorExt
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = [];
    public static PropertyInfo[] GetProperties(Type type)
    {
        return _propertyCache.GetOrAdd(type, t =>
            t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.GetIndexParameters().Length == 0)
            .ToArray());
    }

    private static IEnumerable<IPropertyAccessor> BuildAccessorsFlat(Type type)
    {
        if (type.IsSimpleType())
        {
            // Напрямую примитивы без родительского объекта через PropertyAccessor обработать нельзя,
            // так как у них нет PropertyInfo (нет Getter/Setter). 
            // Для них обычно пишется отдельная ветка в Serialize, либо они заворачиваются в DTO.
            yield break;
        }

        // Базовые свойства текущего уровня объекта
        var props = GetProperties(type);

        foreach (var prop in props)
        {
            Type propType = prop.PropertyType;
            propType = Nullable.GetUnderlyingType(propType) ?? propType;
            if (propType.IsSimpleType()) // Элементарные struct (int, float, bool, custom enums)
            {
                if (propType.IsValueType)
                    yield return MakeAccessor(type, prop);
                else if (propType == typeof(string))
                    yield return MakeAccessorString(type, prop);
            }
            else if (propType.IsArray)
            {
                Type accr = typeof(PropertyAccessorArray<,>).MakeGenericType(type, propType!);
                yield return (IPropertyAccessor)Activator.CreateInstance(accr, prop.GetMethod!, prop.SetMethod!)!;
            }
            else if (propType.IsStructType())
            {
                // Если свойство — это вложенная пользовательская структура (Сложный тип), 
                // рекурсивно вытаскиваем её свойства
                foreach (var subAccessor in BuildAccessorsFlat(propType))
                {
                    yield return subAccessor;
                }
            }
        }
    }
    private static readonly ConcurrentDictionary<Type, List<IPropertyAccessor>> _accessorCache = new();
    public static List<IPropertyAccessor> GetOrBuildAccessors(Type type)
    {
        return _accessorCache.GetOrAdd(type, static t => BuildAccessorsFlat(t).ToList());
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
    private static IPropertyAccessor MakeAccessorString(Type targetType, PropertyInfo prop)
    {
        Type accr = typeof(PropertyAccessorString<>).MakeGenericType(targetType);
        return (IPropertyAccessor)Activator.CreateInstance(accr, prop.GetMethod!, prop.SetMethod!)!;
    }

}
