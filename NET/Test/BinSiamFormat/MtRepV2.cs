using EdfNet.Core.Binary;

namespace Test.BinSiamFormat;

public class Umt3Dat
{
    public uint FType;
    public DateTime BeginDt;
    public DateTime EndDt;
    public string? Comment;
    public long RecCount;
    public uint IntervalMills;

    public ushort FieldId;
    public string? Cluster;
    public string? Well;
    public ushort ShopId;
    public ushort PlaceId;
    public int Depth;

    public ushort RegId;
    public ushort RegNumber;
    public ushort RegVers;

    public ushort SwId;
    public uint HwNumber;
    public ushort SwVers;

}


[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct MtRepV2 // SPSK_FILE_V1_1
{
    public MtRepV2()
    {
        Cluster = new byte[6];
        Well = new byte[6];
    }

    public uint FileType;           //тип файла
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
    public string? Description;     //описание файла

    public byte Year;               //год
    public byte Month;              //месяц
    public byte Day;                //день
    public byte NotUsed;            //

    public ushort Shop;             //номер цеха
    public ushort Field;            //код месторождения
    //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 6)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.U1)]
    public readonly byte[] Cluster;         //номер куста [6]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.U1)]
    public readonly byte[] Well;            //номер скважины [6]
    public ushort PlaceId;          //место установки
    public int Depth;               //глубина установки

    public ushort RegType;          //тип регистратора (new)
    public ushort RegNum;           //номер регистратора (new)
    public ushort RegVer;           //

    public ushort SensType;         //тип датчика
    public UInt32 SensNum;          //номер датчика
    public ushort SensVer;          //версия датчика

    public ushort crc;				//crc16

}

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct MtRepData // SPSK_DATA_V1
{
    public uint Time;          // время измерения от начала дня, мс
    public int Press;          // давление, 0.001 атм
    public int Temp;           // температура, 0.001 °С
    public ushort Vbat;          // напряжение батареи,
    public ushort crc;			// CRC16
}



public static class MtRepV2Ext
{
    private static void UpdateCrc16AndWrite(Stream stream, Span<byte> data)
    {
        ModbusCRC.CalcBytes(data.Slice(0, data.Length - 2))
            .CopyTo(data.Slice(data.Length - 2, 2));
        stream.Write(data);
    }
    public static void Write(this Stream stream, MtRepV2 header)
    {
        var bheader = StructSerialize.ToBytes(header);
        UpdateCrc16AndWrite(stream, bheader);
    }
    public static void Write(this Stream stream, MtRepData data)
    {
        var bdata = StructSerialize.ToBytes(data);
        UpdateCrc16AndWrite(stream, bdata);
    }
}
