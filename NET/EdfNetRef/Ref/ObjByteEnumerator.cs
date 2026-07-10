using EdfNet.Interfaces;
using System.Security.Cryptography;

namespace EdfNet.Ref;

public struct ObjByteEnumerator : IEdfByteEnumerator
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

    public ObjByteEnumerator(object source)
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

    public int Write(Span<byte> dst)
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
        switch (Type.GetTypeCode(t))
        {
            default: throw new Exception("Unsupported type: " + _currObj.GetType().FullName);
            case TypeCode.Byte: MemoryMarshal.Write<byte>(dst, (byte)_currObj); return 1;
            case TypeCode.SByte: MemoryMarshal.Write<sbyte>(dst, (sbyte)_currObj); return 1;
            case TypeCode.UInt16: MemoryMarshal.Write<ushort>(dst, (ushort)_currObj); return 2;
            case TypeCode.Int16: MemoryMarshal.Write<short>(dst, (short)_currObj); return 2;
            case TypeCode.UInt32: MemoryMarshal.Write<uint>(dst, (uint)_currObj); return 4;
            case TypeCode.Int32: MemoryMarshal.Write<int>(dst, (int)_currObj); return 4;
            case TypeCode.UInt64: MemoryMarshal.Write<ulong>(dst, (ulong)_currObj); return 8;
            case TypeCode.Int64: MemoryMarshal.Write<long>(dst, (long)_currObj); return 8;
            case TypeCode.Single: MemoryMarshal.Write<float>(dst, (float)_currObj); return 4;
            case TypeCode.Double: MemoryMarshal.Write<double>(dst, (double)_currObj); return 8;
            case TypeCode.String: return EdfBinString.WriteBin((string?)_currObj, dst);
        }
    }
    public int Read(ReadOnlySpan<byte> src)
    {
        throw new NotSupportedException("Read operation is not supported in ObjByteEnumerator.");
    }

}
