namespace EdfSiam.Common;

[EdfSerializable]
public class ChartNType
{
    public string? Name;
    public string? Unit;
    public string? ApiCode;
    public string? Desc;
}

[EdfSerializable]
public struct Chart2D
{
    public float x;
    public float y;
}
[EdfSerializable(id: (ushort)SchemaId.OMEGADATA)]
public struct OmegaData_v1_1
{
    public uint Time;
    public int Press;
    public int Temp;
    public ushort Vbat;
}
