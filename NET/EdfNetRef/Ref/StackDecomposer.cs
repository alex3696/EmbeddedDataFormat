using System.Collections.Concurrent;
using System.Linq;

namespace EdfNet.Ref;

public class StackDecomposer
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> _propertyCache = [];

    private object _source;
    private Stack<IContextNode> _stack;
    private IContextNode? _currentNode;

    public EdfType? DstType { get; set; }

    public StackDecomposer(object source = default!)
    {
        Reset(source);
    }

    public void Reset(object source)
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

    public bool MoveNext(EdfType? DstType)
    {
        while (_stack.Count > 0)
        {
            var top = _stack.Peek();
            if (!top.MoveNext())
            {
                _stack.Pop();
                continue;
            }
            object? item = top.CurrentValue;
            if (item == null) continue;
            Type type = item.GetType();
            // 1. Если это простой тип или массив байт в режиме Char — это наш целевой примитив
            if (IsSimpleType(type) || (item is byte[] && PoType.Char == DstType?.Type))
            {
                _currentNode = top;
                return true;
            }
            // 2. Если это массив (но не byte[] в режиме Char) — уходим вглубь по массиву
            if (type.IsArray)
            {
                _stack.Push(new ArrayNode((Array)item));
            }
            // 3. Если сложный объект — уходим вглубь по свойствам
            else
            {
                var props = _propertyCache.GetOrAdd(type, t =>
                    t.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(p => p.GetIndexParameters().Length == 0)
                    .ToArray());

                _stack.Push(new ObjectNode(item, props));
            }
        }
        _currentNode = null;
        return false;
    }

    public object GetValue()
    {
        if (_currentNode == null)
            throw new InvalidOperationException("Итератор не выставлен на примитив. Вызовите MoveNext().");

        return _currentNode.CurrentValue!;
    }

    public void SetValue(object obj)
    {
        if (_currentNode == null)
            throw new InvalidOperationException("Итератор не выставлен на примитив. Некуда записывать значение.");

        _currentNode.SetValue(obj);
    }

    private static bool IsSimpleType(Type type)
    {
        return type.IsPrimitive ||
               type.IsEnum ||
               type == typeof(string) ||
               type == typeof(decimal);
    }

    #region Узлы контекста обхода

    private interface IContextNode
    {
        bool MoveNext();
        object? CurrentValue { get; }
        void SetValue(object? value);
    }

    // Узел для обхода свойств обычного объекта
    private class ObjectNode : IContextNode
    {
        private readonly object _target;
        private readonly PropertyInfo[] _properties;
        private int _index = -1;

        public object? CurrentValue => _properties[_index].GetValue(_target);

        public ObjectNode(object target, PropertyInfo[] properties)
        {
            _target = target;
            _properties = properties;
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _properties.Length;
        }

        public void SetValue(object? value)
        {
            var prop = _properties[_index];
            if (prop.CanWrite)
            {
                prop.SetValue(_target, value);
            }
        }
    }

    // Узел для быстрого обхода элементов массива без использования IEnumerator
    private class ArrayNode : IContextNode
    {
        private readonly Array _array;
        private readonly int _length;
        private int _index = -1;

        public object? CurrentValue => _array.GetValue(_index);

        public ArrayNode(Array array)
        {
            _array = array;
            _length = array.Length;
        }

        public bool MoveNext()
        {
            _index++;
            return _index < _length;
        }

        public void SetValue(object? value)
        {
            _array.SetValue(value, _index);
        }
    }

    #endregion
}

