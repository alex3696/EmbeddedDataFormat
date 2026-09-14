namespace EdfConv.BinSiamFormat;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct MtRepV2 // SPSK_FILE_V1_1
{
    public uint FileType;           //тип файла
    ByteArray40 _rawDescription;     //описание файла
    public string? Description
    {
        get => Encoding.UTF8.GetString(_rawDescription);
        set => Encoding.UTF8.GetBytes(value, _rawDescription);
    }
    public byte Year;               //год
    public byte Month;              //месяц
    public byte Day;                //день
    public byte NotUsed;            //
    public ushort Shop;             //номер цеха
    public ushort Field;            //код месторождения
    ByteArray6 _rawCluster;         //номер куста [6]
    ByteArray6 _rawWell;            //номер скважины [6]
    public string? Cluster
    {
        readonly get => Encoding.UTF8.GetStringEndTrim(_rawCluster);
        set => Encoding.UTF8.GetBytes(value, _rawCluster);
    }
    public string? Well
    {
        readonly get => Encoding.UTF8.GetStringEndTrim(_rawWell);
        set => Encoding.UTF8.GetBytes(value, _rawWell);
    }
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
