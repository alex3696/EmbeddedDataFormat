namespace Test.BinSiamFormat;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct ResearchIdV2
{
    public ResearchIdV2()
    {
        Time = new();
        ClusterArray = new byte[6];
        WellArray = new byte[6];
    }

    public ushort ResearchType;     //тип исследования
    public ushort DeviceType;       //тип датчика
    public uint DeviceNum;        //номер датчика
    public ushort Shop;             //номер цеха
    public ushort Oper;             //номер оператора
    public ushort Field;            //код месторождения

    //[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 6)]
    //public string? Cluster;         //номер куста [6]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.U1)]
    public byte[] ClusterArray;       //номер куста [6]
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.U1)]
    public byte[] WellArray;       //номер скважины [6]
    public string? Cluster
    {
        get => StringTool.GetStringUtf8(ClusterArray);
        set => StringTool.SetStringUtf8(value, ClusterArray);
    }
    public string? Well
    {
        get => StringTool.GetStringUtf8(WellArray);
        set => StringTool.SetStringUtf8(value, WellArray);
    }
    public SiamTime Time;           ////время начала исследования
    public ushort RegType;          //тип регистратора (new)
    public uint RegNum;           //номер регистратора (new)
}
