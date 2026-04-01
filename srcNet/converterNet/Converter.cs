using NetEdf.src;
using System.IO;

namespace converterNet;

public class Converter
{
    public static bool IsExt(string file, string ext)
    {
        int fileLen = file.Length;
        int extLen = ext.Length;
        var fileExt = file.Substring(fileLen - extLen);
        return fileExt == ext;
    }
    public static bool ChangeExt(string file, string ext, out string newfile)
    {
        var oldExt = Path.GetExtension(file);
        newfile = file.Replace(oldExt, "."+ext);
        return true;
    }

    public static int BinToText(string file, string newFile)
    {
        using var convert = new BinToTxtConverter(file, newFile);
        convert.Execute();
        return 1;
    }

    public static int BinToCsv(string file, string newFile)
    {
        using var convert = new BinToCsvConverter(file, newFile);
        convert.Execute();
        return 1;
    }
}
