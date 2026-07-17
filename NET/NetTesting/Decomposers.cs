using EdfNet.Interfaces;
using EdfNet.Ref;
using System.Buffers;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace NetTest;

[EdfSerializable]
public class MyPosition
{
    public long X { get; set; }
    public long Y { get; set; }
    public long Z { get; set; }
}

[TestClass]
public class Decomposers
{
    private readonly ArrayBufferWriter<byte> _bufDst;
    PrimitiveDecomposer _refl;
    AotPrimitiveDecomposer _delegate1;
    StackDecomposer _stackDecomposer;

    public readonly EdfType _edfType = new() { Type = PoType.Int64 };
    public static readonly MyPosition DefaultVal = new() { X = 0x01020304_05060708, Y = 2, Z = 0x05060708_01020304 };
    public static readonly long[] ExpectedResult = [0x01020304_05060708, 2, 0x05060708_01020304];

    public Decomposers()
    {
        _bufDst = new ArrayBufferWriter<byte>(32);
        // reflection
        _refl = new PrimitiveDecomposer();
        // delegate 1
        _delegate1 = new AotPrimitiveDecomposer();
        // StackDecomposer
        _stackDecomposer = new();
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
    public void StdReflection_GetValue(MyPosition item, ArrayBufferWriter<byte> dst)
    {
        IEnumerable<object> enmumerable = _refl.Decompose(item);
        foreach (var field in enmumerable)
        {
            var len = PrimitiveWritersBin.TryWrite(dst.GetSpan(), _edfType, field);
            dst.Advance(len);
        }
    }
    // Delegate 1
    //[TestMethod]
    public void Delegate1_GetValue()
    {
        _bufDst.Clear();
        Delegate1_GetValue(DefaultVal, _bufDst);
        Assert.IsTrue(GetExpected().SequenceEqual(GetResult()));
    }
    public void Delegate1_GetValue(MyPosition item, ArrayBufferWriter<byte> dst)
    {
        _delegate1.Decompose(item, dst);
    }
    // StackDecomposer
    [TestMethod]
    public void StackDecomposer_GetValue()
    {
        _bufDst.Clear();
        StackDecomposer_GetValue(DefaultVal, _bufDst);
        Assert.IsTrue(GetExpected().SequenceEqual(GetResult()));
    }
    public void StackDecomposer_GetValue(MyPosition item, ArrayBufferWriter<byte> dst)
    {
        var enm = new StackDecomposer(item);
        while (enm.MoveNext(_edfType))
        {
            var obj = enm.GetValue();
            var len = PrimitiveWritersBin.TryWrite(dst.GetSpan(), _edfType, obj);
            dst.Advance(len);
        }
    }

    [TestMethod]
    public void Generator_GetValue()
    {
        _bufDst.Clear();
        Generator_GetValue(DefaultVal, _bufDst);
        Assert.IsTrue(GetExpected().SequenceEqual(GetResult()));
    }
    public void Generator_GetValue(MyPosition item, ArrayBufferWriter<byte> dst)
    {
        var enm = new MyPositionByteEnumerator(item);
        while (enm.MoveNext(_edfType))
        {
            var len = enm.Write(dst.GetSpan());
            dst.Advance(len);
        }
    }
}
