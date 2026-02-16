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
        try
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
                        EdfErr err = TryReadPrimitives(out var arr, _reader.GetBlockData());
                        if (0 < arr.Count)
                        {
                            _writer.Write(arr);
                        }
                        break;
                }
            }
        }
        catch (EndOfStreamException ex)
        {

        }
        _writer.Flush();
    }

    int _skip = 0;
    int _readed = 0;
    EdfErr TryReadPrimitives(out List<object> ret, ReadOnlySpan<byte> src)
    {
        ret = [];
        ArgumentNullException.ThrowIfNull(_writer.CurrDataType);
        EdfErr err;
        do
        {
            int qty = 0;
            int skip = _skip;
            int readed = 0;

            err = PrimitiveListReader.ReadObjects(_writer.CurrDataType, src, ref skip, ref qty, ref readed, ret);
            src = src.Slice(readed);
            switch (err)
            {
                default:
                case EdfErr.WrongType: return err;
                case EdfErr.DstBufOverflow: return err;
                case EdfErr.SrcDataRequred:
                    _skip += qty;
                    _readed = 0;
                    break;
                case EdfErr.IsOk:
                    _readed += readed;
                    _skip = 0;
                    break;
            }
        }
        while (err != EdfErr.SrcDataRequred);
        return err;
    }
}
