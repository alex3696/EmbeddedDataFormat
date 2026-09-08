using System.Globalization;

namespace EdfConv.BinSiamFormat;

public static class ConverterD
{
    public static int DToEdf(Stream src, IEdfWriter writer)
    {
        var repSize = Marshal.SizeOf<DynRepV2>();
        Span<byte> buf = stackalloc byte[repSize];
        src.ReadExactly(buf);
        var dat = MemoryMarshal.Read<DynRepV2>(buf);

        //writer.WriteSchema(FileTypeId.GetEdfSchema());
        writer.WriteInfData(new FileTypeId { Type = (ushort)dat.FileType, Version = 1 });
        //writer.WriteSchema(DateTimeTz.GetEdfSchema());
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

        writer.WriteInfData(0, "TravelStep", "величина дискреты перемещения 0.1мм/1", EdfPrimitiveType.UInt16, dat.TravelStep);
        writer.WriteInfData(0, "LoadStep", "величина дискреты нагрузки кг/1", EdfPrimitiveType.UInt16, dat.LoadStep);
        writer.WriteInfData(0, "TimeStep", "величина дискреты времени мс/1", EdfPrimitiveType.UInt16, dat.TimeStep);

        writer.WriteInfData(0, "Rod", "диаметр штока", EdfPrimitiveType.Single, (float)(dat.Rod / 10.0f));
        writer.WriteInfData(0, "Aperture", "номер отверстия", EdfPrimitiveType.UInt16, dat.Aperture);

        writer.WriteInfData(0, "MaxWeight", "максимальная нагрузка (кг)", EdfPrimitiveType.UInt32, (uint)(dat.MaxWeight * dat.LoadStep));
        writer.WriteInfData(0, "MinWeight", "минимальная нагрузка (кг)", EdfPrimitiveType.UInt32, (uint)(dat.MinWeight * dat.LoadStep));
        writer.WriteInfData(0, "TopWeight", "вес штанг вверху (кг)", EdfPrimitiveType.UInt32, (uint)(dat.TopWeight * dat.LoadStep));
        writer.WriteInfData(0, "BotWeight", "вес штанг внизу (кг)", EdfPrimitiveType.UInt32, (uint)(dat.BotWeight * dat.LoadStep));
        writer.WriteInfData(0, "Travel", "ход штока (мм)", EdfPrimitiveType.Double, (double)(dat.Travel * dat.TravelStep / 10.0f));
        writer.WriteInfData(0, "BeginPos", "положение штока перед первым измерением (мм)", EdfPrimitiveType.Double,
            (double)(dat.BeginPos * dat.TravelStep / 10.0f));
        writer.WriteInfData(0, "Period", "период качаний (мс)", EdfPrimitiveType.UInt32, (uint)(dat.Period * dat.TimeStep));
        writer.WriteInfData(0, "Cycles", "пропущено циклов", EdfPrimitiveType.UInt16, (ushort)dat.Cycles);

        writer.WriteInfData(0, "Pressure", "затрубное давление (атм)", EdfPrimitiveType.Double, (double)(dat.Pressure / 10.0f));
        writer.WriteInfData(0, "BufPressure", "буферное давление (атм)", EdfPrimitiveType.Double, (double)(dat.BufPressure / 10.0f));
        writer.WriteInfData(0, "LinePressure", "линейное давление (атм)", EdfPrimitiveType.Double, (double)(dat.LinePressure / 10.0f));
        writer.WriteInfData(0, "PumpType", "тип привода станка-качалки {}", EdfPrimitiveType.UInt16, (ushort)(dat.PumpType));

        writer.WriteInfData(0, "Acc", "напряжение аккумулятора датчика, (В)", EdfPrimitiveType.Single, (float)(dat.Acc / 10.0f));
        writer.WriteInfData(0, "Temp", "температура датчика, (°С)", EdfPrimitiveType.Single, (float)(dat.Temp / 10.0f));

        var chSch = ChartNType.GetEdfSchema();
        chSch.Name = "DynamogrammChartInfo";
        writer.WriteSchema(chSch);
        writer.WriteValue(new ChartNType() { Name = "Position", Unit = "m", ApiCode = default, Desc = "перемещение" });
        writer.WriteValue(new ChartNType() { Name = "Weight", Unit = "T", ApiCode = default, Desc = "вес" });

        var dynDataSch = Chart2D.GetEdfSchema();
        dynDataSch.Name = "DynChart";
        //dynDataSch.Type.Childs[0].Name = string.Empty;
        //dynDataSch.Type.Childs[1].Name = string.Empty;
        writer.WriteSchema(dynDataSch);
        Chart2D p = new() { x = 0, y = 0 };
        var items = MemoryMarshal.Cast<byte, ushort>(dat.Data);
        for (int i = 0; i < 1000; i++)
        {
            p.x += (float)(ExtractTravel(items[i]) * dat.TravelStep / 1.0E4);
            p.y = (float)((items[i] & 1023) * dat.LoadStep * 1.0E-3);
            writer.WriteValue(p);
        }
        return 0;
    }
    static sbyte ExtractTravel(ushort number) // 6bit integer
    {
        sbyte result = (sbyte)((number & 0xFC00) >> 10);
        return (sbyte)((result > 31) ? (result - 64) : result);
    }

    public static int EdfToD(IEdfReader reader, Stream dst)
    {
        var dat = new DynRepV2();
        // 
        dat.FileType = 6;
        dat.Id.ResearchType = 1;
        dat.Description = "SIAM COMPLEX DYNAMOGRAM V2.0";
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
    static void ReadBySchemaName(string? schemaName, IEdfReader reader, ref DynRepV2 dat)
    {
        switch (schemaName)
        {
            default: break;
            case "Oper": dat.Id.Oper = reader.ReadValue<ushort>(); break;
            case "TravelStep": dat.TravelStep = reader.ReadValue<ushort>(); break;
            case "LoadStep": dat.LoadStep = reader.ReadValue<ushort>(); break;
            case "TimeStep": dat.TimeStep = reader.ReadValue<ushort>(); break;
            case "Rod": dat.Rod = (ushort)(reader.ReadValue<float>() * 10.0); break;
            case "Aperture": dat.Aperture = reader.ReadValue<ushort>(); break;
            case "MaxWeight": dat.MaxWeight = (ushort)(((double)reader.ReadValue<uint>()) / dat.LoadStep); break;
            case "MinWeight": dat.MinWeight = (ushort)(((double)reader.ReadValue<uint>()) / dat.LoadStep); break;
            case "TopWeight": dat.TopWeight = (ushort)(((double)reader.ReadValue<uint>()) / dat.LoadStep); break;
            case "BotWeight": dat.BotWeight = (ushort)(((double)reader.ReadValue<uint>()) / dat.LoadStep); break;
            case "Travel": dat.Travel = (ushort)(reader.ReadValue<double>() * 10.0 / dat.TravelStep); break;
            case "BeginPos": dat.BeginPos = (ushort)(reader.ReadValue<double>() * 10.0 / dat.TravelStep); break;
            case "Period": dat.Period = (ushort)(((double)reader.ReadValue<uint>()) / dat.TimeStep); break;
            case "Cycles": dat.Cycles = reader.ReadValue<ushort>(); break;
            case "Pressure": dat.Pressure = (short)Math.Round(reader.ReadValue<double>() * 10.0d, 0); break;
            case "BufPressure": dat.BufPressure = (short)Math.Round(reader.ReadValue<double>() * 10.0d, 0); break;
            case "LinePressure": dat.LinePressure = (short)Math.Round(reader.ReadValue<double>() * 10.0d, 0); break;
            case "PumpType": dat.PumpType = reader.ReadValue<ushort>(); break;
            case "Acc": dat.Acc = (ushort)Math.Round(reader.ReadValue<float>() * 10.0, 0); break;
            case "Temp": dat.Temp = (short)Math.Round(reader.ReadValue<float>() * 10.0, 0); break;
            case "DynChart": ReadDynChart(reader, ref dat); break;
        }
    }
    static void ReadBySchemaId(ushort schemaId, IEdfReader reader, ref DynRepV2 dat)
    {
        switch (schemaId)
        {
            default: break;
            case (ushort)StdSchemaType.FILETYPEID:
                if (dat.FileType != reader.ReadValue<FileTypeId>().Type)
                    return;
                break;
            case (ushort)StdSchemaType.BEGINDATETIME:
                dat.Id.Time.Dt = reader.ReadValue<DateTimeTz>().ToDateTime();
                break;
            case (ushort)StdSchemaType.POSITION:
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
            case (ushort)StdSchemaType.DEVICEINFO:
                {
                    var di = reader.ReadValue<DeviceInfo>();
                    dat.Id.DeviceType = di.SwId;
                    dat.Id.DeviceNum = (uint)di.HwNumber;
                }
                break;
            case (ushort)StdSchemaType.REGINFO:
                {
                    var di = reader.ReadValue<DeviceInfo>();
                    dat.Id.RegType = di.SwId;
                    dat.Id.RegNum = (uint)di.HwNumber;
                }
                break;
        }
    }
    static void ReadDynChart(IEdfReader reader, ref DynRepV2 dat)
    {
        Span<byte> byteSpan = dat.Data;
        Span<ushort> items = MemoryMarshal.Cast<byte, ushort>(byteSpan);
        Chart2D record = default;
        Chart2D s;
        for (int i = 0; i < 1000; ++i)
        {
            s = reader.ReadValue<Chart2D>();
            double posDif = 0 < i ? s.x - record.x : s.x;
            ushort tr = (ushort)((((ushort)Math.Round(posDif * 1.0E4 / dat.TravelStep)) & 0x003f) << 10);
            ushort w = (ushort)(((ushort)Math.Round(s.y * 1.0E3 / dat.LoadStep)) & 0x003f);
            items[i] = (ushort)(tr | w);
            record = s;
        }
    }
}
