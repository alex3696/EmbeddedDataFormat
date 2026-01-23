using NetEdf.StoreTypes;

namespace NetEdf.Base;

public static class StructSerialize
{
    public static T FromBytes2<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] T>(byte[] rawData, int position = 0)
        where T : struct
    {
        int rawsize = Marshal.SizeOf<T>();
        if (rawsize > rawData.Length - position)
            throw new ArgumentException("Not enough data to fill struct. Array length from position: " + (rawData.Length - position) + ", Struct length: " + rawsize);

        GCHandle handle = GCHandle.Alloc(rawData, GCHandleType.Pinned);
        T retobj = Marshal.PtrToStructure<T>(handle.AddrOfPinnedObject());
        handle.Free();
        /*
        IntPtr buffer = Marshal.AllocHGlobal(rawsize);
        Marshal.Copy(rawData, position, buffer, rawsize);
        T retobj = default;
        Marshal.PtrToStructure<T>(buffer, retobj);
        Marshal.FreeHGlobal(buffer);
        
        */
        return retobj;
    }
    public static byte[] ToBytes2<T>(T anything)
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


    public static T FromBytes<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(ReadOnlySpan<byte> b)
        where T : struct
    {
        T ret = default;

        var props = typeof(T).GetFields(BindingFlags.Public | BindingFlags.Instance) ?? [];
        foreach (var prop in props)
        {
            if (TryGetValue(prop.FieldType, b, out object? obj, out int len))
            {
                prop.SetValue(ret, obj);
                b = b.Slice(len);
            }
        }
        return ret;

    }
    public static byte[] ToBytes<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] T>(T obj)
        where T : struct
    {
        int size = 0;
        List<byte[]> items = [];
        Type tt = typeof(T);
        var props = tt.GetFields(BindingFlags.Public | BindingFlags.Instance);
        foreach (var prop in props)
        {
            var item = GetBytes(prop.FieldType, prop.GetValue(obj));
            items.Add(item);
            size += item.Length;
        }
        byte[] b = new byte[size];
        size = 0;
        foreach (var item in items)
        {
            item.CopyTo(b, size);
            size += item.Length;
        }
        return b;
    }

    public static bool TryGetValue(Type t, ReadOnlySpan<byte> b, out object? obj, out int len)
    {

        switch (Type.GetTypeCode(t))
        {
            default: break;

            case TypeCode.Byte: obj = b[0]; len = 1; return true;
            case TypeCode.SByte: obj = (sbyte)b[0]; len = 1; return true;

            case TypeCode.Int16: obj = BinaryPrimitives.ReadInt16LittleEndian(b); len = 2; return true;
            case TypeCode.UInt16: obj = BinaryPrimitives.ReadUInt16LittleEndian(b); len = 2; return true;
            case TypeCode.Int32: obj = BinaryPrimitives.ReadInt32LittleEndian(b); len = 4; return true;
            case TypeCode.UInt32: obj = BinaryPrimitives.ReadUInt32LittleEndian(b); len = 4; return true;
            case TypeCode.Int64: obj = BinaryPrimitives.ReadInt64LittleEndian(b); len = 8; return true;
            case TypeCode.UInt64: obj = BinaryPrimitives.ReadUInt64LittleEndian(b); len = 8; return true;

            //case TypeCode.Half:
            case TypeCode.Single: obj = BinaryPrimitives.ReadSingleLittleEndian(b); len = 4; return true;
            case TypeCode.Double: obj = BinaryPrimitives.ReadDoubleLittleEndian(b); len = 8; return true;

            case TypeCode.String:
                if (BString.TryParse(b, out string? str, out len))
                {
                    obj = str;
                    return true;
                }
                break;
        }
        len = 0;
        obj = null;
        return false;

    }
    public static byte[] GetBytes(Type t, object? val)
    {
        switch (val)
        {
            default: break;
            case byte u8: return [u8];
            case sbyte i8: return [(byte)i8];

            case ushort u16: return BitConverter.GetBytes(u16);
            case short i16: return BitConverter.GetBytes(i16);
            case uint u32: return BitConverter.GetBytes(u32);
            case int i32: return BitConverter.GetBytes(i32);
            case ulong u64: return BitConverter.GetBytes(u64);
            case long i64: return BitConverter.GetBytes(i64);

            case Half h: return BitConverter.GetBytes(h);
            case float s: return BitConverter.GetBytes(s);
            case double d: return BitConverter.GetBytes(d);

            case string str: return BString.GetBytes(str);
        }
        if (null == val)
        {
            Type? nut = Nullable.GetUnderlyingType(t);
            if (nut is not null)
                t = nut;
            if (Equals(t, typeof(string)))
            {
                return [0];
            }
        }
        return [];
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
