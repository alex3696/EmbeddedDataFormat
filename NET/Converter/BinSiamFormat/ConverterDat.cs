using EdfNet.Core.Binary;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace EdfConv.BinSiamFormat;

public static class ConverterDat
{
    public static int DatToEdf(Stream src, IEdfWriter writer)
    {
        var repSize = Marshal.SizeOf<MtRepV2>();
        Span<byte> buf = stackalloc byte[repSize];
        src.ReadExactly(buf);
        var dat = MemoryMarshal.Read<MtRepV2>(buf);
        writer.WriteInfData(new FileTypeId { Type = (ushort)dat.FileType, Version = 1 });
        writer.WriteInfData(new DateTimeTz() { Year = (ushort)(dat.Year + 2000), Month = dat.Month, Day = dat.Day });
        writer.WriteInfData(new Position()
        {
            Field = dat.Field.ToString(),
            Cluster = dat.Cluster,
            Well = dat.Well,
            Shop = dat.Shop.ToString(),
        });
        writer.WriteInfData(0, "PlaceId", "место установки", EdfPrimitiveType.UInt16, dat.PlaceId);
        writer.WriteInfData(0, "Depth", "глубина установки", EdfPrimitiveType.Int32, dat.Depth);
        writer.WriteInfData((ushort)SchemaId.DEVICEINFO, "DevInfo", "скважный прибор", new DeviceInfo()
        {
            SwId = dat.SensType,
            SwModel = dat.SensVer,
            SwRevision = 0,
            HwId = 0,
            HwModel = 0,
            HwNumber = dat.SensNum,
        });
        writer.WriteInfData((ushort)SchemaId.REGINFO, "RegInfo", "наземный регистратор", new DeviceInfo()
        {
            SwId = dat.RegType,
            SwModel = dat.RegVer,
            SwRevision = 0,
            HwId = 0,
            HwModel = 0,
            HwNumber = dat.RegNum,
        });
        var chSch = ChartNType.GetEdfSchema();
        chSch.Name = "ChartInfo";
        writer.WriteSchema(chSch);
        writer.WriteValue(new ChartNType() { Name = "Time", Unit = "мс", ApiCode = default, Desc = "время измерения от начала дня" });
        writer.WriteValue(new ChartNType() { Name = "Press", Unit = "0.001 атм", ApiCode = default, Desc = "давление" });
        writer.WriteValue(new ChartNType() { Name = "Temp", Unit = "0.001 °С", ApiCode = default, Desc = "температура" });
        writer.WriteValue(new ChartNType() { Name = "Vbat", Unit = "0.001 V", ApiCode = default, Desc = "напряжение батареи" });

        var dataSch = OmegaData_v1_1.GetEdfSchema();
        writer.WriteSchema(dataSch);

        var recSize = Marshal.SizeOf<MtRepData>();
        Span<byte> recBuf = stackalloc byte[recSize];
        try
        {
            do
            {
                src.ReadExactly(recBuf);
                writer.WriteValue<OmegaData_v1_1>(Unsafe.As<byte, OmegaData_v1_1>(ref MemoryMarshal.GetReference(recBuf)));
            }
            while (true);
        }
        catch (EndOfStreamException /*eofEx*/)
        {
        }
        return 0;
    }

    public static int EdfToDat(IEdfReader reader, Stream dst)
    {
        var dat = new MtRepV2
        {
            FileType = 11,
            Description = "OMEGA SAMT DATA V1.1"
        };
        try
        {
            while (reader.ReadBlock())
            {
                switch (reader.GetBlockType())
                {
                    default:
                    case EdfBlockType.Config: break;
                    case EdfBlockType.Schema: break;
                    case EdfBlockType.Data:
                        if (null != reader.CurrentSchema)
                        {
                            if (0 == reader.CurrentSchema.Id)
                                ReadBySchemaName(reader.CurrentSchema.Name, reader, ref dat);
                            else
                                ReadBySchemaId(reader.CurrentSchema.Id, reader, ref dat, dst);
                        }
                        break;
                }
            }
        }
        catch (EndOfStreamException)
        {
        }
        dst.Flush();
        return 0;
    }
    static void ReadBySchemaId(ushort schemaId, IEdfReader reader, ref MtRepV2 dat, Stream dst)
    {
        switch (schemaId)
        {
            default: break;
            case (ushort)SchemaId.FILETYPEID:
                if (dat.FileType != reader.ReadValue<FileTypeId>().Type)
                    return;
                break;
            case (ushort)SchemaId.BEGINDATETIME:
                {
                    var dt = reader.ReadValue<DateTimeTz>();
                    dat.Year = (byte)(dt.Year - 2000);
                    dat.Month = dt.Month;
                    dat.Day = dt.Day;
                }
                break;
            case (ushort)SchemaId.POSITION:
                {
                    var pos = reader.ReadValue<Position>();
                    if (ushort.TryParse(pos.Field, CultureInfo.InvariantCulture, out ushort field))
                        dat.Field = field;
                    dat.Cluster = pos.Cluster;
                    dat.Well = pos.Well;
                    if (ushort.TryParse(pos.Shop, CultureInfo.InvariantCulture, out ushort shop))
                        dat.Shop = shop;
                }
                break;
            case (ushort)SchemaId.DEVICEINFO:
                {
                    var dvc = reader.ReadValue<DeviceInfo>();
                    dat.SensType = dvc.SwId;
                    dat.SensVer = dvc.SwModel;
                    dat.SensNum = (uint)dvc.HwNumber;
                }
                break;
            case (ushort)SchemaId.REGINFO:
                {
                    var dvc = reader.ReadValue<DeviceInfo>();
                    dat.RegType = dvc.SwId;
                    dat.RegVer = dvc.SwModel;
                    dat.RegNum = (ushort)dvc.HwNumber;
                }
                break;
            case (ushort)SchemaId.OMEGADATA: ReadChart(reader, ref dat, dst); break;
        }
    }
    static void ReadBySchemaName(string? schemaName, IEdfReader reader, ref MtRepV2 dat)
    {
        switch (schemaName)
        {
            default: break;
            case "Shop": dat.Shop = reader.ReadValue<ushort>(); break;
            case "PlaceId": dat.PlaceId = reader.ReadValue<ushort>(); break;
            case "Depth": dat.Depth = reader.ReadValue<int>(); break;
        }
    }
    static void ReadChart(IEdfReader reader, ref MtRepV2 hdr, Stream dst)
    {
        ReadOnlySpan<byte> hdrBuf = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref hdr, 1));
        hdr.crc = ModbusCRC.Calc(hdrBuf);
        dst.Write(hdrBuf);

        MtRepData rec = default;
        Span<byte> recBuf = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref rec, 1));
        try
        {
            while (true)
            {
                var record = reader.ReadValue<OmegaData_v1_1>();
                //rec.Time = record.Time;
                //rec.Press = record.Press;
                //rec.Temp = record.Temp;
                //rec.Vbat = record.Vbat;
                ReadOnlySpan<byte> buf = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref record, 1));
                buf.CopyTo(recBuf);
                // !!! CRC в оригинале есть, но не используется - всегда 0
                //rec.crc = ModbusCRC.Calc(buf);
                dst.Write(recBuf);
            }
        }
        catch (EndOfStreamException)
        {
        }
    }
}
