using EdfNet.Ref;
using System.Buffers;
using System.Collections.Generic;

namespace NetTest;

[TestClass]
public class Decomposers
{
    private readonly ArrayBufferWriter<byte> _bufDst;
    PrimitiveDecomposer _refl;
    StackDecomposer _stackDecomposer;
    YeldDecomposer _yeldDecomposer;

    public readonly EdfType _edfType = TestClasses_Content.KeyValSchema.Type;
    public static readonly KeyVal DefaultVal = TestClasses_Content.TestValue;
    List<EdfType> _lst = new(100);


    public static readonly long[] ExpectedResult = [0x01020304_05060708, 2, 0x05060708_01020304];

    public Decomposers()
    {
        EdfType[] stack = new EdfType[256];
        foreach (var field in _edfType.EnumerateStack(stack))
            _lst.Add(field);

        _bufDst = new ArrayBufferWriter<byte>(1024);
        // reflection
        _refl = new PrimitiveDecomposer();
        // StackDecomposer
        _stackDecomposer = new StackDecomposer();
        // YeldDecomposer
        _yeldDecomposer = new YeldDecomposer();
    }

    public ReadOnlySpan<byte> GetExpected() => TestClasses_Content.TestValueBin;


    private void CheckResults()
    {
    }


    [TestMethod]
    public void StdReflection_GetValue()
    {
        _bufDst.Clear();
        int i = 0;
        _refl.Reset(_edfType, DefaultVal);
        while (i < _lst.Count && _refl.MoveNext(_lst[i++]))
        {
            var len = _refl.Write(_bufDst.GetSpan());
            _bufDst.Advance(len);
        }
        bool eq = GetExpected().SequenceEqual(_bufDst.WrittenSpan);
        if (!eq)
            Console.WriteLine($"writed={_bufDst.WrittenCount} elements={i}");
        Assert.IsTrue(eq);
    }
    // YeldDecomposer
    [TestMethod]
    public void YeldDecomposer_GetValue()
    {
        _bufDst.Clear();
        int i = 0;
        _yeldDecomposer.Reset(_edfType, DefaultVal);
        while (i < _lst.Count && _yeldDecomposer.MoveNext(_lst[i++]))
        {
            var len = _yeldDecomposer.Write(_bufDst.GetSpan());
            _bufDst.Advance(len);
        }
        bool eq = GetExpected().SequenceEqual(_bufDst.WrittenSpan);
        if (!eq)
            Console.WriteLine($"writed={_bufDst.WrittenCount} elements={i}");
        Assert.IsTrue(eq);
    }
    // StackDecomposer
    [TestMethod]
    public void StackDecomposer_GetValue()
    {
        _bufDst.Clear();
        int i = 0;
        _stackDecomposer.Reset(_edfType, DefaultVal);
        while (i < _lst.Count && _stackDecomposer.MoveNext(_lst[i++]))
        {
            var len = _stackDecomposer.Write(_bufDst.GetSpan());
            _bufDst.Advance(len);
        }
        bool eq = GetExpected().SequenceEqual(_bufDst.WrittenSpan);
        if (!eq)
            Console.WriteLine($"writed={_bufDst.WrittenCount} elements={i}");
        Assert.IsTrue(eq);
    }
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
