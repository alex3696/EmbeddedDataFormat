using NetEdf.src;
using Newtonsoft.Json.Linq;
using System.Buffers.Binary;

namespace NetEdfTest;

[TestClass]
public class TestPrimitives
{
    [TestMethod]
    public void TestSrcToBinRef()
    {
        Span<byte> dst = new byte[20];
        Span<byte> expected = [0x7B];

        var actual = Primitives.SrcToBinRef(ref dst, PoType.Int8, (sbyte)123);

        Assert.AreEqual(expected.Length, actual);
    }

    public void TrySrcBinToSrc<T>(PoType type, T value, Span<byte> expected)
        where T : struct
    {
        Span<byte> dst = new byte[10];

        var actual = Primitives.TrySrcToBin(type, value, dst, out int w);

        Assert.AreEqual(EdfErr.IsOk, actual);
        Assert.AreEqual(expected.Length, w);
        Assert.IsTrue(dst.Slice(0, w).SequenceEqual(expected));

        actual = Primitives.TryBinToSrc(type, dst, out int r, out object? obj);

        Assert.AreEqual(EdfErr.IsOk, actual);
        Assert.AreEqual(expected.Length, r);
        Assert.AreEqual(value, obj);
    }
    public void TrySrcToBin_ErrDstBufOverflow(PoType type, object obj, Span<byte> dst)
    {
        var actual = Primitives.TrySrcToBin(type, obj, dst, out int w);

        Assert.AreEqual(EdfErr.DstBufOverflow, actual);
    }
    public void TrySrcToBin_ErrWrongType(PoType type, object obj, Span<byte> dst)
    {
        var actual = Primitives.TrySrcToBin(type, obj, dst, out int w);

        Assert.AreEqual(EdfErr.WrongType, actual);
    }
    public void TrySrcBin_ObjIsString(PoType type, object value, Span<byte> expected)
    {
        Span<byte> dst = new byte[20];

        var actual = Primitives.TrySrcToBin(type, value, dst, out int w);

        Assert.AreEqual(EdfErr.IsOk, actual);
        Assert.AreEqual(expected.Length, w);
        Assert.IsTrue(dst.Slice(0, w).SequenceEqual(expected));

        actual = Primitives.TryBinToSrc(type, dst, out int r, out object? obj);

        Assert.AreEqual(EdfErr.IsOk, actual);
        Assert.AreEqual(expected.Length, r);
        Assert.AreEqual(value, obj);
    }


    [TestMethod]
    public void TestSrcToBinToSrc_Err()
    {
        TrySrcToBin_ErrDstBufOverflow(PoType.Int16, 123, new byte[1]);
        TrySrcToBin_ErrDstBufOverflow(PoType.String, "1234", new byte[1]);

    }

    [TestMethod]
    public void TestSrcToBinToSrc_Struct()
    {
        TrySrcToBin_ErrWrongType(PoType.Struct, 1, new byte[1]);
    }

    [TestMethod]
    public void TestSrcToBinToSrc_Int8()
    {
        TrySrcBinToSrc(PoType.Int8, sbyte.MaxValue, [0x7F]);
        TrySrcBinToSrc(PoType.Int8, sbyte.MinValue, [0x80]);
        TrySrcBinToSrc(PoType.Int8, (sbyte)-123, [0x85]);
        TrySrcBinToSrc(PoType.Int8, (sbyte)123, [0x7B]);
    }

    [TestMethod]
    public void TestSrcToBinToSrc_Unt8()
    {
        TrySrcBinToSrc(PoType.UInt8, byte.MinValue, [0x00]);
        TrySrcBinToSrc(PoType.UInt8, byte.MaxValue, [0xFF]);
        TrySrcBinToSrc(PoType.UInt8, (byte)123, [0x7B]);
    }

    [TestMethod]
    public void TestSrcToBinToSrc_Int16()
    {
        TrySrcBinToSrc(PoType.Int16, short.MaxValue, [0xFF, 0x7F]);
        TrySrcBinToSrc(PoType.Int16, short.MinValue, [0x00, 0x80]);
        TrySrcBinToSrc(PoType.Int16, (short)20520, [0x28, 0x50]);
        TrySrcBinToSrc(PoType.Int16, (short)-20520, [0xD8, 0xAF]);
    }
    [TestMethod]
    public void TestSrcToBinToSrc_UInt16()
    {
        TrySrcBinToSrc(PoType.UInt16, ushort.MaxValue, [0xFF, 0xFF]);
        TrySrcBinToSrc(PoType.UInt16, ushort.MinValue, [0x00, 0x00]);
        TrySrcBinToSrc(PoType.UInt16, (ushort)45662, [0x5E, 0xB2]);
    }

    [TestMethod]
    public void TestSrcToBinToSrc_UInt32()
    {
        TrySrcBinToSrc(PoType.UInt32, uint.MaxValue, [0xff, 0xff, 0xff, 0xff]);
        TrySrcBinToSrc(PoType.UInt32, uint.MinValue, [0x00, 0x00, 0x00, 0x00]);
        TrySrcBinToSrc(PoType.UInt32, (uint)123456, [0x40, 0xE2, 0x01, 0x00]);
    }

    [TestMethod]
    public void TestSrcToBinToSrc_Int32()
    {
        TrySrcBinToSrc(PoType.Int32, int.MaxValue, [0xff, 0xff, 0xff, 0x7f]);
        TrySrcBinToSrc(PoType.Int32, int.MinValue, [0x00, 0x00, 0x00, 0x80]);
        TrySrcBinToSrc(PoType.Int32, (int)123456, [0x40, 0xE2, 0x01, 0x00]);
        TrySrcBinToSrc(PoType.Int32, (int)-123456, [0xC0, 0x1D, 0xFE, 0xFF]);
    }

    [TestMethod]
    public void TestSrcToBinToSrc_UInt64()
    {
        TrySrcBinToSrc(PoType.UInt64, ulong.MaxValue, [0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff]);
        TrySrcBinToSrc(PoType.UInt64, ulong.MinValue, [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00]);
        TrySrcBinToSrc(PoType.UInt64, (ulong)652212, [0xB4, 0xF3, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00]);
    }

    [TestMethod]
    public void TestSrcToBinToSrc_Int64()
    {
        TrySrcBinToSrc(PoType.Int64, long.MaxValue, [0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0x7f]);
        TrySrcBinToSrc(PoType.Int64, long.MinValue, [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x80]);
        TrySrcBinToSrc(PoType.Int64, (long)652212, [0xB4, 0xF3, 0x09, 0x00, 0x00, 0x00, 0x00, 0x00]);
        TrySrcBinToSrc(PoType.Int64, (long)-652212, [0x4C, 0x0C, 0xF6, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);
    }


    [TestMethod]
    public void TestSrcToBinToSrc_Half()
    {
        TrySrcBinToSrc(PoType.Half, Half.MaxValue, [0xFF, 0x7B]);
        TrySrcBinToSrc(PoType.Half, Half.MinValue, [0xFF, 0xFB]);
        TrySrcBinToSrc(PoType.Half, (Half)6.75, [0xC0, 0x46]);
        TrySrcBinToSrc(PoType.Half, (Half)(-6.75), [0xC0, 0xC6]);
    }

    [TestMethod]
    public void TestSrcToBinToSrc_Single()
    {
        TrySrcBinToSrc(PoType.Single, float.MaxValue, [0xFF, 0xFF, 0x7F, 0x7F]);
        TrySrcBinToSrc(PoType.Single, float.MinValue, [0xFF, 0xFF, 0x7F, 0xFF]);
        TrySrcBinToSrc(PoType.Single, (float)6.75, [0x00, 0x00, 0xD8, 0x40]);
        TrySrcBinToSrc(PoType.Single, (float)-6.75, [0x00, 0x00, 0xD8, 0xC0]);
    }

    [TestMethod]
    public void TestSrcToBinToSrc_Double()
    {
        TrySrcBinToSrc(PoType.Double, double.MaxValue, [0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xef, 0x7f]);
        TrySrcBinToSrc(PoType.Double, double.MinValue, [0xff, 0xff, 0xff, 0xff, 0xff, 0xff, 0xef, 0xff]);
        TrySrcBinToSrc(PoType.Double, (double)6.75, [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1B, 0x40]);
        TrySrcBinToSrc(PoType.Double, (double)-6.75, [0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x1B, 0xC0]);
    }

    [TestMethod]
    public void TestSrcToBinToSrc_String()
    {
        TrySrcBin_ObjIsString(PoType.String, "aaaaa S", [0x07, 0x61, 0x61, 0x61, 0x61, 0x61, 0x20, 0x53]);
    }

    public void SrcToTextToSrc<T>(PoType type, T obj, Span<byte> expected)
        where T : notnull
    {
        Span<byte> dst = new byte[40];

        var actual = Primitives.TrySrcToTxt(type, obj, dst, out int w);

        Assert.AreEqual(EdfErr.IsOk, actual);
        Assert.AreEqual(expected.Length, w);
        Assert.IsTrue(dst.Slice(0, w).SequenceEqual(expected));

        actual = Primitives.TryTxtToSrc(type, expected,out object value);
        Assert.AreEqual(EdfErr.IsOk, actual);
        Assert.AreEqual(obj, value);

    }
    public void TryToTxtChar(PoType type, object obj, Span<byte> expected)
    {
        Span<byte> dst = new byte[40];

        var actual = Primitives.TrySrcToTxt(type, obj, dst, out int w);

        Assert.AreEqual(EdfErr.IsOk, actual);
        Assert.AreEqual(expected.Length, w);
        Assert.IsTrue(dst.Slice(0, w).SequenceEqual(expected));
    }

    public void TryTxtToSrcChar(PoType type, object obj, Span<byte> expected)
    {
        Span<byte> dst = new byte[40];

        var actual = Primitives.TryTxtToSrc(type, expected, out object value);

        Assert.AreEqual(EdfErr.IsOk, actual);
        Assert.AreEqual(EdfErr.IsOk, actual);
        Assert.AreEqual(obj, value);
    }
    /*[TestMethod]
    public void TestTrySrcToTxt_Char() // сделать текстовый
    {
        char ch = 'A';
        var len = sizeof(char);
        byte[] arr = new byte[10];
        BinaryPrimitives.WriteUInt16LittleEndian(arr, ch);

        TryToTxtChar(PoType.Char, arr[0], [0x27, arr[0], 0x27]);

        TryTxtToSrcChar(PoType.Char, 'A', [0x27, arr[0], 0x27]);
    }*/

    [TestMethod]
    public void TestTrySrcToTxt_Struct()
    {
        PoType type = PoType.Struct;
        object obj = 1;
        Span<byte> bytes = new byte[1];

        var actual = Primitives.TrySrcToTxt(type, obj, bytes, out int w);

        Assert.AreEqual(EdfErr.WrongType, actual);

    }

    [TestMethod]
    public void TestTrySrcToTxt_UInt8()
    {
        SrcToTextToSrc(PoType.UInt8, byte.MinValue, [0x30]);
        SrcToTextToSrc(PoType.UInt8, byte.MaxValue, [0x32, 0x35, 0x35]);
        SrcToTextToSrc(PoType.UInt8, (byte)123, [0x31, 0x32, 0x33]);
    }

    [TestMethod]
    public void TestTrySrcToTxt_Int8()
    {
        SrcToTextToSrc(PoType.Int8, sbyte.MinValue, [0x2D, 0x31, 0x32, 0x38]);
        SrcToTextToSrc(PoType.Int8, sbyte.MaxValue, [0x31, 0x32, 0x37]);
        SrcToTextToSrc(PoType.Int8, (sbyte)123, [0x31, 0x32, 0x33]);
        SrcToTextToSrc(PoType.Int8, (sbyte)-123, [0x2D, 0x31, 0x32, 0x33]);
    }

    [TestMethod]
    public void TestTrySrcToTxt_UInt16()
    {
        SrcToTextToSrc(PoType.UInt16, ushort.MinValue, [0x30]);
        SrcToTextToSrc(PoType.UInt16, ushort.MaxValue, [0x36, 0x35, 0x35, 0x33, 0x35]);
        SrcToTextToSrc(PoType.UInt16, (ushort)1233, [0x31, 0x32, 0x33, 0x33]);
    }

    [TestMethod]
    public void TestTrySrcToTxt_Int16()
    {
        SrcToTextToSrc(PoType.Int16, short.MinValue, [0x2D, 0x33, 0x32, 0x37, 0x36, 0x38]);
        SrcToTextToSrc(PoType.Int16, short.MaxValue, [0x33, 0x32, 0x37, 0x36, 0x37]);
        SrcToTextToSrc(PoType.Int16, (short)1233, [0x31, 0x32, 0x33, 0x33]);
        SrcToTextToSrc(PoType.Int16, (short)-1233, [0x2D, 0x31, 0x32, 0x33, 0x33]);
    }

    [TestMethod]
    public void TestTrySrcToTxt_UInt32()
    {
        SrcToTextToSrc(PoType.UInt32, uint.MinValue, [0x30]);
        SrcToTextToSrc(PoType.UInt32, uint.MaxValue, [0x34, 0x32, 0x39, 0x34, 0x39, 0x36,
            0x37, 0x32, 0x39, 0x35]);
        SrcToTextToSrc(PoType.UInt32, (uint)12333, [0x31, 0x32, 0x33, 0x33, 0x33]);
    }

    [TestMethod]
    public void TestTrySrcToTxt_Int32()
    {
        SrcToTextToSrc(PoType.Int32, int.MinValue, [0x2D, 0x32, 0x31, 0x34, 0x37, 0x34,
        0x38, 0x33, 0x36, 0x34, 0x38]);
        SrcToTextToSrc(PoType.Int32, int.MaxValue, [0x32, 0x31, 0x34, 0x37, 0x34,
        0x38, 0x33, 0x36, 0x34, 0x37]);
        SrcToTextToSrc(PoType.Int32, (int)12333, [0x31, 0x32, 0x33, 0x33, 0x33]);
        SrcToTextToSrc(PoType.Int32, (int)-12333, [0x2D, 0x31, 0x32, 0x33, 0x33, 0x33]);
    }

    [TestMethod]
    public void TestTrySrcToTxt_UInt64()
    {
        SrcToTextToSrc(PoType.UInt64, ulong.MinValue, [0x30]);
        SrcToTextToSrc(PoType.UInt64, ulong.MaxValue, [0x31, 0x38, 0x34, 0x34, 0x36,
        0x37, 0x34, 0x34, 0x30, 0x37, 0x33, 0x37, 0x30, 0x39, 0x35, 0x35, 0x31, 0x36, 0x31, 0x35]);
        SrcToTextToSrc(PoType.UInt64, (ulong)12333333, [0x31, 0x32, 0x33, 0x33, 0x33, 0x33, 0x33, 0x33]);
    }

    [TestMethod]
    public void TestTrySrcToTxt_Int64()
    {
        SrcToTextToSrc(PoType.Int64, long.MinValue, [0x2D, 0x39, 0x32, 0x32, 0x33, 0x33, 0x37, 0x32,
               0x30, 0x33, 0x36, 0x38, 0x35, 0x34, 0x37, 0x37, 0x35, 0x38, 0x30, 0x38]);
        SrcToTextToSrc(PoType.Int64, long.MaxValue, [0x39, 0x32, 0x32, 0x33, 0x33, 0x37, 0x32,
               0x30, 0x33, 0x36, 0x38, 0x35, 0x34, 0x37, 0x37, 0x35, 0x38, 0x30, 0x37]);
        SrcToTextToSrc(PoType.Int64, (long)12333333, [0x31, 0x32, 0x33, 0x33, 0x33, 0x33, 0x33, 0x33]);
        SrcToTextToSrc(PoType.Int64, (long)-12333333, [0x2D, 0x31, 0x32, 0x33, 0x33, 0x33, 0x33, 0x33, 0x33]);
    }

    [TestMethod]
    public void TestTrySrcToTxt_Half()
    {
        SrcToTextToSrc(PoType.Half, Half.MinValue, [0x2D, 0x36, 0x35, 0x35, 0x30, 0x30]);
        SrcToTextToSrc(PoType.Half, Half.MaxValue, [0x36, 0x35, 0x35, 0x30, 0x30]);
        SrcToTextToSrc(PoType.Half, (Half)6.75, [0x36, 0x2E, 0x37, 0x35]);
        SrcToTextToSrc(PoType.Half, (Half)(-6.75), [0x2D, 0x36, 0x2E, 0x37, 0x35]);
    }

    [TestMethod]
    public void TestTrySrcToTxt_Single()
    {
        SrcToTextToSrc(PoType.Single, float.MinValue, [0x2D, 0x33, 0x2e, 0x34, 0x30,
            0x32, 0x38, 0x32, 0x33, 0x35, 0x45, 0x2B, 0x33, 0x38]);
        SrcToTextToSrc(PoType.Single, float.MaxValue, [0x33, 0x2e, 0x34, 0x30,
            0x32, 0x38, 0x32, 0x33, 0x35, 0x45, 0x2B, 0x33, 0x38]);
        SrcToTextToSrc(PoType.Single, (float)6.75, [0x36, 0x2E, 0x37, 0x35]);
        SrcToTextToSrc(PoType.Single, (float)(-6.75), [0x2D, 0x36, 0x2E, 0x37, 0x35]);
    }

    [TestMethod]
    public void TestTrySrcToTxt_Double()
    {
        SrcToTextToSrc(PoType.Double, double.MinValue, [0x2D, 0x31, 0x2e, 0x37, 0x39,
            0x37, 0x36, 0x39, 0x33, 0x31, 0x33, 0x34, 0x38, 0x36, 0x32, 0x33, 0x31, 0x35, 0x37, 0x45, 0x2B, 0x33, 0x30, 0x38]);
        SrcToTextToSrc(PoType.Double, double.MaxValue, [0x31, 0x2e, 0x37, 0x39,
            0x37, 0x36, 0x39, 0x33, 0x31, 0x33, 0x34, 0x38, 0x36, 0x32, 0x33, 0x31, 0x35, 0x37, 0x45, 0x2B, 0x33, 0x30, 0x38]);
        SrcToTextToSrc(PoType.Double, (double)6.75, [0x36, 0x2E, 0x37, 0x35]);
        SrcToTextToSrc(PoType.Double, (double)(-6.75), [0x2D, 0x36, 0x2E, 0x37, 0x35]);
    }


    public void TrySrcToTxt_String(PoType type, object value, Span<byte> expected)
    {
        Span<byte> dst = new byte[20];

        var actual = Primitives.TrySrcToTxt(PoType.String, value, dst, out int w);
        Assert.AreEqual(EdfErr.IsOk, actual);
        Assert.AreEqual(expected.Length, w);
        Assert.IsTrue(dst.Slice(0, w).SequenceEqual(expected));

        actual = Primitives.TryTxtToSrc(PoType.String, dst, out object obj);
        Assert.AreEqual(EdfErr.IsOk, actual);
        Assert.AreEqual(value, obj);
    }

    [TestMethod]
    public void TestTrySrcToTxt_String_Errr()
    {
        Span<byte> dst = new byte[20];

        var actual = Primitives.TrySrcToTxt(PoType.String, "AAAAAAAAAAAAAAAAA SSSSSSSSSSSSSSS", dst, out int w);

        Assert.AreEqual(EdfErr.DstBufOverflow, actual);

    }
    [TestMethod]
    public void TestTrySrcToTxt_String()
    {
        TrySrcToTxt_String(PoType.String, "AAAA S", [0x22, 0x41, 0x41, 0x41, 0x41, 0x20, 0x53, 0x22]);
    }


}
