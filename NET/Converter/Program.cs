
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
            ".tdf" => st => new EdfTextWriter(st),
            _ => throw new ConvertException($"Wrong destination type {ext}"),
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
            _ => throw new ConvertException($"Unknow source extension {srcExt}"),
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
            if (2 > args.Length)
                throw new ConvertException($"argument 2 required ");
            string srcFile = args[0];
            if (!File.Exists(srcFile))
                throw new ConvertException($"File not exist {srcFile}");

            string dstFile;
            string dstType;
            if (2 == args.Length)
            {
                dstType = args[1].ToLower();
                switch (dstType)
                {
                    case "t": dstFile = Path.ChangeExtension(srcFile, ".tdf"); break;
                    case "b": dstFile = Path.ChangeExtension(srcFile, ".bdf"); break;
                    case "dat": dstFile = Path.ChangeExtension(srcFile, ".dat"); break;
                    case "e": dstFile = Path.ChangeExtension(srcFile, ".e"); break;
                    case "d": dstFile = Path.ChangeExtension(srcFile, ".d"); break;
                    default:
                        {
                            dstFile = args[1];
                            dstType = "";
                            var dstExt = Path.GetExtension(dstFile).ToLower();
                            switch (dstExt)
                            {
                                case ".tdf": dstType = "t"; break;
                                case ".bdf": dstType = "b"; break;
                                case ".dat": dstType = "dat"; break;
                                case ".e": dstType = "e"; break;
                                case ".d": dstType = "d"; break;
                                default: break;
                            }
                        }
                        break;
                }
            }
            else //if (2 < args.Length)
            {
                dstType = args[2];
                dstFile = args[1];
            }

            switch (dstType)
            {
                case "t": return ConvertToEdf(srcFile, dstFile);
                case "b": return ConvertToEdf(srcFile, dstFile);
                case "dat": return ConvertToSiam(srcFile, dstFile, ConverterDat.EdfToDat);
                case "e": return ConvertToSiam(srcFile, dstFile, ConverterE.EdfToE);
                case "d": return ConvertToSiam(srcFile, dstFile, ConverterD.EdfToD);
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
