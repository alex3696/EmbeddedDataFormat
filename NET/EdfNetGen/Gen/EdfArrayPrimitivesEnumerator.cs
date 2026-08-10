namespace EdfNet.Gen;

public struct EdfArrayPrimitivesEnumerator<T> : IEdfByteEnumerator
    where T : struct, IUtf8SpanFormattable
{
    private readonly Array _array;
    private int _index;
    private readonly int _totalLength;
    private readonly PoType _poType;

    public EdfArrayPrimitivesEnumerator(Array array, PoType poType)
    {
        _array = array;
        _poType = poType;
        _index = -1;
        _totalLength = array?.Length ?? 0;
    }
    public bool MoveNext(EdfType et = default!)
    {
        if (_index >= _totalLength - 1) return false;
        _index++;
        return true;
    }

    public readonly int CurrentIndex => _index;
    public readonly PoType CurrentPoType => _poType;
    public readonly int CurrentPoLen => _poType.GetSizeOf();

    public readonly int Write(Span<byte> dst)
    {
        int elementSize = Unsafe.SizeOf<T>();
        if (elementSize > dst.Length)
            return -1;
        ref byte byteRoot = ref MemoryMarshal.GetArrayDataReference(_array);
        ref T firstElement = ref Unsafe.As<byte, T>(ref byteRoot);
        ref T elementRef = ref Unsafe.Add(ref firstElement, _index);
        MemoryMarshal.Write(dst, in elementRef);
        return elementSize;
    }
    public readonly int Read(ReadOnlySpan<byte> src)
    {
        int elementSize = Unsafe.SizeOf<T>();
        ref byte byteRoot = ref MemoryMarshal.GetArrayDataReference(_array);
        ref byte targetByteRef = ref Unsafe.Add(ref byteRoot, _index * elementSize);
        ref T elementRef = ref Unsafe.As<byte, T>(ref targetByteRef);
        elementRef = MemoryMarshal.Read<T>(src);
        return elementSize;
    }
    public readonly int WriteTxt(Span<byte> dst)
    {
        int elementSize = Unsafe.SizeOf<T>();
        if (elementSize > dst.Length)
            return -1;
        ref byte byteRoot = ref MemoryMarshal.GetArrayDataReference(_array);
        ref T firstElement = ref Unsafe.As<byte, T>(ref byteRoot);
        ref T elementRef = ref Unsafe.Add(ref firstElement, _index);
        return PrimitiveWritersTxt.TryWrite(dst, elementRef);
    }
    public int ReadTxt(ReadOnlySpan<byte> src)
    {
        var len = PrimitiveWritersTxt.TryRead(src, out T? val);
        if (0 > len)
            return -1;
        int elementSize = Unsafe.SizeOf<T>();
        ref byte byteRoot = ref MemoryMarshal.GetArrayDataReference(_array);
        ref byte targetByteRef = ref Unsafe.Add(ref byteRoot, _index * elementSize);
        ref T elementRef = ref Unsafe.As<byte, T>(ref targetByteRef);
        elementRef = val.GetValueOrDefault();
        return len;
    }
}
