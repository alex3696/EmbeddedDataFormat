namespace EdfNet.Core;

public class BinToTxt
{
    class StateWriterTxt : WriterTxt
    {
        public StateWriterTxt(Stream stream, Config? cfg = default)
            : base(stream, cfg)
        {
        }
        public BufStateTxt State => _state;
    }
    class StateReaderBin : ReaderBin
    {
        public StateReaderBin(Stream stream, Config? cfg = default)
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
        StateReaderBin reader = new(srcBin);
        StateWriterTxt writer = new(dstTxt, reader.Cfg);
        try
        {
            while (reader.ReadBlock())
            {
                switch (reader.GetBlockType())
                {
                    default: Console.WriteLine($"Block type {reader.GetBlockType()} not supported here."); break;
                    case BlockType.Schema:
                        var rec = reader.CurrentSchema;
                        if (rec != null)
                            writer.WriteSchema(rec);
                        break;
                    case BlockType.Data:
                        ArgumentNullException.ThrowIfNull(writer.CurrentSchema?.Type);
                        Convert(reader.State, writer.State);
                        break;
                }
            }
        }
        catch (EndOfStreamException)
        {

        }
        writer.Flush();
    }
    private static void Convert(BufStateBin readerState, BufStateTxt writerState)
    {
        BufReaderBin br = new(readerState);
        BufWriterTxt bw = new(writerState);

        while (0 < readerState.ReadAvailableLen)
        {
            var et = readerState.Enum.CurrentType;
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
