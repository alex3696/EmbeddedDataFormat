using NetEdf.StoreTypes;

namespace NetEdfTest;

[TestClass]
public class VarIntTest
{
    public delegate int EncodeDelegate<T>(T val, Span<byte> buf);
    public delegate int DecodeDelegate<T>(ReadOnlySpan<byte> buf, out T val);

    public static void CheckEncodeDecode<T>(T val, int byteQty, EncodeDelegate<T> encode, DecodeDelegate<T> decode)
    {
        var sp = new byte[VarInt.maxVarintBytes].AsSpan();
        Assert.AreEqual(byteQty, encode(val, sp));
        Assert.AreEqual(byteQty, decode(sp, out var valDec));
        Assert.AreEqual(val, valDec);
    }
    [TestMethod]
    public void Test_CheckEncSize()
    {
        Assert.AreEqual(1, VarInt.GetEncSize(VarInt.MaxUInt1b));
        Assert.AreEqual(2, VarInt.GetEncSize(VarInt.MaxUInt2b));
        Assert.AreEqual(3, VarInt.GetEncSize(VarInt.MaxUInt3b));
        Assert.AreEqual(4, VarInt.GetEncSize(VarInt.MaxUInt4b));
        Assert.AreEqual(5, VarInt.GetEncSize(VarInt.MaxUInt5b));
        Assert.AreEqual(6, VarInt.GetEncSize(VarInt.MaxUInt6b));
        Assert.AreEqual(7, VarInt.GetEncSize(VarInt.MaxUInt7b));
        Assert.AreEqual(8, VarInt.GetEncSize(VarInt.MaxUInt8b));
        Assert.AreEqual(9, VarInt.GetEncSize(VarInt.MaxUInt9b));

        Assert.AreEqual(3, VarInt.GetEncSize(ushort.MaxValue));
        Assert.AreEqual(3, VarInt.GetEncSize(short.MaxValue));
        Assert.AreEqual(3, VarInt.GetEncSize(short.MinValue));
        Assert.AreEqual(5, VarInt.GetEncSize(uint.MaxValue));
        Assert.AreEqual(5, VarInt.GetEncSize(int.MaxValue));
        Assert.AreEqual(5, VarInt.GetEncSize(int.MinValue));
        Assert.AreEqual(10, VarInt.GetEncSize(ulong.MaxValue));
        Assert.AreEqual(10, VarInt.GetEncSize(long.MaxValue));
        Assert.AreEqual(10, VarInt.GetEncSize(long.MinValue));
    }
    [TestMethod]
    public void Test_ChechStdCheckEncodeDecode()
    {
        CheckEncodeDecode((ushort)VarInt.MaxUInt1b, 1, VarInt.Encode, VarInt.Decode);
        CheckEncodeDecode(VarInt.MaxUInt2b, 2, VarInt.Encode, VarInt.Decode);
        CheckEncodeDecode(VarInt.MaxUInt3b, 3, VarInt.Encode, VarInt.Decode);
        CheckEncodeDecode(VarInt.MaxUInt4b, 4, VarInt.Encode, VarInt.Decode);
        CheckEncodeDecode(VarInt.MaxUInt5b, 5, VarInt.Encode, VarInt.Decode);
        CheckEncodeDecode(VarInt.MaxUInt6b, 6, VarInt.Encode, VarInt.Decode);
        CheckEncodeDecode(VarInt.MaxUInt7b, 7, VarInt.Encode, VarInt.Decode);
        CheckEncodeDecode(VarInt.MaxUInt8b, 8, VarInt.Encode, VarInt.Decode);
        CheckEncodeDecode(VarInt.MaxUInt9b, 9, VarInt.Encode, VarInt.Decode);

        CheckEncodeDecode(ushort.MaxValue, 3, VarInt.Encode, VarInt.Decode);
        CheckEncodeDecode(short.MinValue, 3, VarInt.Encode, VarInt.Decode);
        CheckEncodeDecode(short.MaxValue, 3, VarInt.Encode, VarInt.Decode);

        CheckEncodeDecode(uint.MaxValue, 5, VarInt.Encode, VarInt.Decode);
        CheckEncodeDecode(int.MinValue, 5, VarInt.Encode, VarInt.Decode);
        CheckEncodeDecode(int.MaxValue, 5, VarInt.Encode, VarInt.Decode);

        CheckEncodeDecode(ulong.MaxValue, 10, VarInt.Encode, VarInt.Decode);
        CheckEncodeDecode(long.MinValue, 10, VarInt.Encode, VarInt.Decode);
        CheckEncodeDecode(long.MaxValue, 10, VarInt.Encode, VarInt.Decode);
    }
    [TestMethod]
    public void Test_CheckErrBufOverflow()
    {
        byte[] v = [0x00];

        byte[] t = v.AsSpan(0, 0).ToArray();

        var res = VarInt.EncodeInt64(long.MaxValue, v);
        Assert.AreEqual(VarInt.ErrBufOverflow, res);
        res = VarInt.EncodeInt32(int.MaxValue, v);
        Assert.AreEqual(VarInt.ErrBufOverflow, res);
        res = VarInt.EncodeInt16(short.MaxValue, v);
        Assert.AreEqual(VarInt.ErrBufOverflow, res);
    }
    [TestMethod]
    public void Test_CheckErrBufTooSmall()
    {
        int res;
        res = VarInt.DecodeUInt64([0xFF], out var bMaxUlong);
        Assert.AreEqual(VarInt.ErrBufTooSmall, res);
        Assert.AreEqual((ulong)0xFF, bMaxUlong);

        res = VarInt.DecodeUInt32([0xFF], out var bMaxUint);
        Assert.AreEqual(VarInt.ErrBufTooSmall, res);
        Assert.AreEqual((uint)0xFF, bMaxUint);

        res = VarInt.DecodeUInt16([0xFF], out var bMaxUshort);
        Assert.AreEqual(VarInt.ErrBufTooSmall, res);
        Assert.AreEqual((ushort)0xFF, bMaxUshort);
    }
    [TestMethod]
    public void Test_CheckOverflowValue()
    {
        int res;
        res = VarInt.DecodeUInt64([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF], out var bMaxUlong);
        Assert.AreEqual(VarInt.ErrValueOverflow, res);

        res = VarInt.DecodeUInt32([0xFF, 0xFF, 0xFF, 0xFF, 0xFF], out var bMaxUint);
        Assert.AreEqual(VarInt.ErrValueOverflow, res);

        res = VarInt.DecodeUInt16([0xFF, 0xFF, 0x02], out var bMaxUshort);
        Assert.AreEqual(VarInt.ErrValueOverflow, res);
    }
    [TestMethod]
    public void Test_ChechStdMinMaxValues()
    {
        // UInt16
        var bMaxUshort = VarInt.EncodeUInt16(ushort.MaxValue);
        VarInt.DecodeUInt16(bMaxUshort, out var bMaxUshortDec);
        Assert.AreEqual(ushort.MaxValue, bMaxUshortDec);
        // Int16
        var bMinShort = VarInt.EncodeInt16(short.MinValue);
        VarInt.DecodeInt16(bMinShort, out var bMinShortDec);
        Assert.AreEqual(short.MinValue, bMinShortDec);
        var bMaxShort = VarInt.EncodeInt16(short.MaxValue);
        VarInt.DecodeInt16(bMaxShort, out var bMaxShortDec);
        Assert.AreEqual(short.MaxValue, bMaxShortDec);
        // UInt32
        var bMaxUint = VarInt.EncodeUInt32(uint.MaxValue);
        VarInt.DecodeUInt32(bMaxUint, out var bMaxUintDec);
        Assert.AreEqual(uint.MaxValue, bMaxUintDec);
        // Int32
        var bMinInt = VarInt.EncodeInt32(int.MinValue);
        VarInt.DecodeInt32(bMinInt, out var bMinIntDec);
        Assert.AreEqual(int.MinValue, bMinIntDec);
        var bMaxInt = VarInt.EncodeInt32(int.MaxValue);
        VarInt.DecodeInt32(bMaxInt, out var bMaxIntDec);
        Assert.AreEqual(int.MaxValue, bMaxIntDec);
        // UInt64
        var bMaxUlong = VarInt.EncodeUInt64(ulong.MaxValue);
        VarInt.DecodeUInt64(bMaxUlong, out var maxUlongDec);
        Assert.AreEqual(ulong.MaxValue, maxUlongDec);
        // Int64
        var bMinlong = VarInt.EncodeInt64(long.MinValue);
        VarInt.DecodeInt64(bMinlong, out var minLongDec);
        Assert.AreEqual(long.MinValue, minLongDec);
        var bMaxlong = VarInt.EncodeInt64(long.MaxValue);
        VarInt.DecodeInt64(bMaxlong, out var maxlongDec);
        Assert.AreEqual(long.MaxValue, maxlongDec);
    }
    [TestMethod]
    public void Test_CalcSizes()
    {
        byte[] v = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F];

        var bMaxUlong = VarInt.EncodeUInt64(ulong.MaxValue);

        for (var i = 1; i < v.Length + 1; ++i)
        {
            VarInt.DecodeUInt64(v.AsSpan(v.Length - i, i).ToArray(), out var result);
            Console.WriteLine($"{i:D2}: max unsigned value:{result}");
        }
        for (var i = 1; i < v.Length + 1; ++i)
        {
            VarInt.DecodeInt64(v.AsSpan(v.Length - i, i).ToArray(), out var neg);
            long pos = long.Abs(neg + 1);
            var bb = VarInt.EncodeInt64(pos);
            Console.WriteLine($"{i} min: {neg} max: {pos}");
        }
    }
    [TestMethod]
    public void Test_EncodeDecode_UInt64()
    {
        UInt64 src = 50000;
        byte[] b = VarInt.EncodeUInt64(src);

        Assert.IsLessThan(VarInt.DecodeUInt64(b, out UInt64 sr), 0);
        Assert.AreEqual(src, sr);

        Assert.IsLessThan(VarInt.DecodeUInt64([0x00], out UInt64 u0), 0);
        Assert.AreEqual((UInt64)0, u0);

        Assert.IsLessThan(VarInt.DecodeUInt64([0x80, 0x00], out UInt64 u1), 0);
        Assert.AreEqual((UInt64)128, u1);

        Assert.IsLessThan(VarInt.DecodeUInt64([0x80, 0x80, 0x00], out UInt64 u2), 0);
        Assert.AreEqual((UInt64)16512, u2);
    }
    [TestMethod]
    public void Test_DecodeDecode_UInt64()
    {
        byte[] buf = new byte[10];
        int b = 0;

        b = VarInt.EncodeUInt64((ulong)0, buf);
        Assert.IsTrue(1 == b && Enumerable.SequenceEqual<byte>([0x00], buf.AsSpan(0, 1).ToArray()));

        b = VarInt.EncodeUInt64((ulong)128, buf);
        Assert.IsTrue(2 == b && Enumerable.SequenceEqual<byte>([0x80, 0x00], buf.AsSpan(0, 2).ToArray()));

        b = VarInt.EncodeUInt64((ulong)16512, buf);
        Assert.IsTrue(3 == b && Enumerable.SequenceEqual<byte>([0x80, 0x80, 0x00], buf.AsSpan(0, 3).ToArray()));
    }

}

