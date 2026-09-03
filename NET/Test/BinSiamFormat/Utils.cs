using System.Collections.Specialized;

namespace Test.BinSiamFormat;

public static class Utils
{
    public const double EchoFixedSoundSpeed = 341.00d;
    public const double Discrete3000 = 0.00585938;
    public const double Discrete6000 = 0.01171875;

    public static bool IsDiscreteMaxDepth(double discrete) // Is6000
    {
        return Discrete3000 < discrete;
    }
    public static double ExtractDiscrete(UInt16 level)
    {
        BitVector32 myBV = new(level);
        return (true == myBV[0x4000]) ? Discrete6000 : Discrete3000;
    }
    public static double ExtractLevel(UInt16 val)
    {
        BitVector32 myBV = new(val);
        if (true == myBV[0x4000])
            myBV[0x4000] = false;
        return (ushort)myBV.Data;
    }
    public static double ExtractLevel(UInt16 val, double speed)
    {
        return ExtractLevel(val) * speed / EchoFixedSoundSpeed;
    }
    public static UInt16 ExtractReflections(UInt16 val)
    {
        // ахтунг! параметр передаётся в двоично десятичном виде :-(
        const UInt16 mask = 0x000F;
        UInt16 dec = (UInt16)(((val >> 4) & mask) * 10);
        UInt16 sig = (UInt16)(val & mask);
        int refect = dec + sig;
        return (refect > 99) ? (UInt16)99 : (UInt16)refect;
    }
    public static DateTime ExtractTimestamp(byte year2, byte month, byte day, byte hour, byte min, byte sec,
        DateTime fallback = default)
    {
        try
        {
            var dt = DateTime.Now;
            var epoh = dt.Year - (dt.Year % 100);
            var year = (100 > year2) ? epoh + year2 : year2;
            return new DateTime(year, month, day, hour, min, sec);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Exception: invalid date {ex}");
        }
        return fallback;
    }

    public static DateTime FromByteshhmmssDDMMYY(byte[] val, DateTime fallback = default)
        => ExtractTimestamp(val[5], val[4], val[3], val[0], val[1], val[2], fallback);
    public static void ToByteshhmmssDDMMYY(DateTime dt, byte[] arr)
    {
        arr[5] = (100 > dt.Year) ? (byte)dt.Year : (byte)(dt.Year % 100);
        arr[4] = (byte)dt.Month;
        arr[3] = (byte)dt.Day;
        arr[0] = (byte)dt.Hour;
        arr[1] = (byte)dt.Minute;
        arr[2] = (byte)dt.Second;
    }

    public static DateTime FromBytesYYMMDDhhmmss(byte[] val, DateTime fallback = default)
        => ExtractTimestamp(val[0], val[1], val[2], val[3], val[4], val[5], fallback);
    public static void ToBytesYYMMDDhhmmss(DateTime dt, byte[] arr)
    {
        arr[0] = (100 > dt.Year) ? (byte)dt.Year : (byte)(dt.Year % 100);
        arr[1] = (byte)dt.Month;
        arr[2] = (byte)dt.Day;
        arr[3] = (byte)dt.Hour;
        arr[4] = (byte)dt.Minute;
        arr[5] = (byte)dt.Second;
    }
}
