namespace EdfConv.BinSiamFormat;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct ResearchIdV2
{
    public ushort ResearchType;     //тип исследования
    public ushort DeviceType;       //тип датчика
    public uint DeviceNum;        //номер датчика
    public ushort Shop;             //номер цеха
    public ushort Oper;             //номер оператора
    public ushort Field;            //код месторождения
    ByteArray6 _rawCluster;       //номер куста [6]
    ByteArray6 _rawWell;       //номер скважины [6]
    public string? Cluster
    {
        readonly get => Encoding.UTF8.GetStringEndTrim(_rawCluster);
        set => Encoding.UTF8.GetBytes(value, _rawCluster);
    }
    public string? Well
    {
        readonly get => Encoding.UTF8.GetString(_rawWell);
        set => Encoding.UTF8.GetBytes(value, _rawWell);
    }
    public SiamTime Time;           ////время начала исследования
    public ushort RegType;          //тип регистратора (new)
    public uint RegNum;           //номер регистратора (new)
}
