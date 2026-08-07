namespace EdfNet.Core;

public class BinToTxtConverter : BaseDisposable
{
    readonly Stream _srcFile;
    readonly Stream _dstFile;

    readonly BlockReaderBin _reader;
    readonly ConvWriterTxt _writer;
    readonly RecursiveWriterBinToTxt _conv;

    public BinToTxtConverter(string srcBin, string dstTxt)
    {
        _srcFile = new FileStream(srcBin, FileMode.Open);
        _dstFile = new FileStream(dstTxt, FileMode.Create);

        _reader = new(_srcFile);
        _conv = new(_dstFile);
        _writer = new(_dstFile, _reader.Cfg);
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing)
        {
            _srcFile.Dispose();
            _dstFile.Dispose();
        }
    }
    public void Execute()
    {
        try
        {
            while (_reader.ReadBlock())
            {
                switch (_reader.GetBlockType())
                {
                    default: break;
                    case BlockType.Config:
                        var header = _reader.ReadConfig();
                        if (header != null)
                            _writer.Write(header);
                        break;
                    case BlockType.Schema:
                        var rec = _reader.ReadSchema();
                        if (rec != null)
                            _writer.Write(rec);
                        break;
                    case BlockType.Data:
                        ArgumentNullException.ThrowIfNull(_writer.CurrentSchema?.Type);
                        _conv.DoWrite(_reader.CurrentSchema.Type, _reader);
                        break;
                }
            }
        }
        catch (EndOfStreamException)
        {

        }
        _writer.Flush();
    }

}
