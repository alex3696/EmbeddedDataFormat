using EdfNet.Interfaces;

namespace EdfNet.Ref;

public class YeldDecomposer : IEdfByteEnumerator
{
    private IEnumerator<IContextNode> _enumerator;
    public EdfType? DstType { get; set; }
    public int CurrentIndex { get; private set; }
    public PoType CurrentPoType => DstType.Type;
    public int CurrentPoLen => CurrentPoType.GetSizeOf();

    public YeldDecomposer(EdfType? edfType = default, object source = default!)
    {
        Reset(edfType, source);
    }
    public void Reset(EdfType? edfType, object? source)
    {
        CurrentIndex = -1;
        if (source != null)
        {
            DstType = edfType;
            if (AccessorExt.IsSimpleType(source.GetType())
                || (source is byte[] && PoType.Char == DstType?.Type))
                source = new object[] { source };
            _enumerator = Decompose(source).GetEnumerator();
        }
        else
            _enumerator?.Reset();
    }
    public void ResetAdd(object? source)
    {
        if (source == null)
            return;
        if (AccessorExt.IsSimpleType(source.GetType())
                || (source is byte[] && PoType.Char == DstType?.Type))
            source = new object[] { source };
        _enumerator = Decompose(source).GetEnumerator();
    }

    public bool MoveNext(EdfType? dstType)
    {
        DstType = dstType;
        return _enumerator.MoveNext();
    }
    public int Write(Span<byte> dst) => _enumerator.Current.Write(dst);
    public int Read(ReadOnlySpan<byte> src) => _enumerator.Current.Read(src);

    private IEnumerable<IContextNode> Decompose(object? obj)
    {
        if (obj == null)
            yield break;
        Type type = obj.GetType();

        if (AccessorExt.IsSimpleType(type))
            yield break;
        if (obj is byte[] && PoType.Char == DstType?.Type)
            yield break;

        if (obj is Array arr)
        {
            Type et = type.GetElementType()!;
            if (typeof(object) == et)
            {
                object? item0 = arr.GetValue(0);
                if (item0 != null)
                    et = item0.GetType();
            }
            if (AccessorExt.IsSimpleType(et))
            {
                for (int i = 0; i < arr.Length; ++i)
                {
                    CurrentIndex++;
                    yield return new YArrayNode(DstType, arr, i);
                }
            }
            else if (typeof(byte[]) == et && PoType.Char == DstType?.Type)
            {
                for (int i = 0; i < arr.Length; ++i)
                {
                    CurrentIndex++;
                    yield return new YArrayNode(DstType, arr, i);
                }
            }
            else
            {
                for (int i = 0; i < arr.Length; ++i)
                {
                    var val = arr.GetValue(i);
                    foreach (var subItem in Decompose(val))
                        yield return subItem;
                }
            }
        }
        else
        {
            var acc = AccessorExt.GetOrBuildAccessors(obj.GetType());
            for (int i = 0; i < acc.Count; ++i)
            {
                if (AccessorExt.IsSimpleType(acc[i].GetPropertyType()))
                {
                    CurrentIndex++;
                    yield return new YObjNode(DstType, obj, acc[i]);
                }
                else
                {
                    foreach (var subItem in Decompose(acc[i].GetValue(obj)))
                        yield return subItem;
                }
            }
        }
    }
    private interface IContextNode
    {
        //Type GetPropertyType();
        object? GetValue();
        void SetValue(object? value);
        int Write(Span<byte> dst);
        int Read(ReadOnlySpan<byte> src);
    }
    private class YObjNode : IContextNode
    {
        private EdfType? _edfType;
        private object _target;
        private IPropertyAccessor _accessor;

        public YObjNode()
        {

        }
        public YObjNode(EdfType? edfType, object target, IPropertyAccessor accessor)
        {
            Reset(edfType, target, accessor);
        }
        public void Reset(EdfType? edfType, object target, IPropertyAccessor accessor)
        {
            _edfType = edfType;
            _target = target;
            _accessor = accessor;
        }
        public Type GetPropertyType()
        {
            return _accessor.GetPropertyType();
        }
        public object? GetValue() => _accessor.GetValue(_target);
        public void SetValue(object? value) => _accessor.SetValue(_target, value);
        public int Read(ReadOnlySpan<byte> src) => _accessor.ReadValue(_target, src);
        public int Write(Span<byte> dst) => _accessor.WriteValue(_target, dst);

    }
    private class YArrayNode : IContextNode
    {
        private EdfType? _edfType;
        private Array _array;
        private int _index;

        public YArrayNode()
        {

        }
        public YArrayNode(EdfType? edfType, Array array, int index)
        {
            Reset(edfType, array, index);
        }
        public void Reset(EdfType? edfType, Array array, int index)
        {
            _edfType = edfType;
            _array = array;
            _index = index;
        }

        public Type GetPropertyType() => _array.GetType().GetElementType();
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
    }
}

