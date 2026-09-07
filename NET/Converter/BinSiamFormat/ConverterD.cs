namespace EdfConv.BinSiamFormat;

public static class ConverterD
{
    static sbyte ExtractTravel(ushort number) // 6bit integer
    {
        sbyte result = (sbyte)((number & 0xFC00) >> 10);
        return (sbyte)((result > 31) ? (result - 64) : result);
    }
    public static int DToEdf(Stream src, Stream dst, IEdfWriter writer)
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

    public static int EdfToD(Stream src, Stream dst, IEdfReader reader)
    {
        return 0;
    }
}
