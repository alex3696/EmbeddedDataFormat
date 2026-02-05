using NetEdf.Base;
using System.Reflection.PortableExecutable;

namespace NetEdf.src;


public class BinToTxtConverter: BaseDisposable
{
    readonly Stream _srcFile;
    readonly Stream _dstFile;
    readonly BaseReader _reader;
    readonly BaseWriter _writer;

    public BinToTxtConverter(string srcBin, string dstTxt)
    {
        _srcFile = new FileStream(srcBin, FileMode.Open);
        _dstFile = new FileStream(dstTxt, FileMode.Create);
        _reader = new BinReader(_srcFile);
        _writer= new BinWriter(_dstFile);
    }
    public void Execute()
    {
        throw new NotImplementedException();
    }
}
