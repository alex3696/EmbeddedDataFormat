namespace EdfConv.BinSiamFormat;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct EchoRepV2
{
    public uint FileType;           //тип файла
    ByteArray40 _rawDescription;     //описание файла
    public string? Description
    {
        readonly get => Encoding.UTF8.GetStringEndTrim(_rawDescription);
        set => Encoding.UTF8.GetBytes(value, _rawDescription);
    }
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
    public ByteArray3000 Data;      //данные динамограммы [1000]
    public ushort crc;						//crc16
}
