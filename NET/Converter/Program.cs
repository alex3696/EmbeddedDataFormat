
internal class Program
{
    static int UseStreams<T>(string srcFile, string dstFile
        , Func<Stream, Stream, T, int> func, Func<Stream, T> factory)
    {
        using var src = new FileStream(srcFile, FileMode.Open, FileAccess.Read);
        using var dst = new FileStream(dstFile, FileMode.Create, FileAccess.Write);
        return func.Invoke(src, dst, factory.Invoke(dst));
    }
    static int UseStreams(string srcFile, string dstFile, Action<Stream, Stream> func)
    {
        using var src = new FileStream(srcFile, FileMode.Open, FileAccess.Read);
        using var dst = new FileStream(dstFile, FileMode.Create, FileAccess.Write);
        func.Invoke(src, dst);
        return 0;
    }
    static int ConvertToEdf(string srcFile, string dstFile, Func<Stream, IEdfWriter> factory)
    {
        var srcExt = Path.GetExtension(srcFile).ToLower();
        var dstExt = Path.GetExtension(dstFile).ToLower();
        if (0 != dstExt.CompareTo(dstExt))
            throw new ConvertException($"Same extension {srcExt}");
        return srcExt switch
        {
            ".bdf" => UseStreams(srcFile, dstFile, BinToTxt.Convert),
            ".tdf" => UseStreams(srcFile, dstFile, TxtToBin.Convert),
            ".dat" => UseStreams(srcFile, dstFile, ConverterDat.DatToEdf, factory),
            ".d" => UseStreams(srcFile, dstFile, ConverterD.DToEdf, factory),
            ".e" => UseStreams(srcFile, dstFile, ConverterE.EToEdf, factory),
            _ => throw new ConvertException($"Unknow extension {srcExt}"),
        };
    }
    static int ConvertToSiam(string srcFile, string dstFile, Func<Stream, Stream, IEdfReader, int> func)
    {
        var ext = Path.GetExtension(srcFile).ToLower();
        return ext switch
        {
            ".bdf" => UseStreams(srcFile, dstFile, func, st => new EdfBinaryReader(st)),
            ".tdf" => UseStreams(srcFile, dstFile, func, st => new EdfTextReader(st)),
            _ => throw new ConvertException($"Unknow extension {ext}"),
        };
    }

    public static int Main(string[] args)
    {
        try
        {
            string srcFile = args[0];
            if (!File.Exists(srcFile))
                throw new ConvertException($"File not exist {srcFile}");
            switch (args[1].ToLower())
            {
                case "t": return ConvertToEdf(srcFile, Path.ChangeExtension(srcFile, ".tdf"), st => new EdfTextWriter(st));
                case "b": return ConvertToEdf(srcFile, Path.ChangeExtension(srcFile, ".bdf"), st => new EdfBinaryWriter(st));
                case "dat": return ConvertToSiam(srcFile, Path.ChangeExtension(srcFile, ".dat"), ConverterDat.EdfToDat);
                case "e": return ConvertToSiam(srcFile, Path.ChangeExtension(srcFile, ".e"), ConverterE.EdfToE);
                case "d": return ConvertToSiam(srcFile, Path.ChangeExtension(srcFile, ".d"), ConverterD.EdfToD);
                default: break;
            }
            throw new ConvertException($"Unknow command {args[1].ToLower()}");
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
        return -1;
    }
}
