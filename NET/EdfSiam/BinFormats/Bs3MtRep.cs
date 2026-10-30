namespace EdfSiam.BinFormats;

[EdfSerializable(id: 1000, name: "TMCommonInfo")]
public struct TMCommonInfo
{
    public byte DevQty;
    public byte Media;
    public uint TimeDiscrete;
    public int NoDataMark;
}
[EdfSerializable(id: 1001, name: "TMDeviceInfo")]
public struct TMDeviceInfo
{
    public byte Idx;
    public byte Addr;
    public string SensType;
    public ushort SensVer;
    public ushort Location;
    public int Depth_mm;
    public string SensNum;
    public ushort Par1;
    public ushort Par2;
    public ushort Par3;
}
[EdfSerializable(id: 1002, name: "ChartExactData")]
public struct ChartExactData
{
    public byte Idx;
    public uint Time;
    public ushort VPwr;
    public ushort RIso;
    public int Press;
    public int Temp;
}

[EdfSerializable(id: 1010)]
public struct ChartMultiData0
{
    public uint Time;
    public ushort VPwr;
    public ushort RIso;
}
[EdfSerializable(id: 1011)]
public struct ChartMultiData1
{
    public uint Time;
    public ushort VPwr;
    public ushort RIso;
    public int Press1;
    public int Temp1;
}
[EdfSerializable(id: 1012)]
public struct ChartMultiData2
{
    public uint Time;
    public ushort VPwr;
    public ushort RIso;
    public int Press1;
    public int Temp1;
    public int Press2;
    public int Temp2;
}
[EdfSerializable(id: 1013)]
public struct ChartMultiData3
{
    public uint Time;
    public ushort VPwr;
    public ushort RIso;
    public int Press1;
    public int Temp1;
    public int Press2;
    public int Temp2;
    public int Press3;
    public int Temp3;
}
[EdfSerializable(id: 1014)]
public struct ChartMultiData4
{
    public uint Time;
    public ushort VPwr;
    public ushort RIso;
    public int Press1;
    public int Temp1;
    public int Press2;
    public int Temp2;
    public int Press3;
    public int Temp3;
    public int Press4;
    public int Temp4;
}
[EdfSerializable(id: 1015)]
public struct ChartMultiData5
{
    public uint Time;
    public ushort VPwr;
    public ushort RIso;
    public int Press1;
    public int Temp1;
    public int Press2;
    public int Temp2;
    public int Press3;
    public int Temp3;
    public int Press4;
    public int Temp4;
    public int Press5;
    public int Temp5;
}
[EdfSerializable(id: 1016)]
public struct ChartMultiData6
{
    public uint Time;
    public ushort VPwr;
    public ushort RIso;
    public int Press1;
    public int Temp1;
    public int Press2;
    public int Temp2;
    public int Press3;
    public int Temp3;
    public int Press4;
    public int Temp4;
    public int Press5;
    public int Temp5;
    public int Press6;
    public int Temp6;
}
[EdfSerializable(id: 1017)]
public struct ChartMultiData7
{
    public uint Time;
    public ushort VPwr;
    public ushort RIso;
    public int Press1;
    public int Temp1;
    public int Press2;
    public int Temp2;
    public int Press3;
    public int Temp3;
    public int Press4;
    public int Temp4;
    public int Press5;
    public int Temp5;
    public int Press6;
    public int Temp6;
    public int Press7;
    public int Temp7;
}
[EdfSerializable(id: 1018)]
public struct ChartMultiData8
{
    public uint Time;
    public ushort VPwr;
    public ushort RIso;
    public int Press1;
    public int Temp1;
    public int Press2;
    public int Temp2;
    public int Press3;
    public int Temp3;
    public int Press4;
    public int Temp4;
    public int Press5;
    public int Temp5;
    public int Press6;
    public int Temp6;
    public int Press7;
    public int Temp7;
    public int Press8;
    public int Temp8;
}
