using EdfNet.Interfaces;
using System.Linq;
using System.Runtime.CompilerServices;

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
        DstType = edfType;
        ResetAdd(source);
    }
    public void ResetAdd(object? source)
    {
        if (source == null)
            return;
        if (source.GetType().IsSimpleType()
            || (source is byte[] && PoType.Char == DstType?.Type))
        {
            var arr = Array.CreateInstance(source.GetType(), 1);
            arr.SetValue(source, 0);
            source = arr;
        }
        PushComplexObjectElements(source);
    }

    private void PushComplexObjectElements(object obj)
    {
        if (obj is Array arr)
        {
            var elementType = obj.GetType().GetElementType();
            if (true == elementType?.IsSimpleType()
                || (elementType == typeof(byte[]) && PoType.Char == DstType?.Type))
            {
                _stack.Push(new ArrayNode(arr));
            }
            else if (true == elementType?.IsStructType())
            {
                var flatArray = arr.Cast<object>().ToArray();
                for (int i = flatArray.Length - 1; i >= 0; i--)
                {
                    _stack.Push(new ObjectNode(flatArray[i]));
                }
            }
        }
        else
        {
            _stack.Push(new ObjectNode(obj));
        }
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
            var type = top.GetPropertyType();
            //var underlying = Nullable.GetUnderlyingType(type);
            // 1. Если это простой тип или массив байт в режиме Char — это наш целевой примитив
            if (type.IsSimpleType() || (type == typeof(byte[]) && PoType.Char == dstType?.Type))
            {
                _currentNode = top;
                return true;
            }
            else
            {
                var obj = top.GetValue();
                if (null != obj)
                    PushComplexObjectElements(obj);
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
    public int WriteTxt(Span<byte> dst)
    {
        ArgumentNullException.ThrowIfNull(_currentNode);
        return _currentNode.WriteTxt(dst);
    }
    public int ReadTxt(ReadOnlySpan<byte> src)
    {
        ArgumentNullException.ThrowIfNull(_currentNode);
        return _currentNode.ReadTxt(src);
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
        int WriteTxt(Span<byte> dst);
        int ReadTxt(ReadOnlySpan<byte> src);
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
        public int WriteTxt(Span<byte> dst) => _accessors.ElementAt(_index).WriteValueTxt(_target, dst);
        public int ReadTxt(ReadOnlySpan<byte> src) => _accessors.ElementAt(_index).ReadValueTxt(_target, src);
    }

    private class ArrayNode : IContextNode
    {
        EdfType? _edfType;
        private readonly Array _array;
        private int _flatIndex = -1;
        private readonly int[] _indices = [];
        private readonly int[] _dims = [];
        public ArrayNode(Array array)
        {
            _array = array;
            if (array != null)
            {
                _indices = new int[array.Rank];
                _dims = new int[array.Rank];
                for (int i = 0; i < array.Rank; i++)
                {
                    _dims[i] = array.GetLength(i);
                }
            }
        }
        private void UpdateIndices(int flatIndex)
        {
            int remainder = flatIndex;
            for (int i = _dims.Length - 1; i >= 0; i--)
            {
                _indices[i] = remainder % _dims[i];
                remainder /= _dims[i];
            }
        }
        public bool MoveNext(EdfType? dstType)
        {
            _flatIndex++;
            if (_flatIndex >= _array.Length)
                return false;
            UpdateIndices(_flatIndex);
            _edfType = dstType;
            return true;
        }
        public object? GetValue() => _array.GetValue(_indices);
        public void SetValue(object? value) => _array.SetValue(value, _indices);
        public int Write(Span<byte> dst)
        {
            ArgumentNullException.ThrowIfNull(_edfType);
            int elementSize = Marshal.SizeOf(_array.GetType().GetElementType()!);
            if (dst.Length < elementSize)
                return -1;
            ref byte byteRoot = ref MemoryMarshal.GetArrayDataReference(_array);
            int byteOffset = _flatIndex * elementSize;
            ref byte targetByteRef = ref Unsafe.Add(ref byteRoot, byteOffset);
            ReadOnlySpan<byte> srcSpan = MemoryMarshal.CreateReadOnlySpan(ref targetByteRef, elementSize);
            srcSpan.CopyTo(dst);
            return elementSize;
            /*
            var obj = _array.GetValue(_indices);
            ArgumentNullException.ThrowIfNull(obj);
            ArgumentNullException.ThrowIfNull(_edfType);
            var len = PrimitiveWritersBin.TryWrite(dst, _edfType, obj);
            return len;
            */
        }
        public int Read(ReadOnlySpan<byte> src)
        {
            ArgumentNullException.ThrowIfNull(_edfType);
            var len = PrimitiveWritersBin.TryRead(src, _edfType, out var obj);
            _array.SetValue(obj, _indices);
            return len;
        }
        public int WriteTxt(Span<byte> dst)
        {
            var obj = _array.GetValue(_indices);
            ArgumentNullException.ThrowIfNull(obj);
            ArgumentNullException.ThrowIfNull(_edfType);
            return PrimitiveWritersTxt.TryWrite(dst, _edfType, obj);
        }
        public int ReadTxt(ReadOnlySpan<byte> src)
        {
            throw new NotImplementedException();
            //ArgumentNullException.ThrowIfNull(_edfType);
            //var len = Primitive.TryReadTxt(src, _edfType, out var obj);
            //_array.SetValue(obj, _index);
            //return len;
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

