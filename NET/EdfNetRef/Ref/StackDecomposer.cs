using EdfNet.Interfaces;
using System.Linq;

namespace EdfNet.Ref;

public class StackDecomposer : IEdfByteEnumerator
{
    private Stack<IContextNode> _stack;
    private IContextNode? _currentNode;
    public EdfType? DstType { get; set; }
    public int CurrentIndex { get; private set; }
    public PoType CurrentPoType => DstType.Type;
    public int CurrentPoLen => CurrentPoType.GetSizeOf();

    public StackDecomposer(EdfType? edfType = default, object source = default!)
    {
        Reset(edfType, source);
    }

    public void Reset(EdfType? edfType, object? source)
    {
        _stack ??= new();
        _stack.Clear();
        _currentNode = null;
        CurrentIndex = -1;
        if (source != null)
        {
            DstType = edfType;
            if (AccessorExt.IsSimpleType(source.GetType())
                || (source is byte[] && PoType.Char == DstType?.Type))
            {
                var rootArray = new object[] { source };
                _stack.Push(new ArrayNode(rootArray));
            }
            else
                _stack.Push(new ObjectNode(source));
        }
    }
    public void ResetAdd(object? source)
    {
        if (source == null)
            return;
        if (AccessorExt.IsSimpleType(source.GetType())
            || (source is byte[] && PoType.Char == DstType?.Type))
        {
            var rootArray = new object[] { source };
            _stack.Push(new ArrayNode(rootArray));
        }
        else
            _stack.Push(new ObjectNode(source));
    }


    public bool MoveNext(EdfType? dstType)
    {
        DstType = dstType;
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
            var et = _array.GetType().GetElementType();
            if (typeof(object) == et)
            {
                object? item0 = _array.GetValue(0);
                if (item0 != null)
                    et = item0.GetType();
            }
            ArgumentNullException.ThrowIfNull(et);
            return et;
        }
    }

    #endregion
}

