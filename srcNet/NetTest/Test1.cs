using NetEdf.src;

namespace NetTest;

public class Test1
{
    public static void TestPrimitiveDecomposer()
    {
        int val0 = 123;
        var flaten0 = new PrimitiveDecomposer(val0).ToArray();
        Assert.AreEqual(1, flaten0.Length, "flaten0.Length not equal 1");
        Assert.AreEqual(123, flaten0[0]);

        var data = new
        {
            Id = 1,
            Meta = new { Code = "A1", Active = true },
            Tags = new[] { "tag1", "tag2" }
        };
        var flaten2 = new PrimitiveDecomposer(data).ToArray();
        Assert.AreEqual(5, flaten2.Length, "flaten2.Length not equal 5");

        Assert.AreEqual(1, flaten2[0]);
        Assert.AreEqual("A1", flaten2[1]);
        Assert.IsTrue((bool?)flaten2[2]);
        Assert.AreEqual("tag1", flaten2[3]);
        Assert.AreEqual("tag2", flaten2[4]);
    }




    class KeyValueStruct : IEquatable<KeyValueStruct>
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
        public byte[]? Arr { get; set; }

        public bool Equals(KeyValueStruct? other)
        {
            if (other is null)
                return false;
            if (!string.Equals(Key, other.Key))
                return false;
            if (!string.Equals(Value, other.Value))
                return false;
            if (!Arr.SequenceEqual(other.Arr))
                return false;
            return true;
        }
        public override bool Equals(object? obj)
        {
            return Equals(obj as KeyValueStruct);
        }
    }
    public static int TestPackUnpack()
    {
        TypeRec TestStructInf = new()
        {
            Inf = new()
            {
                Type = PoType.Struct,
                Name = "KeyValue",
                Dims = [2],
                Childs =
                [
                    new (PoType.String, "Key"),
                    new (PoType.String, "Value"),
                    new (PoType.UInt8, "Test", [3]),
                ]
            }
        };
        KeyValueStruct val1 = new() { Key = "Key1", Value = "Value1", Arr = [11, 12, 13] };
        KeyValueStruct val2 = new() { Key = "Key2", Value = "Value2", Arr = [21, 22, 23] };
        KeyValueStruct[] kvArr = [val1, val2];

        byte[] binBuf = new byte[1024];
        using (var memStream = new MemoryStream(binBuf))
        using (var bw = new BinWriter(memStream))
        {
            bw.Write(TestStructInf);
            bw.Write(kvArr);
            //bw.Write(TestStructInf.Inf, val1);
            //bw.Write(TestStructInf.Inf, val2);
            Assert.AreEqual(30, bw.CurrentQty);
        }
        var mssrc = new MemoryStream(binBuf);
        byte[] buf = new byte[1024];
        using var mem = new MemoryStream(buf);
        using var reader = new BinReader(mssrc);

        //if (!reader.ReadBlock())
        //    Assert.Fail("there are no block");
        //var header = reader.ReadHeader();
        if (!reader.ReadBlock())
            Assert.Fail("there are no block");
        var rec = reader.ReadInfo();
        Assert.IsNotNull(rec);
        if (!reader.ReadBlock())
            Assert.Fail("there are no block");

        reader.TryRead(out KeyValueStruct[]? data);

        Assert.AreEqual(kvArr[0], data[0]);
        Assert.AreEqual(kvArr[1], data[1]);

        return 0;
    }
}
