using EdfNet.Ref;

namespace NetTest;

public class MyPosition
{
    public byte X { get; set; }
    public byte Y { get; set; }
}

[TestClass]
public class DecomposerZeroAllocTests
{
    [TestMethod]
    public void TestDecomposerZeroAlloc()
    {
        var dcs = new AotPrimitiveDecomposer();
        var buf = new MyArrayBufferWriter(500);
        dcs.Decompose(new MyPosition() { X = 1, Y = 2 }, buf);
        dcs.Decompose(new MyPosition() { X = 3, Y = 4 }, buf);
        dcs.Decompose(new MyPosition() { X = 5, Y = 6 }, buf);
        Assert.IsTrue(buf.WrittenSpan.Slice(0, 6).SequenceEqual(new byte[6] { 1, 2, 3, 4, 5, 6 }));
    }
}
