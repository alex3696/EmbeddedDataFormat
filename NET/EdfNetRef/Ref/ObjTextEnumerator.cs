using EdfNet.Interfaces;
using System.Globalization;
using System.Linq;

namespace EdfNet.Ref;

public struct ObjTextEnumerator : IEdfByteEnumerator
{
    // Ссылка на итератор примитивов исходного PrimitiveDecomposer
    private readonly IEnumerator<object> _decomposerEnum;
    private readonly PrimitiveDecomposer _decomposer;

    // Скрытый счетчик для отслеживания текущего индекса примитива
    private int _currentIndex;
    private object? _currObj;

    // Контрактные свойства интерфейса
    public readonly int CurrentIndex => _currentIndex;
    public readonly PoType CurrentPoType
    {
        get
        {
            ArgumentNullException.ThrowIfNull(_currObj, nameof(_currObj));
            var t = _currObj.GetType();
            if (PoType.Char == _decomposer.DstType?.Type && typeof(byte[]) == t)
            {
                return PoType.Char;
            }
            return t.GetPoType();
        }
    }
    public readonly int CurrentPoLen
    {
        get
        {
            ArgumentNullException.ThrowIfNull(_currObj, nameof(_currObj));
            var t = _currObj.GetType();
            if (PoType.Char == _decomposer.DstType?.Type && typeof(byte[]) == t)
            {
                return _decomposer.DstType.Dims?.ElementAt(0) ?? 1;
            }
            return Type.GetTypeCode(t) switch
            {
                TypeCode.Byte => 1,
                TypeCode.SByte => 1,
                TypeCode.UInt16 => 2,
                TypeCode.Int16 => 2,
                TypeCode.UInt32 => 4,
                TypeCode.Int32 => 4,
                TypeCode.UInt64 => 8,
                TypeCode.Int64 => 8,
                TypeCode.Single => 4,
                TypeCode.Double => 8,
                TypeCode.String => EdfBinString.SizeOf((string?)_currObj),
                _ => throw new Exception("Unsupported type: " + _currObj.GetType().FullName),
            };
        }
    }

    public ObjTextEnumerator(object source)
    {
        ArgumentNullException.ThrowIfNull(source);
        _decomposer = new PrimitiveDecomposer(source);
        _decomposerEnum = _decomposer.GetEnumerator();
        _currentIndex = -1; // Значение -1 до первого вызова MoveNext()
    }

    // тут надо проанализировать
    // если  PoType.Char надо брать c# объект весь массив byte[]
    // иначе по одному элементу
    public bool MoveNext(EdfType? et = default)
    {
        _decomposer.DstType = et;
        if (_decomposerEnum.MoveNext())
        {
            _currentIndex++;
            _currObj = _decomposerEnum.Current;
            return true;
        }
        return false;
    }

    public readonly int Write(Span<byte> dst)
    {
        ArgumentNullException.ThrowIfNull(_currObj, nameof(_currObj));
        var t = _currObj.GetType();
        if (PoType.Char == _decomposer.DstType?.Type)
        {
            if (2 > dst.Length)
                return -1;
            var src = (byte[])_currObj;
            int i = 0;
            dst[0] = 34;
            var edfLen = null == _decomposer.DstType.Dims ? 0 : _decomposer.DstType.Dims[0];
            for (; i < int.Min(edfLen, src.Length); i++)
            {
                if (i + 1 > dst.Length)
                    return -1;
                if (0 == src[i])
                    break;
                dst[i + 1] = src[i];
            }
            dst[i + 1] = 34;
            return i + 2;
        }
        if (dst.Length < CurrentPoLen)
            return -1;
        return Type.GetTypeCode(t) switch
        {
            TypeCode.Byte => TryFormat((byte)_currObj, dst),
            TypeCode.SByte => TryFormat((sbyte)_currObj, dst),
            TypeCode.UInt16 => TryFormat((ushort)_currObj, dst),
            TypeCode.Int16 => TryFormat((short)_currObj, dst),
            TypeCode.UInt32 => TryFormat((uint)_currObj, dst),
            TypeCode.Int32 => TryFormat((int)_currObj, dst),
            TypeCode.UInt64 => TryFormat((ulong)_currObj, dst),
            TypeCode.Int64 => TryFormat((long)_currObj, dst),
            TypeCode.Single => TryFormat((float)_currObj, dst),
            TypeCode.Double => TryFormat((double)_currObj, dst),
            TypeCode.String => TryFormat((string?)_currObj, dst),
            _ => throw new Exception("Unsupported type: " + _currObj.GetType().FullName),
        };
    }
    public int Read(ReadOnlySpan<byte> src)
    {
        throw new NotSupportedException("Read operation is not supported in ObjByteEnumerator.");
    }
    public static int TryFormat<T>(T obj, Span<byte> dst)
        where T : IUtf8SpanFormattable
    {
        try
        {
            if (obj.TryFormat(dst, out int w, default, CultureInfo.InvariantCulture))
                return w;
            return -1;
        }
        catch (Exception)
        {
        }
        return -1;
    }
    public static int TryFormat(string? str, Span<byte> dst)
    {
        try
        {
            dst[0] = 34;
            var len = EdfBinString.CopyStringToSpan(str, dst.Slice(1, EdfBinString.MaxLen));
            dst[len + 1] = 34;
            return len + 2;
        }
        catch (Exception)
        {
        }
        return -1;
    }
}
