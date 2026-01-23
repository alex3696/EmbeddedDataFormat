using NetEdf.StoreTypes;

namespace NetEdfTest;

[TestClass]
public class VarIntTest
{


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
        Assert.AreEqual(10, VarInt.GetEncSize(ulong.MaxValue));
        Assert.AreEqual(10, VarInt.GetEncSize(long.MaxValue));
        Assert.AreEqual(10, VarInt.GetEncSize(long.MinValue));

        var sp = new byte[VarInt.maxVarintBytes].AsSpan();
        Assert.AreEqual(1, VarInt.EncodeUInt16(VarInt.MaxUInt1b,sp));
        Assert.AreEqual(2, VarInt.EncodeUInt16(VarInt.MaxUInt2b, sp));
        Assert.AreEqual(3, VarInt.EncodeUInt32(VarInt.MaxUInt3b, sp));
        Assert.AreEqual(4, VarInt.EncodeUInt32(VarInt.MaxUInt4b, sp));
        Assert.AreEqual(5, VarInt.EncodeUInt64(VarInt.MaxUInt5b, sp));
        Assert.AreEqual(6, VarInt.EncodeUInt64(VarInt.MaxUInt6b, sp));
        Assert.AreEqual(7, VarInt.EncodeUInt64(VarInt.MaxUInt7b, sp));
        Assert.AreEqual(8, VarInt.EncodeUInt64(VarInt.MaxUInt8b, sp));
        Assert.AreEqual(9, VarInt.EncodeUInt64(VarInt.MaxUInt9b, sp));
        Assert.AreEqual(10, VarInt.EncodeUInt64(ulong.MaxValue, sp));
        Assert.AreEqual(10, VarInt.EncodeInt64(long.MaxValue, sp));
        Assert.AreEqual(10, VarInt.EncodeInt64(long.MinValue, sp));
    }

    [TestMethod]
    public void Test_ChechErrBufOverflow()
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
    public void Test_ChechErrBufTooSmall()
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
    public void Test_CheckOverflowValues()
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
    public void Test_ChechMaxValues()
    {
        var bMaxUlong = VarInt.EncodeUInt64(ulong.MaxValue);
        VarInt.DecodeUInt64(bMaxUlong, out var maxUlongDec);
        Assert.AreEqual(ulong.MaxValue, maxUlongDec);

        var bMaxUint = VarInt.EncodeUInt32(uint.MaxValue);
        VarInt.DecodeUInt32(bMaxUint, out var bMaxUintDec);
        Assert.AreEqual(uint.MaxValue, bMaxUintDec);

        var bMaxUshort = VarInt.EncodeUInt16(ushort.MaxValue);
        VarInt.DecodeUInt16(bMaxUshort, out var bMaxUshortDec);
        Assert.AreEqual(ushort.MaxValue, bMaxUshortDec);

        var bMaxlong = VarInt.EncodeInt64(long.MaxValue);
        VarInt.DecodeInt64(bMaxlong, out var maxlongDec);
        Assert.AreEqual(long.MaxValue, maxlongDec);

        var bMaxInt = VarInt.EncodeInt32(int.MaxValue);
        VarInt.DecodeInt32(bMaxInt, out var bMaxIntDec);
        Assert.AreEqual(int.MaxValue, bMaxIntDec);

        var bMaxShort = VarInt.EncodeInt16(short.MaxValue);
        VarInt.DecodeInt16(bMaxShort, out var bMaxShortDec);
        Assert.AreEqual(short.MaxValue, bMaxShortDec);

        var bMinlong = VarInt.EncodeInt64(long.MinValue);
        VarInt.DecodeInt64(bMinlong, out var minLongDec);
        Assert.AreEqual(long.MinValue, minLongDec);

        var bMinInt = VarInt.EncodeInt32(int.MinValue);
        VarInt.DecodeInt32(bMinInt, out var bMinIntDec);
        Assert.AreEqual(int.MinValue, bMinIntDec);

        var bMinShort = VarInt.EncodeInt16(short.MinValue);
        VarInt.DecodeInt16(bMinShort, out var bMinShortDec);
        Assert.AreEqual(short.MinValue, bMinShortDec);

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
        Assert.IsTrue(0 < b && Enumerable.SequenceEqual<byte>([0x00], buf.AsSpan(0, 1).ToArray()));

        b = VarInt.EncodeUInt64((ulong)128, buf);
        Assert.IsTrue(0 < b && Enumerable.SequenceEqual<byte>([0x80, 0x00], buf.AsSpan(0, 2).ToArray()));

        b = VarInt.EncodeUInt64((ulong)16512, buf);
        Assert.IsTrue(0 < b && Enumerable.SequenceEqual<byte>([0x80, 0x80, 0x00], buf.AsSpan(0, 3).ToArray()));
    }

    [TestMethod]
    public void Test_EncodeDecode_Int64()
    {
        Int64 src = -50000;
        byte[] b = VarInt.EncodeInt64(src);

        Assert.IsLessThan(VarInt.DecodeInt64(b, out Int64 sr), 0);
        Assert.AreEqual(src, sr);
    }

    [TestMethod]
    public void Test_CalcMaxSizes()
    {

        //var decoded1 = VarInt.DecodeUInt64([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F], out ulong lp1);
        //var decoded2 = VarInt.DecodeUInt64([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x00], out ulong lp2);
        //var decoded3 = VarInt.DecodeUInt64([0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x01], out ulong lp3);

        var sp = new byte[10].AsSpan(0, 10);
        var encoded = VarInt.EncodeUInt64(ulong.MaxValue, sp);

        var decoded4 = VarInt.DecodeUInt64(sp, out ulong lp4);




        /*
        byte[] src = [0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0x7F];
        var decoded = VarInt.DecodeUInt64(src, out ulong lp);
        lp+=0xFFFF;
        var sm = new byte[10].AsSpan(0, 10);
        var encoded = VarInt.EncodeUInt64(lp, sm);
        decoded = VarInt.DecodeUInt64(sm, out ulong lp2);
        */

        /*
        var sp = new byte[10].AsSpan(0, 10);
        var sm = new byte[10].AsSpan(0, 10);
        for (ulong i = 1; i <= uint.MaxValue; ++i)
        {
            //Array.Clear(v, 0, v.Length);
            var bp = VarInt.EncodeUInt64(VarInt.MaxInt9b + i, sp);

            var decoded = VarInt.DecodeUInt64(sp, out ulong lp);

            var bm = VarInt.EncodeUInt64(VarInt.MaxInt9b - i, sm);
            Console.WriteLine($" b");
        }
        */

        /*
        int b = 0;
        var sp = new byte[10].AsSpan(0,10);
        for (ulong i = 1; i <= uint.MaxValue; ++i)
        {
            //Array.Clear(v, 0, v.Length);
            var bnow = VarInt.EncodeUInt64(i, sp);
            if(bnow != b)
            {
                Console.WriteLine($"{(i-1):D2}: max unsigned value:{b}");
                b=bnow;
            }
        }
        */
    }

}

