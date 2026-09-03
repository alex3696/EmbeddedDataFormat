namespace Test.BinSiamFormat;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct EchoRepV2
{
    public EchoRepV2()
    {
        Id = new();
        Data = new byte[3000];
    }
    public uint FileType;           //тип файла
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
    public string? Description;     //описание файла

    public ResearchIdV2 Id;         //идентификаторы исследования

    public ushort Reflections;      //число отражений
    public ushort Level;			//уровень без поправки на скорость звука (для скорости 341.333 м/с), м
    public short Pressure;          //затрубное давление, 0.1 атм (new)
    public ushort Table;            //номер таблицы скоростей
    public ushort Speed;			//скорость звука, 0.1 м/с
    public short BufPressure;       //буферное давление, 0.1 атм (new)
    public short LinePressure;      //линейное давление, 0.1 атм (new)
    public ushort Current;          //ток, 0.1А (new)
    public byte IdleHour;           //время простоя, ч (new)
    public byte IdleMin;            //время простоя, мин (new)
    public ushort Mode;				//режим исследования (new)
    public ushort Acc;              //напряжение аккумулятора датчика, 0.1В (new)
    public short Temp;              //температура датчика, 0.1С (new)

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3000)]
    public byte[] Data;             //данные динамограммы [3000]

    public ushort crc;						//crc16

}
