using NetEdf;

namespace NetEdfTest;

[TestClass]
public class TestEDFBinString
{
    [TestMethod]
    public void TestSizeOf()
    {
        string str = "AAAA SSSS CCCC";

        var actual = EdfBinString.SizeOf(str);

        Assert.AreEqual(0xE, actual);
    }

    [TestMethod]
    public void TestWriteBin()
    {
        string str = "AAAA SSSS CCCC";
        Span<byte> dst = new byte[20];
        Span<byte> expected = [0xE, 0x41, 0x41, 0x41, 0x41, 0x20,
            0x53, 0x53, 0x53, 0x53, 0x20, 0x43, 0x43, 0x43, 0x43];

        var actual = EdfBinString.WriteBin(str, dst);

        Assert.AreEqual(expected.Length, actual);
        Assert.IsTrue(dst.Slice(0, actual).SequenceEqual(expected));
    }

    [TestMethod]
    public void TestReadBin()
    {
        ReadOnlySpan<byte> src = [0xE, 0x41, 0x41, 0x41, 0x41, 0x20,
            0x53, 0x53, 0x53, 0x53, 0x20, 0x43, 0x43, 0x43, 0x43];

        string value = "AAAA SSSS CCCC";

        var actual = EdfBinString.ReadBin(src, out string? str);

        Assert.AreEqual(value, str);


    }
}
