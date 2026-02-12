using NetEdf.Base;

namespace NetEdf.src;


public class BinToTxtConverter : BaseDisposable
{
    readonly Stream _srcFile;
    readonly Stream _dstFile;
    readonly BinReader _reader;
    readonly TxtWriter _writer;

    public BinToTxtConverter(string srcBin, string dstTxt)
    {
        _srcFile = new FileStream(srcBin, FileMode.Open);
        _dstFile = new FileStream(dstTxt, FileMode.Create);
        _reader = new BinReader(_srcFile);
        _writer = new TxtWriter(_dstFile);
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _reader.Dispose();
            _writer.Dispose();
            _srcFile.Dispose();
            _dstFile.Dispose();
        }
    }
    public void Execute()
    {
        while (_reader.ReadBlock())
        {
            switch (_reader.GetBlockType())
            {
                default: break;
                case BlockType.Header:
                    var header = _reader.ReadHeader();
                    if (header != null)
                        _writer.Write(header);
                    break;
                case BlockType.VarInfo:
                    var rec = _reader.ReadInfo();
                    if (rec != null)
                        _writer.Write(rec);
                    break;
                case BlockType.VarData:
                    var readed = _reader.TryRead(out object[]? arr);
                    if (arr != null && 0 < arr.Length)
                    {
                        _writer.Write(arr);
                    }
                    break;
            }
            _writer.Flush();
        }
    }
}
