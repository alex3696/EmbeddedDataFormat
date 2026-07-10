using EdfNet.Interfaces;
using System.Globalization;

namespace EdfNet.Ref;

public struct ObjTextEnumerator : IEdfByteEnumerator
{
    // Ссылка на итератор примитивов исходного PrimitiveDecomposer
    private readonly IEnumerator<object> _decomposerEnum;

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
            if (t.IsArray && t.GetElementType() == typeof(byte))
            {
                return PoType.Char;
            }
            return Type.GetTypeCode(t) switch
            {
                TypeCode.Byte => PoType.UInt8,
                TypeCode.SByte => PoType.Int8,
                TypeCode.UInt16 => PoType.UInt16,
                TypeCode.Int16 => PoType.Int16,
                TypeCode.UInt32 => PoType.UInt32,
                TypeCode.Int32 => PoType.Int32,
                TypeCode.UInt64 => PoType.UInt64,
                TypeCode.Int64 => PoType.Int64,
                TypeCode.Single => PoType.Single,
                TypeCode.Double => PoType.Double,
                TypeCode.String => PoType.String,
                _ => throw new Exception("Unsupported type: " + _currObj.GetType().FullName),
            };
        }
    }
    public readonly int CurrentPoLen
    {
        get
        {
            ArgumentNullException.ThrowIfNull(_currObj, nameof(_currObj));
            var t = _currObj.GetType();
            if (t.IsArray && t.GetElementType() == typeof(byte))
            {
                return (_currObj as Array)?.Length ?? 0;
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
        var decomposer = new PrimitiveDecomposer(source);
        _decomposerEnum = decomposer.GetEnumerator();
        _currentIndex = -1; // Значение -1 до первого вызова MoveNext()
    }

    public bool MoveNext()
    {
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
        ArgumentOutOfRangeException.ThrowIfLessThan(dst.Length, CurrentPoLen);
        ArgumentNullException.ThrowIfNull(_currObj, nameof(_currObj));
        var t = _currObj.GetType();
        if (t.IsArray && t.GetElementType() == typeof(byte))
        {
            var byteArray = (byte[])_currObj;
            if (dst.Length < byteArray.Length)
                return -1;
            byteArray.CopyTo(dst);
            return byteArray.Length;
        }
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
            TypeCode.String => EdfBinString.WriteBin((string?)_currObj, dst),
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
        if (obj.TryFormat(dst, out int w, default, CultureInfo.InvariantCulture))
            return w;
        return -1;
    }
}
