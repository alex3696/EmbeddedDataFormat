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
    static int ConvertEdf(string srcFile, string dstFile, Func<Stream, IEdfWriter> factory)
    {
        var ext = Path.GetExtension(srcFile).ToLower();
        switch (ext)
        {
            default: Console.WriteLine($"Unknow extension {ext}"); return -1;
            case ".bdf": UseStreams(srcFile, dstFile, BinToTxt.Convert); break;
            case ".tdf": UseStreams(srcFile, dstFile, TxtToBin.Convert); break;
            case ".dat": UseStreams(srcFile, dstFile, ConverterDat.DatToEdf, factory); break;
            case ".d": UseStreams(srcFile, dstFile, ConverterD.DToEdf, factory); break;
            case ".e": UseStreams(srcFile, dstFile, ConverterE.EToEdf, factory); break;
        }
        return 0;
    }
    static int ConvertEdfToSiam(string srcFile, string dstFile, Func<Stream, Stream, IEdfReader, int> func)
    {
        var ext = Path.GetExtension(srcFile).ToLower();
        switch (ext)
        {
            default: Console.WriteLine($"Unknow extension {ext}"); return -1;
            case ".bdf": return UseStreams(srcFile, dstFile, func, st => new EdfBinaryReader(st));
            case ".tdf": return UseStreams(srcFile, dstFile, func, st => new EdfTextReader(st));
        }
    }

    public static int Main(string[] args)
    {
        string srcFile = args[0];
        if (!File.Exists(srcFile))
        {
            Console.WriteLine($"File not exist {srcFile}");
            return -1;
        }
        switch (args[1].ToLower())
        {
            case "t": return ConvertEdf(srcFile, Path.ChangeExtension(srcFile, ".tdf"), st => new EdfTextWriter(st));
            case "b": return ConvertEdf(srcFile, Path.ChangeExtension(srcFile, ".bdf"), st => new EdfBinaryWriter(st));
            case "dat": return ConvertEdfToSiam(srcFile, Path.ChangeExtension(srcFile, ".dat"), ConverterDat.EdfToDat);
            case "e": return ConvertEdfToSiam(srcFile, Path.ChangeExtension(srcFile, ".e"), ConverterE.EdfToE);
            case "d": return ConvertEdfToSiam(srcFile, Path.ChangeExtension(srcFile, ".d"), ConverterD.EdfToD);
            default: break;
        }
        Console.WriteLine($"Unknow parametr {args[1]}");
        return -1;
    }
}
