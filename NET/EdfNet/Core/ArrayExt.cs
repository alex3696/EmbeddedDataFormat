namespace EdfNet.Core;

public static class ArrayExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetElementAtFlatIndex<T>(this Array arr, int flatIndex)
    {
        //ref byte byteRoot = ref MemoryMarshal.GetArrayDataReference(_array);
        //ref byte targetByteRef = ref Unsafe.Add(ref byteRoot, _index * elementSize);
        //ref T elementRef = ref Unsafe.As<byte, T>(ref targetByteRef);
        if (flatIndex < 0 || flatIndex >= arr.Length)
            throw new ArgumentOutOfRangeException(paramName: nameof(flatIndex));
        ref byte byteRoot = ref MemoryMarshal.GetArrayDataReference(arr);
        ref T firstElement = ref Unsafe.As<byte, T>(ref byteRoot);
        return ref Unsafe.Add(ref firstElement, flatIndex);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ref T GetElementAtFlatIndexUnsafe<T>(this Array arr, int flatIndex)
    {
        ref byte byteRoot = ref MemoryMarshal.GetArrayDataReference(arr);
        ref T firstElement = ref Unsafe.As<byte, T>(ref byteRoot);
        return ref Unsafe.Add(ref firstElement, flatIndex);
    }
}
