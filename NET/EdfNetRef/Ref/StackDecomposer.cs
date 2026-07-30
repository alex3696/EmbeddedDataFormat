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
public static class AccessorExt
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = [];
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
        var props = _propertyCache.GetOrAdd(type, t =>
                t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.GetIndexParameters().Length == 0)
                .ToArray());

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
    public static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal);

    }
}
public class StackDecomposer
{
    private object? _source;
    private Stack<IContextNode> _stack;
    private IContextNode? _currentNode;

    public EdfType? DstType { get; set; }

    public StackDecomposer(object source = default!)
    {
        Reset(source);
    }

    public void Reset(object? source)
    {
        _stack ??= new();
        _stack.Clear();
        _currentNode = null;
        _source = source;
        if (_source != null)
        {
            // Начальный корень оборачиваем в массив из 1 элемента для единообразия старта
            var rootArray = new object[] { _source };
            _stack.Push(new ArrayNode(rootArray));
        }
    }

    public bool MoveNext(EdfType? dstType)
    {
        while (_stack.Count > 0)
        {
            var top = _stack.Peek();
            if (!top.MoveNext(dstType))
            {
                _stack.Pop();
                continue;
            }
            //object? item = top.GetValue();
            //if (item == null) continue;
            //Type type = item.GetType();
            var type = top.GetPropertyType();
            // 1. Если это простой тип или массив байт в режиме Char — это наш целевой примитив
            if (AccessorExt.IsSimpleType(type) || (type == typeof(byte[]) && PoType.Char == dstType?.Type))
            {
                _currentNode = top;
                return true;
            }
            // 2. Если это массив (но не byte[] в режиме Char) — уходим вглубь по массиву
            if (type.IsArray)
            {
                object? item = top.GetValue();
                if (item == null) continue;
                _stack.Push(new ArrayNode((Array)item));
            }
            // 3. Если сложный объект — уходим вглубь по свойствам
            else
            {
                //var props = _propertyCache.GetOrAdd(type, t =>
                //    t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                //    .Where(p => p.GetIndexParameters().Length == 0)
                //    .ToArray());
                object? item = top.GetValue();
                if (item == null) continue;
                _stack.Push(new ObjectNode(item/*, props*/));
            }
        }
        _currentNode = null;
        return false;
    }

    public int Write(Span<byte> dst)
    {
        ArgumentNullException.ThrowIfNull(_currentNode);
        return _currentNode.Write(dst);
    }
    public int Read(ReadOnlySpan<byte> src)
    {
        ArgumentNullException.ThrowIfNull(_currentNode);
        return _currentNode.Read(src);
    }

    #region Узлы контекста обхода

    private interface IContextNode
    {
        Type GetPropertyType();
        bool MoveNext(EdfType? dstType);
        object? GetValue();
        void SetValue(object? value);
        int Write(Span<byte> dst);
        int Read(ReadOnlySpan<byte> src);
    }

    private class ObjectNode : IContextNode
    {
        EdfType? _edfType;
        private readonly object _target;
        //private readonly PropertyInfo[] _properties;
        private readonly List<IPropertyAccessor> _accessors;

        private int _index = -1;
        public ObjectNode(object target/*, PropertyInfo[] properties*/)
        {
            _target = target;
            //_properties = properties;
            _accessors = AccessorExt.GetOrBuildAccessors(_target.GetType());
        }
        public bool MoveNext(EdfType? dstType)
        {
            _index++;
            _edfType = dstType;
            return _index < _accessors.Count;
        }

        public object? GetValue()
        {
            return _accessors.ElementAt(_index).GetValue(_target);
            //return _properties[_index].GetValue(_target);
        }

        public void SetValue(object? value)
        {
            _accessors.ElementAt(_index).SetValue(_target, value);
            //var prop = _properties[_index];
            //if (prop.CanWrite)
            //{
            //    prop.SetValue(_target, value);
            //}
        }
        public Type GetPropertyType()
        {
            return _accessors.ElementAt(_index).GetPropertyType();
        }
        public int Write(Span<byte> dst)
        {
            return _accessors.ElementAt(_index).WriteValue(_target, dst);
            //var obj = _properties[_index].GetValue(_target);
            //ArgumentNullException.ThrowIfNull(obj);
            //ArgumentNullException.ThrowIfNull(_edfType);
            //var len = PrimitiveWritersBin.TryWrite(dst, _edfType, obj);
            //return len;
        }
        public int Read(ReadOnlySpan<byte> src)
        {
            return _accessors.ElementAt(_index).ReadValue(_target, src);
            //ArgumentNullException.ThrowIfNull(_edfType);
            //var len = PrimitiveWritersBin.TryRead(src, _edfType, out var obj);
            //var prop = _properties[_index];
            //if (prop.CanWrite)
            //    prop.SetValue(_target, obj);
            //return len;
        }
    }

    private class ArrayNode : IContextNode
    {
        EdfType? _edfType;
        private readonly Array _array;
        private readonly int _length;
        private int _index = -1;
        public ArrayNode(Array array)
        {
            _array = array;
            _length = array.Length;
        }
        public bool MoveNext(EdfType? dstType)
        {
            _index++;
            _edfType = dstType;
            return _index < _length;
        }
        public object? GetValue() => _array.GetValue(_index);
        public void SetValue(object? value) => _array.SetValue(value, _index);
        public int Write(Span<byte> dst)
        {
            var obj = _array.GetValue(_index);
            ArgumentNullException.ThrowIfNull(obj);
            ArgumentNullException.ThrowIfNull(_edfType);
            var len = PrimitiveWritersBin.TryWrite(dst, _edfType, obj);
            return len;
        }
        public int Read(ReadOnlySpan<byte> src)
        {
            ArgumentNullException.ThrowIfNull(_edfType);
            var len = PrimitiveWritersBin.TryRead(src, _edfType, out var obj);
            _array.SetValue(obj, _index);
            return len;
        }

        public Type GetPropertyType()
        {
            var t = _array.GetType().GetElementType();
            ArgumentNullException.ThrowIfNull(t);
            return t;
        }
    }

    #endregion
}

