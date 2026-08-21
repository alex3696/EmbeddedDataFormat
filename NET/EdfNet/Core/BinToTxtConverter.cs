namespace EdfNet.Core;

public class BinToTxtConverter : BaseDisposable
{
    readonly Stream _srcFile;
    readonly Stream _dstFile;

    readonly BlockReaderBin _reader;
    readonly ConvWriterTxt _writer;

    public BinToTxtConverter(string srcBin, string dstTxt)
    {
        _srcFile = new FileStream(srcBin, FileMode.Open);
        _dstFile = new FileStream(dstTxt, FileMode.Create);

        _reader = new(_srcFile);
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
                    default: Console.WriteLine($"Block type {_reader.GetBlockType()} not supported here."); break;
                    case BlockType.Schema:
                        var rec = _reader.CurrentSchema;
                        if (rec != null)
                            _writer.WriteSchema(rec);
                        break;
                    case BlockType.Data:
                        ArgumentNullException.ThrowIfNull(_writer.CurrentSchema?.Type);
                        Convert(new BufReaderBin(_reader.State), new BufWriterTxt(_writer.State));
                        break;
                }
            }
        }
        catch (EndOfStreamException)
        {

        }
        _writer.Flush();
    }
    private void Convert<TReader, TWriter>(TReader br, TWriter bw)
        where TReader : IBufReader, allows ref struct
        where TWriter : IBufWriter, allows ref struct
    {
        while (0 < _reader.State.ReadAvailableLen)
        {
            var et = _reader.State.Enum.CurrentType;
            switch (et.Type)
            {
                default:
                case PoType.Struct: throw new EdfWrongTypeException();
                case PoType.UInt8: bw.Write(br.Read<byte>()); break;
                case PoType.Int8: bw.Write(br.Read<sbyte>()); break;
                case PoType.UInt16: bw.Write(br.Read<ushort>()); break;
                case PoType.Int16: bw.Write(br.Read<short>()); break;
                case PoType.UInt32: bw.Write(br.Read<uint>()); break;
                case PoType.Int32: bw.Write(br.Read<int>()); break;
                case PoType.UInt64: bw.Write(br.Read<ulong>()); break;
                case PoType.Int64: bw.Write(br.Read<long>()); break;
                case PoType.Single: bw.Write(br.Read<float>()); break;
                case PoType.Double: bw.Write(br.Read<double>()); break;
                case PoType.String: bw.Write(br.ReadString()); break;
                case PoType.Char: bw.WriteCharArray(br.ReadCharArray()); break;
            }
        }
    }
}
