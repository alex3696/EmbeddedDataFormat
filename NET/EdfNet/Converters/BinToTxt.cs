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
        public TextStreamWriter Writer => _textWriter;
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
        //Span<byte> buf = stackalloc byte[256];
        using StateReaderBin reader = new(srcBin);
        using StateWriterTxt writer = new(dstTxt, reader.Cfg);
        BufReaderBin br = new();
        BufWriterTxt tw = new();
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
                        {
                            writer.WriteSchema(rec);
                            br = new BufReaderBin(reader.State);
                            tw = new BufWriterTxt(writer.Writer, writer.Enum);
                        }
                        break;
                    case EdfBlockType.Data:
                        ArgumentNullException.ThrowIfNull(writer.CurrentSchema?.Type);
                        while (0 < reader.State.ReadAvailableLen)
                        {
                            switch (br.CurrentType.Type)
                            {
                                default:
                                case EdfPrimitiveType.Struct: throw new PrimitiveNotSupportedException(br.CurrentType.Type);
                                case EdfPrimitiveType.UInt8: tw.Write(br.ReadUInt8()); break;
                                case EdfPrimitiveType.Int8: tw.Write(br.ReadInt8()); break;
                                case EdfPrimitiveType.UInt16: tw.Write(br.ReadUInt16()); break;
                                case EdfPrimitiveType.Int16: tw.Write(br.ReadInt16()); break;
                                case EdfPrimitiveType.UInt32: tw.Write(br.ReadUInt32()); break;
                                case EdfPrimitiveType.Int32: tw.Write(br.ReadInt32()); break;
                                case EdfPrimitiveType.UInt64: tw.Write(br.ReadUInt64()); break;
                                case EdfPrimitiveType.Int64: tw.Write(br.ReadInt64()); break;
                                case EdfPrimitiveType.Single: tw.Write(br.ReadSingle()); break;
                                case EdfPrimitiveType.Double: tw.Write(br.ReadDouble()); break;
                                case EdfPrimitiveType.Char:
                                case EdfPrimitiveType.String: //bw.Write(br.ReadString()); break;
                                    //br.ReadToSpan(buf, out var pt, out var len);
                                    //tw.WriteSpan(buf.Slice(0, len), pt);
                                    br.ReadTo(ref tw);
                                    break;
                            }
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
}
