namespace Test.BinSiamFormat;


[EdfSerializable(10)]
public class FileTypeId
{
    public ushort Type;
    public ushort Version;
}

[EdfSerializable(11, "BeginDateTime")]
public class DateTimeTz
{
    public static DateTimeTz FromDateTime(DateTime date, byte tz = 0)
    {
        return new DateTimeTz()
        {
            Year = (byte)(date.Year - 2000),
            Month = (byte)date.Month,
            Day = (byte)date.Day,
            Hour = (byte)date.Hour,
            Min = (byte)date.Minute,
            Sec = (byte)date.Second,
            mSec = (byte)date.Millisecond,
            Tz = tz
        };
    }

    public ushort Year;
    public byte Month;
    public byte Day;
    public byte Hour;
    public byte Min;
    public byte Sec;
    public ushort mSec;
    public byte Tz;
}

[EdfSerializable(11, "BeginDateTime")]
public class Position
{
    public string? Field;
    public string? Cluster;
    public string? Well;
    public string? Shop;
}
