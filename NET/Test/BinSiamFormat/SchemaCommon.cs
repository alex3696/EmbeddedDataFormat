namespace Test.BinSiamFormat;


[EdfSerializable(10)]
public class FileTypeId
{
    public ushort Type;
    public ushort Version;
}

[EdfSerializable(11, "BeginDateTime")]
public class DateTimeZ
{
    public ushort Year;
    public byte Month;
    public byte Day;
    public byte Hour;
    public byte Min;
    public byte Sec;
    public ushort mSec;
    public byte Tz;
}
