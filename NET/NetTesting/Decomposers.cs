using System.Buffers;
using System.Collections.Generic;

namespace NetTest;

[TestClass]
public class Decomposers
{
    private readonly ArrayBufferWriter<byte> _bufDst;

    public readonly EdfType _edfType = TestClasses_Content.KeyValSchema.Type;
    public static readonly ComplexType DefaultVal = TestClasses_Content.TestValue;
    List<EdfType> _lst = new(100);

    public static readonly long[] ExpectedResult = [0x01020304_05060708, 2, 0x05060708_01020304];

    public Decomposers()
    {
        EdfType[] stack = new EdfType[256];
        foreach (var field in _edfType.EnumerateStack(stack))
            _lst.Add(field);

        _bufDst = new ArrayBufferWriter<byte>(1024);
    }

    public ReadOnlySpan<byte> GetExpected() => TestClasses_Content.TestValueBin;

    [TestMethod]
    public void Generator_GetValue()
    {
        _bufDst.Clear();
        int i = 0;
        var enm = DefaultVal.GetByteEnumerator();
        while (i < _lst.Count && enm.MoveNext(_lst[i++]))
        {
            var len = enm.Write(_bufDst.GetSpan());
            _bufDst.Advance(len);
        }
        bool eq = GetExpected().SequenceEqual(_bufDst.WrittenSpan);
        if (!eq)
            Console.WriteLine($"writed={_bufDst.WrittenCount} elements={i}");
        Assert.IsTrue(eq);
    }
}
