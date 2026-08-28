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
        StateReaderTxt reader = new(srcBin);
        if(!reader.ReadBlock() || EdfBlockType.Config != reader.GetBlockType() )
            throw new Exception("There are no config block");
        using StateWriterBin writer = new(dstTxt, reader.Cfg);
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
                        BufWriterBin bw = new(writer.State);
                        BufReaderTxt br = new(reader.TokenReader, reader.Enum);
                        do
                        {
                            Convert(br, bw);
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

    private static void Convert(BufReaderTxt br, BufWriterBin bw)
    {
        switch (bw.CurrentType.Type)
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
            case EdfPrimitiveType.String: bw.Write(br.ReadString()); break;
            case EdfPrimitiveType.Char: bw.WriteCharArray(br.ReadCharArray()); break;
        }
    }
}
