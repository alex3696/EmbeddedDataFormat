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
    StackDecomposer _stackDecomposer;
    YeldDecomposer _yeldDecomposer;

    public readonly EdfType _edfType = new() { Type = PoType.Int64 };
    public static readonly MyPosition DefaultVal = new() { X = 0x01020304_05060708, Y = 2, Z = 0x05060708_01020304 };
    public static readonly long[] ExpectedResult = [0x01020304_05060708, 2, 0x05060708_01020304];

    public Decomposers()
    {
        _bufDst = new ArrayBufferWriter<byte>(32);
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
    public void StdReflection_GetValue(MyPosition item, ArrayBufferWriter<byte> dst)
    {
        _refl.Reset(_edfType, item);
        while (_refl.MoveNext(_edfType))
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
    public void YeldDecomposer_GetValue(MyPosition item, ArrayBufferWriter<byte> dst)
    {
        _yeldDecomposer.Reset(_edfType, item);
        while (_yeldDecomposer.MoveNext(_edfType))
        {
            var len = _yeldDecomposer.Write(dst.GetSpan());
            dst.Advance(len);
        }
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
        _stackDecomposer.Reset(_edfType, item);
        while (_stackDecomposer.MoveNext(_edfType))
        {
            var len = _stackDecomposer.Write(dst.GetSpan());
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
    /*
    [TestMethod]
    public void TestPrimitiveDecomposer()
    {
        int val0 = 123;
        var flaten0 = new PrimitiveDecomposer().Reset(val0).ToArray();
        Assert.HasCount(1, flaten0, "flaten0.Length not equal 1");
        Assert.AreEqual(123, flaten0[0]);

        var data = new
        {
            Id = 1,
            Meta = new { Code = "A1", Active = true },
            Tags = new[] { "tag1", "tag2" }
        };
        var flaten2 = new PrimitiveDecomposer().Reset(data).ToArray();
        Assert.HasCount(5, flaten2, "flaten2.Length not equal 5");

        Assert.AreEqual(1, flaten2[0]);
        Assert.AreEqual("A1", flaten2[1]);
        Assert.IsTrue((bool?)flaten2[2]);
        Assert.AreEqual("tag1", flaten2[3]);
        Assert.AreEqual("tag2", flaten2[4]);
    }
    */
}
