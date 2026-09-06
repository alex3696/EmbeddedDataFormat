using System.Diagnostics.CodeAnalysis;

namespace Test.BinSiamFormat;

public static class StructSerialize
{
    public static T FromBytes<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(ReadOnlySpan<byte> rawData)
        where T : struct
    {
        int rawSize = Marshal.SizeOf<T>();
        if (rawSize > rawData.Length)
            throw new ArgumentException(
                $"Not enough data to fill struct. Span length from position: {rawData.Length}, Struct length: {rawSize}");
        byte[] buffer = ArrayPool<byte>.Shared.Rent(rawSize);
        try
        {
            rawData.CopyTo(buffer);
            return FromBytes<T>(buffer);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public static T FromBytes<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(byte[] rawData, int position = 0)
        where T : struct
    {
        int rawsize = Marshal.SizeOf<T>();
        if (rawsize > rawData.Length - position)
            throw new ArgumentException("Not enough data to fill struct. Array length from position: " + (rawData.Length - position) + ", Struct length: " + rawsize);
        GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
        try
        {
            return Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        }
        finally
        {
            handle.Free();
        }
    }
    public static byte[] ToBytes<T>(T anything)
        where T : struct
    {
        int rawSize = Marshal.SizeOf(anything);
        byte[] rawData = new byte[rawSize];
        GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
        Marshal.StructureToPtr(anything, handle.AddrOfPinnedObject(), false);
        handle.Free();
        return rawData;
        /*
        int rawSize = Marshal.SizeOf(anything);
        IntPtr buffer = Marshal.AllocHGlobal(rawSize);
        Marshal.StructureToPtr(anything, buffer, false);
        byte[] rawDatas = new byte[rawSize];
        Marshal.Copy(buffer, rawDatas, 0, rawSize);
        Marshal.FreeHGlobal(buffer);
        return rawDatas;
        */
    }




}
/*
 [StructLayout(LayoutKind.Explicit, Size = 11, Pack = 1)]
private struct MyStructType
{
    [FieldOffset(0)]
    public UInt16 Type;
    [FieldOffset(2)]
    public Byte DeviceNumber;
    [FieldOffset(3)]
    public UInt32 TableVersion;
    [FieldOffset(7)]
    public UInt32 SerialNumber;
}
 */
