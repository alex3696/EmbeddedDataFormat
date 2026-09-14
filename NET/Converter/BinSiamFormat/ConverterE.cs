using System.Globalization;

namespace EdfConv.BinSiamFormat;

public static class ConverterE
{
    public static int EToEdf(Stream src, IEdfWriter writer)
    {
        var repSize = Marshal.SizeOf<EchoRepV2>();
        Span<byte> buf = stackalloc byte[repSize];
        src.ReadExactly(buf);
        var dat = MemoryMarshal.Read<EchoRepV2>(buf);
        writer.WriteInfData(new FileTypeId { Type = (ushort)dat.FileType, Version = 1 });
        writer.WriteInfData(DateTimeTz.FromDateTime(dat.Id.Time.Dt));
        writer.WriteInfData(new Position()
        {
            Field = dat.Id.Field.ToString(),
            Cluster = dat.Id.Cluster,
            Well = dat.Id.Well,
            Shop = dat.Id.Shop.ToString(),
        });
        writer.WriteInfData(13, "DevInfo", "прибор", new DeviceInfo()
        {
            SwId = dat.Id.DeviceType,
            SwModel = 0,
            SwRevision = 0,
            HwId = 0,
            HwModel = 0,
            HwNumber = dat.Id.DeviceNum,
        });
        writer.WriteInfData(14, "RegInfo", "регистратор", new DeviceInfo()
        {
            SwId = dat.Id.RegType,
            SwModel = 0,
            SwRevision = 0,
            HwId = 0,
            HwModel = 0,
            HwNumber = dat.Id.RegNum,
        });
        writer.WriteInfData(0, "Oper", default, EdfPrimitiveType.UInt16, dat.Id.Oper);

        double discrete = Utils.ExtractDiscrete(dat.Level);
        int maxDepthMult = 1;
        if (Utils.Discrete3000 < discrete)
            maxDepthMult = 2;
        float speed = (float)dat.Speed / 10;
        float xDiscrete = speed / 341;

        writer.WriteInfData(0, "Discrete", "величина дискреты", EdfPrimitiveType.Double, discrete);
        writer.WriteInfData(0, "Reflections", "число отражений", EdfPrimitiveType.UInt16, Utils.ExtractReflections(dat.Reflections));
        writer.WriteInfData(0, "Level", "уровень без поправки на скорость звука (для скорости 341.333 м/с), м", EdfPrimitiveType.Double, Utils.ExtractLevel(dat.Level));
        writer.WriteInfData(0, "Pressure", "затрубное давление (атм)", EdfPrimitiveType.Double, (double)(dat.Pressure / 10.0f));
        writer.WriteInfData(0, "Table", "номер таблицы скоростей", EdfPrimitiveType.UInt16, dat.Table);
        writer.WriteInfData(0, "Speed", "скорость звука, м/с", EdfPrimitiveType.Single, speed);
        writer.WriteInfData(0, "BufPressure", "буферное давление (атм)", EdfPrimitiveType.Double, (double)Math.Round(dat.BufPressure / 10.0f, 1));
        writer.WriteInfData(0, "LinePressure", "линейное давление (атм)", EdfPrimitiveType.Double, (double)Math.Round(dat.LinePressure / 10.0f, 1));
        writer.WriteInfData(0, "Current", "ток, 0.1А", EdfPrimitiveType.UInt16, dat.Current);
        writer.WriteInfData(0, "IdleHour", "время простоя, ч", EdfPrimitiveType.UInt8, dat.IdleHour);
        writer.WriteInfData(0, "IdleMin", "время простоя, мин", EdfPrimitiveType.UInt8, dat.IdleMin);
        writer.WriteInfData(0, "Mode", "режим исследования", EdfPrimitiveType.UInt8, (byte)dat.Mode);
        writer.WriteInfData(0, "Acc", "напряжение аккумулятора датчика, (В)", EdfPrimitiveType.Single, (float)(dat.Acc / 10.0f));
        writer.WriteInfData(0, "Temp", "температура датчика, (°С)", EdfPrimitiveType.Single, (float)(dat.Temp / 10.0f));
        var chSch = ChartNType.GetEdfSchema();
        chSch.Name = "EchoChartInfo";
        writer.WriteSchema(chSch);
        writer.WriteValue(new ChartNType() { Name = "Depth", Unit = "m", ApiCode = default, Desc = "глубина" });
        writer.WriteValue(new ChartNType() { Name = "Val", Unit = "adc", ApiCode = default, Desc = "амплитуда" });
        var chartSch = Chart2D.GetEdfSchema();
        chartSch.Name = "EchoChart";
        writer.WriteSchema(chartSch);
        Chart2D p = new() { x = 0, y = 0 };
        for (int i = 0; i < 3000; i++)
        {
            if (dat.Data[i] > 127)
                p.y = (float)(Utils.UnPow(-1 * (dat.Data[i] - 127), 1.0d / 0.35d) / 1000d);
            else
                p.y = (float)(Utils.UnPow(dat.Data[i], 1.0d / 0.35d) / 1000d);
            p.x = xDiscrete * i * maxDepthMult;
            writer.WriteValue(p);
        }
        return 0;
    }

    public static int EdfToE(IEdfReader reader, Stream dst)
    {
        var dat = new EchoRepV2
        {
            FileType = 5,
            Description = "SIAM COMPLEX ECHOGRAM V2.0"
        };
        dat.Id.ResearchType = 1;
        double discrete = 1.0;
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
                                ReadBySchemaName(reader.CurrentSchema.Name, reader, ref dat, ref discrete);
                            else
                                ReadBySchemaId(reader.CurrentSchema.Id, reader, ref dat);
                        }
                        break;
                }
            }
        }
        catch (EndOfStreamException)
        {
        }
        ReadOnlySpan<byte> buf = MemoryMarshal.AsBytes(MemoryMarshal.CreateSpan(ref dat, 1));
        dat.crc = EdfNet.Core.Binary.ModbusCRC.Calc(buf[..^2]);
        dst.Write(buf);
        dst.Flush();
        return 0;
    }
    static void ReadBySchemaName(string? schemaName, IEdfReader reader, ref EchoRepV2 dat, ref double discrete)
    {
        switch (schemaName)
        {
            default: break;
            case "Oper": dat.Id.Oper = reader.ReadValue<ushort>(); break;
            case "Discrete": discrete = reader.ReadValue<double>(); break;
            case "Reflections": dat.Reflections = Utils.PackReflections(reader.ReadValue<ushort>()); break;
            case "Level": dat.Level = Utils.PackLevel(reader.ReadValue<double>(), discrete); break;
            case "Pressure": dat.Pressure = (short)Math.Round(reader.ReadValue<double>() * 10.0d, 0); break;
            case "Table": dat.Table = reader.ReadValue<ushort>(); break;
            case "Speed": dat.Speed = (ushort)Math.Round(reader.ReadValue<float>() * 10.0d, 0); break;
            case "BufPressure": dat.BufPressure = (short)Math.Round(reader.ReadValue<double>() * 10.0d, 0); break;
            case "LinePressure": dat.LinePressure = (short)Math.Round(reader.ReadValue<double>() * 10.0d, 0); break;
            case "Current": dat.Current = reader.ReadValue<ushort>(); break;
            case "IdleHour": dat.IdleHour = reader.ReadValue<byte>(); break;
            case "IdleMin": dat.IdleMin = reader.ReadValue<byte>(); break;
            case "Acc": dat.Acc = (ushort)Math.Round(reader.ReadValue<float>() * 10.0, 0); break;
            case "Temp": dat.Temp = (short)Math.Round(reader.ReadValue<float>() * 10.0, 0); break;
            case "EchoChart": ReadChart(reader, ref dat); break;
        }
    }
    static void ReadBySchemaId(ushort schemaId, IEdfReader reader, ref EchoRepV2 dat)
    {
        switch (schemaId)
        {
            default: break;
            case (ushort)SchemaId.FILETYPEID:
                if (dat.FileType != reader.ReadValue<FileTypeId>().Type)
                    return;
                break;
            case (ushort)SchemaId.BEGINDATETIME:
                dat.Id.Time.Dt = reader.ReadValue<DateTimeTz>().ToDateTime();
                break;
            case (ushort)SchemaId.POSITION:
                {
                    var pos = reader.ReadValue<Position>();
                    if (ushort.TryParse(pos.Field, CultureInfo.InvariantCulture, out ushort field))
                        dat.Id.Field = field;
                    dat.Id.Cluster = pos.Cluster;
                    dat.Id.Well = pos.Well;
                    if (ushort.TryParse(pos.Shop, CultureInfo.InvariantCulture, out ushort shop))
                        dat.Id.Shop = shop;
                }
                break;
            case (ushort)SchemaId.DEVICEINFO:
                {
                    var di = reader.ReadValue<DeviceInfo>();
                    dat.Id.DeviceType = di.SwId;
                    dat.Id.DeviceNum = (uint)di.HwNumber;
                }
                break;
            case (ushort)SchemaId.REGINFO:
                {
                    var di = reader.ReadValue<DeviceInfo>();
                    dat.Id.RegType = di.SwId;
                    dat.Id.RegNum = (uint)di.HwNumber;
                }
                break;
        }
    }
    static void ReadChart(IEdfReader reader, ref EchoRepV2 dat)
    {
        Span<byte> byteSpan = dat.Data;
        Span<ushort> items = MemoryMarshal.Cast<byte, ushort>(byteSpan);
        //Chart2D record = default;
        Chart2D s;
        for (int i = 0; i < 3000; ++i)
        {
            s = reader.ReadValue<Chart2D>();
            dat.Data[i] = (byte)Math.Round(Math.Pow(Math.Abs(s.y * 1000d), 0.35d));
            if (0 > s.y)
                dat.Data[i] += 127;
        }
    }
}
