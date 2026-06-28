using NetEdf.src;
using System.Text;

namespace NetEdfTest;

[TestClass]
public class TestModbusCRC
{
    [TestMethod]
    public void TestCalcStream()
    {
        string text = "AAAA S AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
            "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
        UInt16 expectedRes = 0x564D;
        var temp = Encoding.UTF8.GetBytes(text);
        Stream stream = new MemoryStream(temp);

        var actual = ModbusCRC.Calc(stream);

        Assert.AreEqual(expectedRes, actual);

    }

    [TestMethod]
    public void TestCalcFn()
    {
        Span<byte> buf = new byte[2] { 0x01, 0x02 };
        UInt16 expectedRes = 0xE181;

        var actual = ModbusCRC.CalcFn(buf);

        Assert.AreEqual(expectedRes, actual);
    }

    [TestMethod]
    public void TestCalcWithReverseT()
    {
        Span<byte> buf = new byte[2] { 0x01, 0x02 };
        UInt16 expectedRes = 0xE181;

        var actual = ModbusCRC.Calc(buf);

        Assert.AreEqual(expectedRes, actual);
    }

    [TestMethod]
    public void TestCalcWithoutReverseT()
    {
        Span<byte> buf = new byte[2] { 0x01, 0x02 };
        UInt16 expectedRes = 0xE181;

        var actual = ModbusCRC.CalcTR(buf);

        Assert.AreEqual(expectedRes, actual);
    }
}
