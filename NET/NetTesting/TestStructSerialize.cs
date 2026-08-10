using EdfNet.Gen;
using EdfNet.Interfaces;
using EdfNet.Utils;
using System.Runtime.InteropServices;
using System.Text;
namespace NetTest;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct MyPos
{
    public UInt32 X;
    public UInt32 Y;
    public UInt32 Z;
}


[EdfSerializable]
public class KeyValueStruct : IEquatable<KeyValueStruct>
{
    public string? Key { get; set; }
    public string? Value { get; set; }
    [EdfArray(3)]
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

    public override int GetHashCode()
    {
        throw new NotImplementedException();
    }
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


    [TestMethod]
    public void TestPackUnpack()
    {
        Schema TestStructInf = new()
        {
            Type = new()
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
        using (var bw = new WriterBin(memStream))
        {
            bw.Write(TestStructInf);
            bw.WriteValue(val1);
            bw.WriteValue(val2);

            //bw.Write(TestStructInf.Inf, val1);
            //bw.Write(TestStructInf.Inf, val2);
            Assert.AreEqual(30, bw.CurrentDataLen);
        }
        var mssrc = new MemoryStream(binBuf);
        var reader = new BinReader(mssrc);

        //if (!reader.ReadBlock())
        //    Assert.Fail("there are no block");
        //var header = reader.ReadHeader();
        if (!reader.ReadBlock())
            Assert.Fail("there are no block");
        var rec = reader.ReadSchema();
        Assert.IsNotNull(rec);
        if (!reader.ReadBlock())
            Assert.Fail("there are no block");

        var readEnumerator1 = new KeyValueStructByteEnumerator(new());
        reader.ReadData(ref readEnumerator1);
        Assert.AreEqual(val1, readEnumerator1.Result);

        var readEnumerator2 = new KeyValueStructByteEnumerator(new());
        reader.ReadData(ref readEnumerator2);
        Assert.AreEqual(val2, readEnumerator2.Result);
    }

    class KeyValue
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
    };
    class ComplexVariable
    {
        public long Time { get; set; }
        public class StateT
        {
            public sbyte Text { get; set; }
            public class PosT
            {
                public int X { get; set; }
                public int Y { get; set; }
            };
            public PosT Pos { get; set; }
            public double[,] Temp { get; set; }
        };
        public StateT[] State { get; set; }
    };

    static int WriteSample(IWriter dw)
    {
        Schema keyValueType = new()
        {
            Id = 0,
            Name = "VariableKV",
            Desc = "comment",
            Type = new()
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

        Assert.AreEqual(EdfErr.IsOk, dw.WriteInfData(0, PoType.String, "тестовый ключ 1", "Value 1"));
        Assert.AreEqual(EdfErr.IsOk, dw.WriteInfData(0, PoType.String, "тестовый ключ 2", "Value 2"));
        Assert.AreEqual(EdfErr.IsOk, dw.WriteInfData(0, PoType.String, "тестовый ключ 3", "Value 3"));
        Assert.AreEqual(EdfErr.IsOk, dw.WriteInfData(0, PoType.String, "test NULL string", string.Empty));

        const char chBegin = '0'; const char chEnd = '9';
        char ch = chBegin;
        StringBuilder sb = new(260);
        for (int i = 0; i < 260; i++)
        {
            sb.Append(ch);
            ch++;
            if (chEnd < ch)
                ch = chBegin;
        }
        Assert.AreEqual(EdfErr.IsOk, dw.WriteInfData(0, PoType.String, "test 260 string", sb.ToString()));

        Schema t = new() { Type = new(PoType.Int32), Id = 0, Name = "weight variable" };
        dw.Write(t);
        Assert.AreEqual(EdfErr.IsOk, dw.WriteValue(unchecked((int)0xFFFFFFFF)));

        Schema td = new() { Type = new(PoType.Double), Id = 0, Name = "TestDouble" };
        dw.Write(td);
        Assert.AreEqual(EdfErr.IsOk, dw.WriteValue(1.1d));
        Assert.AreEqual(EdfErr.IsOk, dw.WriteValue(2.1d));
        Assert.AreEqual(EdfErr.IsOk, dw.WriteValue(3.1d));

        Schema tchar = new() { Type = new(PoType.Char, string.Empty, [20]), Id = 0, Name = "Char Text" };
        dw.Write(tchar);
        Assert.AreEqual(EdfErr.IsOk, dw.Write(GetCString("Char", 20)));
        Assert.AreEqual(EdfErr.IsOk, dw.Write(GetCString("Value", 20)));
        Assert.AreEqual(EdfErr.IsOk, dw.Write(GetCString("Array     Value", 20)));

        Schema schComlexChar = new()
        {
            Type = new()
            {
                Type = PoType.Struct,
                Name = "Chat10Test",
                Childs =
                [
                    new (PoType.UInt8),
                    new (PoType.Char, name: null, dims:[10]),
                    new (PoType.UInt16),
                ]
            }
        };
        dw.Write(schComlexChar);
        dw.WriteValue((byte)8);
        var schComlexCharBuf = new byte[10];
        Encoding.UTF8.GetBytes("Char", schComlexCharBuf.AsSpan());
        dw.Write(schComlexCharBuf);
        dw.WriteValue((ushort)16);

        EdfType comlexVarInf = new()
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
        dw.Write(new Schema() { Type = comlexVarInf });
        var cv = new ComplexVariable()
        {
            Time = -123,
            State =
            [
                new(){ Text = 1,Pos = new (){X=11,Y=12 },Temp = new double[2,2]{ {1.1,1.2 },{1.3,1.4 } }  },
                new(){ Text = 2,Pos = new (){X=21,Y=22 },Temp = new double[2,2]{ {2.1,2.2 },{2.3,2.4 } }  },
                new(){ Text = 3,Pos = new (){X=31,Y=32 },Temp = new double[2,2]{ {3.1,3.2 },{3.3,3.4 } }  },
            ]
        };
        Assert.AreEqual(EdfErr.IsOk, dw.Write(cv));
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
        using (var w = new WriterBin(file, new Config() { Blocksize = 300 }))
        {
            WriteSample(w);
        }
        // BIN append
        using (var file = new FileStream(binFile, FileMode.Open))
        {
            Config cfg;
            {
                var edf = new BinReader(file);
                cfg = edf.Cfg;
                //try
                //{
                //    while (edf.ReadBlock())
                //    {
                //    }
                //}
                //catch (EndOfStreamException)
                //{
                //}
            }
            file.Seek(0, SeekOrigin.End);
            using (var edf = new WriterBin(file, cfg))
            {
                edf.WriteInfData(0, PoType.Int32, "Int32 Key", unchecked((int)0xb1b2b3b4));
            }
        }
        // TXT write
        using (var file = new FileStream(txtFile, FileMode.Create))
        using (var w = new WriterTxt(file, new Config(300)))
        {
            WriteSample(w);
        }
        // TXT append
        using (var file = new FileStream(txtFile, FileMode.Append))
        using (var edf = new WriterTxt(file))
        {
            edf.WriteInfData(0, PoType.Int32, "Int32 Key", unchecked((int)0xb1b2b3b4));
        }
        using (var binToText = new BinToTxtConverter(binFile, txtConvFile))
            binToText.Execute();
        bool isEqual = FileUtils.FileCompare(txtFile, txtConvFile);
        Assert.IsTrue(isEqual);
    }

    static int WriteBigVar(IWriter dw)
    {
        int arrLen = (int)(dw.Cfg.Blocksize / sizeof(uint) * 2.5);
        Schema rec = new()
        {
            Id = 0xF1F2,
            Type = new() { Type = PoType.Int32, Name = "variable", Dims = [(ushort)arrLen], },
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
        using (var w = new WriterBin(file))//dw.Write(Header.Default);
        {
            WriteBigVar(w);
        }
        // TXT write
        using (var file = new FileStream(txtFile, FileMode.Create))
        using (var w = new WriterTxt(file))
        {
            WriteBigVar(w);
        }
        using (var binToText = new BinToTxtConverter(binFile, txtConvFile))
            binToText.Execute();

        bool isEqual = FileUtils.FileCompare(txtFile, txtConvFile);
        Assert.IsTrue(isEqual);
    }

    [TestMethod]
    public void TestTypeInfEquality()
    {
        EdfType inf1 = new()
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
        EdfType inf2 = new()
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
        EdfType inf3 = new()
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
        EdfType? nullInf = default; // null
        Assert.AreEqual(nullInf, nullInf);
        Assert.AreNotEqual<EdfType?>(inf3, nullInf);
        Assert.IsFalse(inf3.Equals(nullInf));
        Assert.AreEqual(inf1, inf2);
        Assert.AreNotEqual(inf1, inf3);
    }

}

