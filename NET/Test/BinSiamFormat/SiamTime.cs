namespace Test.BinSiamFormat;

[DebuggerDisplay("{Dt,nq}")]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public readonly struct SiamTime
{
    public SiamTime()
    {
        TimeArray = new byte[6];
    }
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.U1)]
    public readonly byte[] TimeArray;
    public DateTime Dt
    {
        get => Utils.FromByteshhmmssDDMMYY(TimeArray);
        set => Utils.ToByteshhmmssDDMMYY(value, TimeArray);
    }
}
