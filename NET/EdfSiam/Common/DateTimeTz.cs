namespace EdfSiam.Common;

[EdfSerializable((ushort)SchemaId.BEGINDATETIME, "BeginDateTime")]
public class DateTimeTz
{
    public DateTime ToDateTime()
    {
        return new DateTime(Year, Month, Day, Hour, Min, Sec, mSec, DateTimeKind.Unspecified);
    }
    public static DateTimeTz FromDateTime(DateTime date, sbyte tz = 0)
    {
        return new DateTimeTz()
        {
            Year = (ushort)date.Year,
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
    public sbyte Tz;
}
