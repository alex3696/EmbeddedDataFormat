
internal class Program
{
    static int UseStreams(string srcFile, string dstFile
        , Func<IEdfReader, Stream, int> func, Func<Stream, IEdfReader> readerFactory)
    {
        using var srcStrteam = new FileStream(srcFile, FileMode.Open, FileAccess.Read);
        using var dstStream = new FileStream(dstFile, FileMode.Create, FileAccess.Write);
        var reader = readerFactory.Invoke(srcStrteam);
        if (reader is IDisposable d)
        {
            using (d)
            {
                return func.Invoke(reader, dstStream);
            }
        }
        else
        {
            return func.Invoke(reader, dstStream);
        }
    }
    static int UseStreams(string srcFile, string dstFile
        , Func<Stream, IEdfWriter, int> func, Func<Stream, IEdfWriter> writerFactory)
    {
        using var srcStrteam = new FileStream(srcFile, FileMode.Open, FileAccess.Read);
        using var dstStream = new FileStream(dstFile, FileMode.Create, FileAccess.Write);
        var writer = writerFactory.Invoke(dstStream);
        if (writer is IDisposable d)
        {
            using (d)
            {
                return func.Invoke(srcStrteam, writer);
            }
        }
        else
        {
            return func.Invoke(srcStrteam, writer);
        }
    }
    static int UseStreams(string srcFile, string dstFile, Action<Stream, Stream> func)
    {
        using var srcStream = new FileStream(srcFile, FileMode.Open, FileAccess.Read);
        using var dstStream = new FileStream(dstFile, FileMode.Create, FileAccess.Write);
        func.Invoke(srcStream, dstStream);
        return 0;
    }
    static Func<Stream, IEdfWriter> MakeWriter(string ext)
    {
        return ext switch
        {
            ".bdf" => st => new EdfBinaryWriter(st),
            _ => st => new EdfTextWriter(st),
        };
    }
    static int ConvertToEdf(string srcFile, string dstFile)
    {
        var srcExt = Path.GetExtension(srcFile).ToLower();
        var dstExt = Path.GetExtension(dstFile).ToLower();
        if (0 != dstExt.CompareTo(dstExt))
            throw new ConvertException($"Same extension {srcExt}");
        return srcExt switch
        {
            ".bdf" => UseStreams(srcFile, dstFile, BinToTxt.Convert),
            ".tdf" => UseStreams(srcFile, dstFile, TxtToBin.Convert),
            ".dat" => UseStreams(srcFile, dstFile, ConverterDat.DatToEdf, MakeWriter(dstExt)),
            ".d" => UseStreams(srcFile, dstFile, ConverterD.DToEdf, MakeWriter(dstExt)),
            ".e" => UseStreams(srcFile, dstFile, ConverterE.EToEdf, MakeWriter(dstExt)),
            _ => throw new ConvertException($"Unknow extension {srcExt}"),
        };
    }
    static int ConvertToSiam(string srcFile, string dstFile, Func<IEdfReader, Stream, int> factory)
    {
        var ext = Path.GetExtension(srcFile).ToLower();
        return ext switch
        {
            ".bdf" => UseStreams(srcFile, dstFile, factory, st => new EdfBinaryReader(st)),
            ".tdf" => UseStreams(srcFile, dstFile, factory, st => new EdfTextReader(st)),
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
                case "t": return ConvertToEdf(srcFile, Path.ChangeExtension(srcFile, ".tdf"));
                case "b": return ConvertToEdf(srcFile, Path.ChangeExtension(srcFile, ".bdf"));
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
