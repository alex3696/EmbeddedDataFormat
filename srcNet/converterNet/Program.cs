using NetEdf.src;

namespace converterNet;

internal class Program
{
    static int Main(string[] args)
    {
        if (args != null && args.Length >= 2)
        {
            switch (args[1])
            {
                case "t":
                    if (Converter.IsExt(args[0], "bdf") && Converter.ChangeExt(args[0], "tdf", out string newFileTdf))
                        return Converter.BinToText(args[0], newFileTdf);
                    break;
                case "c":
                    if (Converter.IsExt(args[0], "bdf") && Converter.ChangeExt(args[0], "csv", out string newFileCsv))
                        return Converter.BinToCsv(args[0], newFileCsv);
                    break;
            }
        }
        return 0;
    }
}
