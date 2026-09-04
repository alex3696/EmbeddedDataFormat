namespace Test.BinSiamFormat;

public static class ConverterD
{
    public static int DToEdf(Stream src, Stream dst, IEdfWriter writer)
    {
        Span<byte> buf = stackalloc byte[Marshal.SizeOf<DynRepV2>()];
        src.ReadExactly(buf);
        var rep = StructSerialize.FromBytes<DynRepV2>(buf);

        writer.WriteSchema(FileTypeId.GetEdfSchema());
        writer.WriteValue(new FileTypeId { Type = (ushort)rep.FileType, Version = 1 });



        return 0;
    }

    public static int EdfToD(Stream src, Stream dst, IEdfReader reader)
    {
        return 0;
    }
}
