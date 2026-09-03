namespace Test.BinSiamFormat;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct DynRepV2
{
    public DynRepV2()
    {
        Id = new();
        Data = new byte[2000];
    }

    public uint FileType;         //тип файла
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 40)]
    public string? Description;     //описание файла

    public ResearchIdV2 Id;         //идентификаторы исследования

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

    //[MarshalAs(UnmanagedType.SafeArray, SafeArraySubType = VarEnum.VT_DATE)]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2000)]
    public byte[] Data;           //данные динамограммы [1000]

    public ushort crc;						//crc16

}
