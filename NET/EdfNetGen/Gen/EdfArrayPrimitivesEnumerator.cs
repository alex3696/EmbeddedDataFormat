namespace EdfNet.Gen;

public struct EdfArrayPrimitivesEnumerator<T> : IEdfByteEnumerator
    where T : struct // unmanaged заменили на struct, так как мы safe
{
    private readonly Array _array;
    private int _index;
    private readonly int _totalLength;
    private readonly PoType _poType;

    // Массив одномерных координат для GetValue/SetValue, 
    // чтобы не выделять его в цикле, рассчитываем индексы математически
    //private readonly int[] _indices;
    //private readonly int[] _dims;

    public EdfArrayPrimitivesEnumerator(Array array, PoType poType)
    {
        _array = array;
        _poType = poType;
        _index = -1;
        _totalLength = array?.Length ?? 0;

        //if (array != null)
        //{
        //    _indices = new int[array.Rank];
        //    _dims = new int[array.Rank];
        //    for (int i = 0; i < array.Rank; i++)
        //    {
        //        _dims[i] = array.GetLength(i);
        //    }
        //}
        //else
        //{
        //    _indices = [];
        //    _dims = [];
        //}
    }
    public bool MoveNext(EdfType et = default!)
    {
        if (_index >= _totalLength - 1) return false;
        _index++;

        // Математически пересчитываем плоский _index в многомерные координаты
        // Например, для [3, 2, 1] и плоского индекса 3 координаты будут [1, 1, 0]
        //int remainder = _index;
        //for (int i = _dims.Length - 1; i >= 0; i--)
        //{
        //    _indices[i] = remainder % _dims[i];
        //    remainder /= _dims[i];
        //}
        return true;
    }

    public readonly int CurrentIndex => _index;
    public readonly PoType CurrentPoType => _poType;
    public readonly int CurrentPoLen => _poType.GetSizeOf();


    public readonly int Write(Span<byte> destination)
    {
        if (_array == null)
            return 0;
        int elementSize = _poType.GetSizeOf();
        // 1. Используем не-generic перегрузку для Array! Она возвращает ref byte на начало данных.
        ref byte byteRoot = ref MemoryMarshal.GetArrayDataReference(_array);
        // 2. Считаем точное смещение в байтах для нужного элемента многомерного массива
        int byteOffset = _index * elementSize;
        // 3. Сдвигаемся по байтам от начала массива
        ref byte targetByteRef = ref Unsafe.Add(ref byteRoot, byteOffset);
        // 4. Магия реинтерпретации памяти: превращаем ref byte в ref T (No boxing, no unsafe!)
        ref T elementRef = ref Unsafe.As<byte, T>(ref targetByteRef);
        // 5. Записываем в Span
        MemoryMarshal.Write(destination, in elementRef);
        return elementSize;
    }
    public readonly int Read(ReadOnlySpan<byte> src)
    {
        if (_array == null) return 0;
        int elementSize = _poType.GetSizeOf();
        int byteOffset = _index * elementSize;
        // 1. Получаем ссылку на первый байт массива
        ref byte byteRoot = ref MemoryMarshal.GetArrayDataReference(_array);
        // 2. Сдвигаемся на нужный байт
        ref byte targetByteRef = ref Unsafe.Add(ref byteRoot, byteOffset);
        // 3. Превращаем в ref T
        ref T elementRef = ref Unsafe.As<byte, T>(ref targetByteRef);
        // 4. Прямая запись из файла в ячейку памяти многомерного массива
        elementRef = MemoryMarshal.Read<T>(src);
        return elementSize;
    }

}
