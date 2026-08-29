using EdfNet.Core.Binary;
using EdfNet.Core.Text;

namespace EdfNet.Converters;

public class TxtToBin
{
    class StateWriterBin : EdfBinaryWriter
    {
        public StateWriterBin(Stream stream, EdfConfig? cfg = default)
            : base(stream, cfg)
        {
        }
        public BufStateBin State => _state;
    }
    class StateReaderTxt : EdfTextReader
    {
        public StateReaderTxt(Stream stream, EdfConfig? cfg = default)
            : base(stream, cfg)
        {
        }
        public EdfTokenReader TokenReader => _tokenReader;
        public TextCircularEdfTypeEnumerator Enum => _enum;
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
        using StateReaderTxt reader = new(srcBin);
        if (!reader.ReadBlock() || EdfBlockType.Config != reader.GetBlockType())
            throw new Exception("There are no config block");
        using StateWriterBin writer = new(dstTxt, reader.Cfg);
        BufReaderTxt tr = new();
        BufWriterBin bw = new();
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
                            tr = new(reader.TokenReader, reader.Enum);
                            bw = new(writer.State);
                        }
                        break;
                    case EdfBlockType.Data:
                        ArgumentNullException.ThrowIfNull(writer.CurrentSchema?.Type);
                        do
                        {
                            switch (bw.CurrentType.Type)
                            {
                                default:
                                case EdfPrimitiveType.Struct: throw new EdfWrongTypeException();
                                case EdfPrimitiveType.UInt8: bw.Write(tr.ReadUInt8()); break;
                                case EdfPrimitiveType.Int8: bw.Write(tr.ReadInt8()); break;
                                case EdfPrimitiveType.UInt16: bw.Write(tr.ReadUInt16()); break;
                                case EdfPrimitiveType.Int16: bw.Write(tr.ReadInt16()); break;
                                case EdfPrimitiveType.UInt32: bw.Write(tr.ReadUInt32()); break;
                                case EdfPrimitiveType.Int32: bw.Write(tr.ReadInt32()); break;
                                case EdfPrimitiveType.UInt64: bw.Write(tr.ReadUInt64()); break;
                                case EdfPrimitiveType.Int64: bw.Write(tr.ReadInt64()); break;
                                case EdfPrimitiveType.Single: bw.Write(tr.ReadSingle()); break;
                                case EdfPrimitiveType.Double: bw.Write(tr.ReadDouble()); break;
                                case EdfPrimitiveType.Char:
                                case EdfPrimitiveType.String:
                                    tr.ReadToSpan(buf, out var pt, out var len);
                                    bw.WriteSpan(buf.Slice(0, len), pt); break;
                            }
                        }
                        while (reader.TokenReader.MoveNext()
                            && reader.TokenReader.TokenType != TextTokenType.EOF
                            && reader.TokenReader.TokenType != TextTokenType.ConfigBegin
                            && reader.TokenReader.TokenType != TextTokenType.SchemaBegin
                            && reader.TokenReader.TokenType != TextTokenType.RecBegin);
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
