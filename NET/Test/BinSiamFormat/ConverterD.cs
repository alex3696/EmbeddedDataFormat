namespace Test.BinSiamFormat;

public static class ConverterD
{
    public static int DToEdf(Stream src, Stream dst, IEdfWriter writer)
    {
        var repSize = Marshal.SizeOf<DynRepV2>();
        Span<byte> buf = stackalloc byte[repSize];
        src.ReadExactly(buf);
        var dat = MemoryMarshal.Read<DynRepV2>(buf);

        writer.WriteSchema(FileTypeId.GetEdfSchema());
        writer.WriteValue(new FileTypeId { Type = (ushort)dat.FileType, Version = 1 });

        writer.WriteSchema(DateTimeTz.GetEdfSchema());
        writer.WriteValue(DateTimeTz.FromDateTime(dat.Id.Time.Dt));


        return 0;
    }

    public static int EdfToD(Stream src, Stream dst, IEdfReader reader)
    {
        return 0;
    }
}
