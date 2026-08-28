using EdfNet.Core.Binary;
using EdfNet.Core.Text;

namespace EdfNet.Converters;

public class BinToTxt
{
    class StateWriterTxt : EdfTextWriter
    {
        public StateWriterTxt(Stream stream, EdfConfig? cfg = default)
            : base(stream, cfg)
        {
        }
        public TextCircularEdfTypeEnumerator Enum => _enum;
        public BufferedTextWriter Writer => _textWriter;
    }
    class StateReaderBin : EdfBinaryReader
    {
        public StateReaderBin(Stream stream, EdfConfig? cfg = default)
            : base(stream, cfg)
        {
        }
        public BufStateBin State => _state;
    }

    public static void Convert(string srcBin, string dstTxt)
    {
        using var src = new FileStream(srcBin, FileMode.Open, FileAccess.Read);
        using var dst = new FileStream(dstTxt, FileMode.Create, FileAccess.Write);
        Convert(src, dst);
    }

    public static void Convert(Stream srcBin, Stream dstTxt)
    {
        Span<byte> buf = stackalloc byte[256];
        StateReaderBin reader = new(srcBin);
        using StateWriterTxt writer = new(dstTxt, reader.Cfg);
        try
        {
            while (reader.ReadBlock())
            {
                switch (reader.GetBlockType())
                {
                    default: Console.WriteLine($"Block type {reader.GetBlockType()} not supported here."); break;
                    case EdfBlockType.Schema:
                        var rec = reader.CurrentSchema;
                        if (rec != null)
                            writer.WriteSchema(rec);
                        break;
                    case EdfBlockType.Data:
                        ArgumentNullException.ThrowIfNull(writer.CurrentSchema?.Type);
                        var br = new BufReaderBin(reader.State);
                        var tw = new BufWriterTxt(writer.Writer, writer.Enum);
                        while (0 < reader.State.ReadAvailableLen)
                        {
                            Convert(new BufReaderBin(reader.State),
                                    new BufWriterTxt(writer.Writer, writer.Enum),
                                    buf);
                        }
                        break;
                }
            }
        }
        catch (EndOfStreamException)
        {

        }
        writer.Flush();
    }
    private static void Convert(BufReaderBin br, BufWriterTxt bw, Span<byte> buf)
    {
        switch (br.CurrentType.Type)
        {
            default:
            case EdfPrimitiveType.Struct: throw new EdfWrongTypeException();
            case EdfPrimitiveType.UInt8: bw.Write(br.Read<byte>()); break;
            case EdfPrimitiveType.Int8: bw.Write(br.Read<sbyte>()); break;
            case EdfPrimitiveType.UInt16: bw.Write(br.Read<ushort>()); break;
            case EdfPrimitiveType.Int16: bw.Write(br.Read<short>()); break;
            case EdfPrimitiveType.UInt32: bw.Write(br.Read<uint>()); break;
            case EdfPrimitiveType.Int32: bw.Write(br.Read<int>()); break;
            case EdfPrimitiveType.UInt64: bw.Write(br.Read<ulong>()); break;
            case EdfPrimitiveType.Int64: bw.Write(br.Read<long>()); break;
            case EdfPrimitiveType.Single: bw.Write(br.Read<float>()); break;
            case EdfPrimitiveType.Double: bw.Write(br.Read<double>()); break;
            case EdfPrimitiveType.String:
                br.ReadToSpan(buf, out var pt, out var len);
                bw.WriteSpan(buf.Slice(0, len), pt); break;
            //bw.Write(br.ReadString()); break;
            case EdfPrimitiveType.Char: bw.WriteCharArray(br.ReadCharArray()); break;
        }
    }
}
