namespace EdfConv.BinSiamFormat;

public enum StdSchemaType : ushort
{
    FILETYPEID = 10,
    BEGINDATETIME,
    POSITION,
    DEVICEINFO,
    REGINFO,

    OMEGADATA
}

[EdfSerializable((ushort)StdSchemaType.FILETYPEID)]
public class FileTypeId
{
    public ushort Type;
    public ushort Version;
}

[EdfSerializable((ushort)StdSchemaType.BEGINDATETIME, "BeginDateTime")]
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

[EdfSerializable((ushort)StdSchemaType.POSITION, "Position")]
public class Position
{
    public string? Field;
    public string? Cluster;
    public string? Well;
    public string? Shop;
}

[EdfSerializable((ushort)StdSchemaType.DEVICEINFO, "DevInfo")]
public class DeviceInfo
{
    public ushort SwId;
    public ushort SwModel;
    public ushort SwRevision;
    public ushort HwId;
    public ushort HwModel;
    public ulong HwNumber;
}

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
