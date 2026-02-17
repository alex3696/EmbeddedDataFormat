using NetEdf;
using NetEdf.src;
using System.Text;
namespace NetEdfTest;


[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct MyPos
{
    public UInt32 X;
    public UInt32 Y;
    public UInt32 Z;
}


[EdfBinSerializable]
public partial struct SubVal
{
    public SubVal()
    {
    }
    public double ValDouble { get; set; } = 0x11;
    public byte ValByte { get; set; } = 0x22;
    public sbyte ValSByte { get; set; } = 0x33;
}
[EdfBinSerializable]
public partial class KeyVal
{
    public string Test { get; set; }
    public int Key { get; set; }
    public int Val { get; set; }
    public SubVal subVal { get; set; }
}

[TestClass]
public class TestStructSerialize
{
    static string _testPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
    static string GetTestFilePath(string filename) => Path.Combine(_testPath, filename);

    static byte[] GetCString(string str, int len)
    {
        var ret = new byte[len];
        Encoding.UTF8.GetBytes(str, ret.AsSpan());
        return ret;
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
    }
    [TestMethod]
    public void TestPackUnpack()
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
    }



    struct KeyValue
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
    };
    static int WriteSample(BaseWriter dw)
    {
        TypeRec keyValueType = new()
        {
            Id = 0,
            Name = "VariableKV",
            Desc = "comment",
            Inf = new()
            {
                Type = PoType.Struct,
                Name = "KeyValue",
                Childs =
                [
                    new (PoType.String, "Key"),
                    new (PoType.String, "Value"),
                ]
            }
        };
        dw.Write(keyValueType);
        Assert.AreEqual(EdfErr.IsOk, dw.Write(new KeyValue() { Key = "Key1", Value = "Value1" }));
        Assert.AreEqual(EdfErr.IsOk, dw.Write(new KeyValue() { Key = "Key2", Value = "Value2" }));
        Assert.AreEqual(EdfErr.IsOk, dw.Write(new KeyValue() { Key = "Key3", Value = "Value3" }));

        Assert.AreEqual(EdfErr.IsOk, dw.WriteInfData(0, PoType.String, "тестовый ключ", "String Value"));

        TypeRec t = new() { Inf = new(PoType.Int32), Id = 0, Name = "weight variable" };
        dw.Write(t);
        Assert.AreEqual(EdfErr.IsOk, dw.Write(unchecked((int)0xFFFFFFFF)));

        TypeRec td = new() { Inf = new(PoType.Double), Id = 0, Name = "TestDouble" };
        dw.Write(td);
        Assert.AreEqual(EdfErr.IsOk, dw.Write(1.1d));
        Assert.AreEqual(EdfErr.IsOk, dw.Write(2.1d));
        Assert.AreEqual(EdfErr.IsOk, dw.Write(3.1d));

        TypeRec tchar = new() { Inf = new(PoType.Char, string.Empty, [20]), Id = 0, Name = "Char Text" };
        dw.Write(tchar);
        Assert.AreEqual(EdfErr.IsOk, dw.Write(GetCString("Char", 20)));
        Assert.AreEqual(EdfErr.IsOk, dw.Write(GetCString("Value", 20)));
        Assert.AreEqual(EdfErr.IsOk, dw.Write(GetCString("Array     Value", 20)));

        TypeInf comlexVarInf = new()
        {
            Type = PoType.Struct,
            Name = "ComplexVariable",
            Childs =
            [
                new (PoType.Int64, "time"),
                new ()
                {
                    Type = PoType.Struct, Name = "State", Dims = [3],
                    Childs =
                    [
                        new (PoType.Int8, "text"),
                        new(PoType.Struct,"Pos")
                        {
                            Childs =
                            [
                                new (PoType.Int32, "x"),
                                new (PoType.Int32, "y"),
                            ]
                        },
                        new (PoType.Double, "Temp", [2,2]),
                    ]
                }
            ]
        };
        dw.Write(new TypeRec() { Inf = comlexVarInf });


        return 0;
    }
    [TestMethod]
    public void WriteSample()
    {
        string binFile = GetTestFilePath("t_write.bdf");
        string txtFile = GetTestFilePath("t_write.tdf");
        string txtConvFile = GetTestFilePath("t_writeConv.tdf");

        // BIN write
        using (var file = new FileStream(binFile, FileMode.Create))
        using (var w = new BinWriter(file))
        {
            WriteSample(w);
        }
        // BIN append
        //using (var file = new FileStream(binFile, FileMode.Append))
        //using (var edf = new BinWriter(file))
        //{
        //    edf.WriteInfData(0, PoType.Int32, "Int32 Key", unchecked((int)0xb1b2b3b4));
        //}

        // TXT write
        using (var file = new FileStream(txtFile, FileMode.Create))
        using (var w = new TxtWriter(file))
        {
            WriteSample(w);
        }
        // TXT append
        //using (var file = new FileStream(txtFile, FileMode.Append))
        //using (var edf = new BinWriter(file))
        //{
        //    edf.WriteInfData(0, PoType.Int32, "Int32 Key", unchecked((int)0xb1b2b3b4));
        //}

        using (var binToText = new BinToTxtConverter(binFile, txtConvFile))
            binToText.Execute();

        bool isEqual = FileUtils.FileCompare(txtFile, txtConvFile);
        Assert.IsTrue(isEqual);
    }


    static int WriteBigVar(BaseWriter dw)
    {
        int arrLen = (int)(dw.Cfg.Blocksize / sizeof(uint) * 2.5);
        TypeRec rec = new()
        {
            Inf = new() { Type = PoType.Int32, Name = "variable", Dims = [(uint)arrLen], },
            Id = 0xF0F1F2F3
        };
        dw.Write(rec);
        int[] test = new int[arrLen];
        for (uint i = 0; i < arrLen; i++)
            test[i] = (int)i;
        Assert.AreEqual(EdfErr.IsOk, (EdfErr)dw.Write(test));//write all
        Assert.AreEqual(EdfErr.SrcDataRequred, (EdfErr)dw.Write(test.AsSpan(0, 15).ToArray()));
        Assert.AreEqual(EdfErr.SrcDataRequred, (EdfErr)dw.Write(test.AsSpan(15, arrLen - 30).ToArray()));
        Assert.AreEqual(EdfErr.IsOk, (EdfErr)dw.Write(test.AsSpan(arrLen - 15).ToArray()));
        dw.Flush();
        return 0;
    }
    [TestMethod]
    public void WriteBigVar()
    {
        string binFile = GetTestFilePath("t_big.bdf");
        string txtFile = GetTestFilePath("t_big.tdf");
        string txtConvFile = GetTestFilePath("t_bigConv.tdf");
        // BIN write
        using (var file = new FileStream(binFile, FileMode.Create))
        using (var w = new BinWriter(file))//dw.Write(Header.Default);
        {
            WriteBigVar(w);
        }
        // TXT write
        using (var file = new FileStream(txtFile, FileMode.Create))
        using (var w = new TxtWriter(file))
        {
            WriteBigVar(w);
        }
        using (var binToText = new BinToTxtConverter(binFile, txtConvFile))
            binToText.Execute();

        bool isEqual = FileUtils.FileCompare(txtFile, txtConvFile);
        Assert.IsTrue(isEqual);
    }






    [TestMethod]
    public void TestPrimitiveDecomposer()
    {
        int val0 = 123;
        var flaten0 = new PrimitiveDecomposer(val0).ToArray();
        Assert.AreEqual(123, flaten0[0]);

        var data = new
        {
            Id = 1,
            Meta = new { Code = "A1", Active = true },
            Tags = new[] { "tag1", "tag2" }
        };
        var flaten2 = new PrimitiveDecomposer(data).ToArray();
        Assert.AreEqual(1, flaten2[0]);
        Assert.AreEqual("A1", flaten2[1]);
        Assert.IsTrue((bool?)flaten2[2]);
        Assert.AreEqual("tag1", flaten2[3]);
        Assert.AreEqual("tag2", flaten2[4]);
    }








    [TestMethod]
    public void TestTypeInfEquality()
    {
        TypeInf inf1 = new()
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
        };
        TypeInf inf2 = new()
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
        };
        TypeInf inf3 = new()
        {
            Type = PoType.Struct,
            Name = "KeyValue",
            Dims = [2],
            Childs =
            [
                new (PoType.String, "Key2"),
                new (PoType.String, "Value"),
                new (PoType.UInt8, "Test", [3]),
                new (PoType.String, "Key3"),
            ]
        };
        TypeInf? nullInf = default; // null
        Assert.AreEqual(nullInf, nullInf);
        Assert.AreNotEqual<TypeInf?>(inf3, nullInf);
        Assert.IsFalse(inf3.Equals(nullInf));
        Assert.AreEqual(inf1, inf2);
        Assert.AreNotEqual(inf1, inf3);
    }


    [TestMethod]
    public void TestSourceGenSerialize()
    {
        KeyVal kvs = new() { Key = 0xFABC, Val = 0x1234, Test = "123", subVal = new SubVal() };
        Span<byte> sa = stackalloc byte[1024];
        kvs.SerializeBin(sa);
        int bc = KeyVal.DeserializeBin(sa, out var okv);


    }




}

