namespace NetEdf.StoreTypes;

public class FpFloat
{
    public const int accuracy = 6;
    byte _signpow = 0;
    byte[] _var = [];

    public bool IsNegative => 0 < (byte)(_signpow & 0x80);
    public byte Pow => (byte)(_signpow & 0x7F);

    public FpFloat(byte[] num, byte signpow = 0)
    {
        _var = num;
        _signpow = signpow;
    }

    public static FpFloat Parse(double d, int maxAccuracy = accuracy)
    {
        byte signpow = 0;
        if (double.IsNegative(d))
            signpow |= 0x80;

        d = double.Abs(d);

        ulong ui64 = Convert.ToUInt64(d * SimplePower(10, maxAccuracy));
        ulong delim = 10;
        var rest = ui64 % delim;
        while (0 == rest)
        {
            maxAccuracy--;
            delim *= 10;
            rest = ui64 % delim;
        }

        signpow |= (byte)(0x7F & maxAccuracy);
        ui64 = Convert.ToUInt64(d * SimplePower(10, maxAccuracy));
        byte[] varint = VarInt.EncodeUInt64(ui64);
        return new FpFloat(varint, signpow);
    }

    public double ToDouble()
    {
        if (_var.Length == VarInt.DecodeUInt64(_var, out ulong ui64))
        {
            double d = Convert.ToDouble(ui64);
            if (IsNegative)
                d *= -1;
            d /= SimplePower(10, Pow);
            return d;
        }
        return default;
    }

    public static int SimplePower(int x, int pow)
    {
        return (int)Math.Pow(x, pow);
    }



}
