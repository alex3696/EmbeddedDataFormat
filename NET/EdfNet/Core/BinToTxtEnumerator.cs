using System.Globalization;

namespace EdfNet.Core;

public struct BinToTxtEnumerator : IEdfByteEnumerator
{
    private int _currentIndex;

    private EdfType? _currentType;
    private Memory<byte> _src;

    public readonly int CurrentIndex => _currentIndex;
    public readonly PoType CurrentPoType
    {
        get
        {
            ArgumentNullException.ThrowIfNull(_currentType, nameof(_currentType));
            return _currentType.Type;
        }
    }
    public readonly int CurrentPoLen
    {
        get
        {
            ArgumentNullException.ThrowIfNull(_currentType, nameof(_currentType));
            return _currentType.Type switch
            {
                PoType.UInt8 => 1,
                PoType.Int8 => 1,
                PoType.UInt16 => 2,
                PoType.Int16 => 2,
                PoType.UInt32 => 4,
                PoType.Int32 => 4,
                PoType.UInt64 => 8,
                PoType.Int64 => 8,
                PoType.Single => 4,
                PoType.Double => 8,
                PoType.Char => (int)_currentType.GetTotalElements(),
                PoType.String => EdfBinString.SizeOf(_src.Span),
                _ => throw new Exception("Unsupported type: " + _currentType.Type),
            };
        }
    }

    public BinToTxtEnumerator(Memory<byte> src)
    {
        _src = src;
        _currentIndex = -1;
    }

    public bool MoveNext(EdfType? et = default)
    {
        _currentType = et;
        _currentIndex++;
        _src = _src[CurrentPoLen..];
        return 0 < _src.Length;
    }

    public readonly int Write(Span<byte> dst)
    {
        ArgumentNullException.ThrowIfNull(_currentType, nameof(_currentType));

        if (PoType.Char == _currentType?.Type)
        {
            if (2 > dst.Length)
                return -1;
            var src = _src.Span;
            int i = 0;
            dst[0] = 34;
            var edfLen = (int)_currentType.GetTotalElements();
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
        switch (_currentType?.Type)
        {
            default: throw new Exception("Unsupported type: " + _currentType);
            case PoType.UInt8: return TryFormat((byte)_src.Span[0], dst);
            case PoType.Int8: return TryFormat((sbyte)_src.Span[0], dst);
            case PoType.UInt16: return TryFormat(MemoryMarshal.Read<ushort>(_src.Span), dst);
            case PoType.Int16: return TryFormat(MemoryMarshal.Read<short>(_src.Span), dst);
            case PoType.UInt32: return TryFormat(MemoryMarshal.Read<uint>(_src.Span), dst);
            case PoType.Int32: return TryFormat(MemoryMarshal.Read<int>(_src.Span), dst);
            case PoType.UInt64: return TryFormat(MemoryMarshal.Read<ulong>(_src.Span), dst);
            case PoType.Int64: return TryFormat(MemoryMarshal.Read<long>(_src.Span), dst);
            case PoType.Single: return TryFormat(MemoryMarshal.Read<float>(_src.Span), dst);
            case PoType.Double: return TryFormat(MemoryMarshal.Read<double>(_src.Span), dst);
            case PoType.String:
                {
                    var byteLen = EdfBinString.SizeOf(_src.Span);
                    if (dst.Length < byteLen + 2)
                        return -1;
                    dst[0] = 34;
                    dst[byteLen + 1] = 34;
                    _src.Span.Slice(1, byteLen).CopyTo(dst.Slice(1, byteLen));
                    return byteLen + 2;
                }
        }
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
