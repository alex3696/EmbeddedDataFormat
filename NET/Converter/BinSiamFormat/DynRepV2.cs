namespace EdfConv.BinSiamFormat;

// 2 122
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct DynRepV2
{
    public uint FileType;           //тип файла
    ByteArray40 _rawDescription;     //описание файла
    public string? Description
    {
        get => Encoding.UTF8.GetString(_rawDescription);
        set => Encoding.UTF8.GetBytes(value, _rawDescription);
    }
    public ResearchIdV2 Id;         //идентификаторы исследования // 40
    public ushort Rod;              //диаметр штока, 0.1 мм
    public ushort Aperture;         //номер отверстия
    public ushort MaxWeight;        //максимальная нагрузка, дискрет (изм)
    public ushort MinWeight;        //минимальная нагрузка, дискрет (изм)
    public ushort TopWeight;        //вес штанг вверху, дискрет (изм)
    public ushort BotWeight;        //вес штанг внизу, дискрет (изм)
    public ushort Travel;           //ход штока, дискрет
    public ushort BeginPos;         //положение штока перед первым измерением, дискрет
    public ushort TravelStep;       //величина дискреты перемещения, 0.1 мм
    public ushort Period;           //период качаний, дискрет
    public ushort TimeStep;         //величина дискреты времени, мс
    public ushort Cycles;           //пропущено циклов
    public ushort LoadStep;         //величина дискреты нагрузки, кг (new)
    public short Pressure;          //затрубное давление, 0.1 атм (new)
    public short BufPressure;       //буферное давление, 0.1 атм (new)
    public short LinePressure;      //линейное давление, 0.1 атм (new)
    public ushort PumpType;         //тип привода станка-качалки (new)
    public ushort Acc;              //напряжение аккумулятора датчика, 0.1В (new)
    public short Temp;              //температура датчика, 0.1С (new)
    public ByteArray2000 Data;      //данные динамограммы [1000]
    public ushort crc;              //crc16

}
