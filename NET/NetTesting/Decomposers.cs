using EdfNet.Ref;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

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

    public ReadOnlySpan<byte> GetExpected() => MemoryMarshal.Cast<long, byte>(ExpectedResult).Slice(0, 3 * 8);
    public ReadOnlySpan<byte> GetResult() => _bufDst.WrittenSpan.Slice(0, 3 * 8);


    [TestMethod]
    public void StdReflection_GetValue()
    {
        _bufDst.Clear();
        StdReflection_GetValue(DefaultVal, _bufDst);
        Assert.IsTrue(GetExpected().SequenceEqual(GetResult()));
    }
    public void StdReflection_GetValue(KeyVal item, ArrayBufferWriter<byte> dst)
    {
        int i = 0;
        _refl.Reset(_edfType, item);
        while (i < _lst.Count && _refl.MoveNext(_lst[i++]))
        {
            var len = _refl.Write(dst.GetSpan());
            dst.Advance(len);
        }
    }
    // YeldDecomposer
    [TestMethod]
    public void YeldDecomposer_GetValue()
    {
        _bufDst.Clear();
        YeldDecomposer_GetValue(DefaultVal, _bufDst);
        Assert.IsTrue(GetExpected().SequenceEqual(GetResult()));
    }
    public void YeldDecomposer_GetValue(KeyVal item, ArrayBufferWriter<byte> dst)
    {
        int i = 0;
        _yeldDecomposer.Reset(_edfType, item);
        while (i < _lst.Count && _yeldDecomposer.MoveNext(_lst[i++]))
        {
            var len = _yeldDecomposer.Write(dst.GetSpan());
            dst.Advance(len);
        }
    }
    // StackDecomposer
    [TestMethod]
    public void StackDecomposer_GetValue()
    {
        StackDecomposer_GetValue(DefaultVal, _bufDst);
        Assert.IsTrue(GetExpected().SequenceEqual(GetResult()));
    }
    public void StackDecomposer_GetValue(KeyVal item, ArrayBufferWriter<byte> dst)
    {
        int i = 0;
        _stackDecomposer.Reset(_edfType, item);
        while (i < _lst.Count && _stackDecomposer.MoveNext(_lst[i++]))
        {
            var len = _stackDecomposer.Write(dst.GetSpan());
            dst.Advance(len);
        }
    }

    [TestMethod]
    public void Generator_GetValue()
    {
        Generator_GetValue(DefaultVal, _bufDst);
        Assert.IsTrue(GetExpected().SequenceEqual(GetResult()));
    }
    public void Generator_GetValue(KeyVal item, ArrayBufferWriter<byte> dst)
    {
        int i = 0;
        var enm = item.GetByteEnumerator();
        while (i < _lst.Count && enm.MoveNext(_lst[i++]))
        {
            var len = enm.Write(dst.GetSpan());
            dst.Advance(len);
        }
    }

}
