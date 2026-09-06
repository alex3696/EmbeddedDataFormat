namespace Test.BinSiamFormat;

[DebuggerDisplay("{Dt,nq}")]
[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct SiamTime
{
    ByteArray6 _rawTime;
    public DateTime Dt
    {
        get => Utils.FromByteshhmmssDDMMYY(_rawTime);
        set => Utils.ToByteshhmmssDDMMYY(value, _rawTime);
    }
}
