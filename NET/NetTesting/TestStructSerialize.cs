using EdfNet.Converters;
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
[EdfSerializable]
public class KeyValue
{
    public string? Key { get; set; }
    public string? Value { get; set; }
};

[EdfSerializable]
public class ComplexVariable
{
    public long Time { get; set; }
    [EdfSerializable]
    public class StateT
    {
        public sbyte Text { get; set; }
        [EdfSerializable]
        public class PosT
        {
            public int X { get; set; }
            public int Y { get; set; }
        };
        public PosT? Pos { get; set; }

        [EdfArray(2, 2)]
        public double[,]? Temp { get; set; }
    };
    [EdfArray(3)]
    public StateT[]? State { get; set; }
};


[TestClass]
public class TestStructSerialize
{
    public static string _testPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
    public static string GetTestFilePath(string filename) => Path.Combine(_testPath, filename);

    static byte[] GetCString(string str, int len)
    {
        var ret = new byte[len];
        Encoding.UTF8.GetBytes(str, ret.AsSpan());
        return ret;
    }


    [TestMethod]
    public void TestPackUnpack()
    {
        EdfSchema TestStructInf = new()
        {
            Type = new()
            {
                Type = EdfPrimitiveType.Struct,
                Name = "KeyValue",
                Dims = [2],
                Childs =
                [
                    new (EdfPrimitiveType.String, "Key"),
                    new (EdfPrimitiveType.String, "Value"),
                    new (EdfPrimitiveType.UInt8, "Test", [3]),
                ]
            }
        };
        KeyValueStruct val1 = new() { Key = "Key1", Value = "Value1", Arr = [11, 12, 13] };
        KeyValueStruct val2 = new() { Key = "Key2", Value = "Value2", Arr = [21, 22, 23] };
        KeyValueStruct[] kvArr = [val1, val2];

        byte[] binBuf = new byte[1024];
        using (var memStream = new MemoryStream(binBuf))
        using (var bw = new EdfBinaryWriter(memStream))
        {
            //CompositeResolver.Instance.TryRegister([new GeneratedEdfResolver()]);
            bw.WriteSchema(TestStructInf);
            bw.WriteValue(val1);
            bw.WriteValue(val2);
            Assert.AreEqual(30, bw.CurrentDataLen);
        }
        var mssrc = new MemoryStream(binBuf);
        var reader = new EdfBinaryReader(mssrc);

        //if (!reader.ReadBlock())
        //    Assert.Fail("there are no block");
        //var header = reader.ReadHeader();
        if (!reader.ReadBlock())
            Assert.Fail("there are no block");
        var rec = reader.ReadSchema();
        Assert.IsNotNull(rec);
        if (!reader.ReadBlock())
            Assert.Fail("there are no block");

        var r1 = reader.ReadValue<KeyValueStruct>();
        Assert.AreEqual(val1, r1);

        var r2 = reader.ReadValue<KeyValueStruct>();
        Assert.AreEqual(val2, r2);
    }


    static int WriteSample(IWriter dw)
    {
        EdfSchema keyValueType = new()
        {
            Id = 0,
            Name = "VariableKV",
            Desc = "comment",
            Type = new()
            {
                Type = EdfPrimitiveType.Struct,
                Name = "KeyValue",
                Childs =
                [
                    new (EdfPrimitiveType.String, "Key"),
                    new (EdfPrimitiveType.String, "Value"),
                ]
            }
        };
        dw.WriteSchema(keyValueType);
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteValue(new KeyValue() { Key = "Key1", Value = "Value1" }));
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteValue(new KeyValue() { Key = "Key2", Value = "Value2" }));
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteValue(new KeyValue() { Key = "Key3", Value = "Value3" }));

        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteInfData(0, EdfPrimitiveType.String, "тестовый ключ 1", "Value 1"));
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteInfData(0, EdfPrimitiveType.String, "тестовый ключ 2", "Value 2"));
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteInfData(0, EdfPrimitiveType.String, "тестовый ключ 3", "Value 3"));
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteInfData(0, EdfPrimitiveType.String, "test NULL string", string.Empty));

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
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteInfData(0, EdfPrimitiveType.String, "test 260 string", sb.ToString()));

        EdfSchema t = new() { Type = new(EdfPrimitiveType.Int32), Id = 0, Name = "weight variable" };
        dw.WriteSchema(t);
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteValue(unchecked((int)0xFFFFFFFF)));

        EdfSchema td = new() { Type = new(EdfPrimitiveType.Double), Id = 0, Name = "TestDouble" };
        dw.WriteSchema(td);
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteValue(1.1d));
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteValue(2.1d));
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteValue(3.1d));

        EdfSchema tchar = new() { Type = new(EdfPrimitiveType.Char, string.Empty, [20]), Id = 0, Name = "Char Text" };
        dw.WriteSchema(tchar);
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteValue(GetCString("Char", 20)));
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteValue(GetCString("Value", 20)));
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteValue(GetCString("Array     Value", 20)));

        EdfSchema schComlexChar = new()
        {
            Type = new()
            {
                Type = EdfPrimitiveType.Struct,
                Name = "Chat10Test",
                Childs =
                [
                    new (EdfPrimitiveType.UInt8),
                    new (EdfPrimitiveType.Char, name: null, dims:[10]),
                    new (EdfPrimitiveType.UInt16),
                ]
            }
        };
        dw.WriteSchema(schComlexChar);
        dw.WriteValue((byte)8);
        var schComlexCharBuf = new byte[10];
        Encoding.UTF8.GetBytes("Char", schComlexCharBuf.AsSpan());
        dw.WriteValue(schComlexCharBuf);
        dw.WriteValue((ushort)16);

        EdfType comlexVarInf = new()
        {
            Type = EdfPrimitiveType.Struct,
            Name = "ComplexVariable",
            Childs =
            [
                new (EdfPrimitiveType.Int64, "time"),
                new ()
                {
                    Type = EdfPrimitiveType.Struct, Name = "State", Dims = [3],
                    Childs =
                    [
                        new (EdfPrimitiveType.Int8, "text"),
                        new(EdfPrimitiveType.Struct,"Pos")
                        {
                            Childs =
                            [
                                new (EdfPrimitiveType.Int32, "x"),
                                new (EdfPrimitiveType.Int32, "y"),
                            ]
                        },
                        new (EdfPrimitiveType.Double, "Temp", [2,2]),
                    ]
                }
            ]
        };
        dw.WriteSchema(new EdfSchema() { Type = comlexVarInf });
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
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteValue(cv));
        return 0;
    }
    [TestMethod]
    public void WriteSample()
    {
        string binFile = GetTestFilePath("t_write.bdf");
        string txtFile = GetTestFilePath("t_write.tdf");
        string txtConvFile = GetTestFilePath("t_writeConv.tdf");
        string binConvFile = GetTestFilePath("t_writeConv.bdf");

        // BIN write
        using (var file = new FileStream(binFile, FileMode.Create))
        using (var w = new EdfBinaryWriter(file, new EdfConfig() { BlockSize = 300 }))
        {
            WriteSample(w);
        }
        // BIN append
        using (var file = new FileStream(binFile, FileMode.Open))
        {
            EdfConfig cfg;
            {
                var edf = new EdfBinaryReader(file);
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
            using (var edf = new EdfBinaryWriter(file, cfg))
            {
                edf.WriteInfData(0, EdfPrimitiveType.Int32, "Int32 Key", unchecked((int)0xb1b2b3b4));
            }
        }
        // TXT write
        using (var file = new FileStream(txtFile, FileMode.Create))
        using (var w = new EdfTextWriter(file, new EdfConfig(300)))
        {
            WriteSample(w);
        }
        // TXT append
        using (var file = new FileStream(txtFile, FileMode.Append))
        using (var edf = new EdfTextWriter(file))
        {
            edf.WriteInfData(0, EdfPrimitiveType.Int32, "Int32 Key", unchecked((int)0xb1b2b3b4));
        }
        BinToTxt.Convert(binFile, txtConvFile);
        bool isEqual = FileUtils.FileCompare(txtFile, txtConvFile);
        Assert.IsTrue(isEqual);

        TxtToBin.Convert(txtFile, binConvFile);
        Assert.IsTrue(FileUtils.FileCompare(binFile, binConvFile));
    }

    static int WriteBigVar(IWriter dw)
    {
        int arrLen = 160;
        EdfSchema rec = new()
        {
            Id = 0xF1F2,
            Type = new() { Type = EdfPrimitiveType.Int32, Name = "variable", Dims = [(ushort)arrLen], },
        };
        dw.WriteSchema(rec);
        int[] test = new int[arrLen];
        for (uint i = 0; i < arrLen; i++)
            test[i] = (int)i;
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteValue(test));//write all
        Assert.AreEqual(EdfErrorCode.SrcDataRequred, dw.WriteValue(test.AsSpan(0, 15).ToArray()));
        Assert.AreEqual(EdfErrorCode.SrcDataRequred, dw.WriteValue(test.AsSpan(15, arrLen - 30).ToArray()));
        Assert.AreEqual(EdfErrorCode.IsOk, dw.WriteValue(test.AsSpan(arrLen - 15).ToArray()));
        dw.Flush();
        return 0;
    }
    [TestMethod]
    public void WriteBigVar()
    {
        string binFile = GetTestFilePath("t_big.bdf");
        string txtFile = GetTestFilePath("t_big.tdf");
        string txtConvFile = GetTestFilePath("t_bigConv.tdf");
        string binConvFile = GetTestFilePath("t_bigConv.bdf");
        // BIN write
        using (var file = new FileStream(binFile, FileMode.Create))
        using (var w = new EdfBinaryWriter(file))//dw.Write(Header.Default);
        {
            WriteBigVar(w);
        }
        // TXT write
        using (var file = new FileStream(txtFile, FileMode.Create))
        using (var w = new EdfTextWriter(file))
        {
            WriteBigVar(w);
        }
        BinToTxt.Convert(binFile, txtConvFile);

        bool isEqual = FileUtils.FileCompare(txtFile, txtConvFile);
        Assert.IsTrue(isEqual, "WriteBigVar file does not match ");

        TxtToBin.Convert(txtFile, binConvFile);
        Assert.IsTrue(FileUtils.FileCompare(binFile, binConvFile));
    }

    [TestMethod]
    public void TestTypeInfEquality()
    {
        EdfType inf1 = new()
        {
            Type = EdfPrimitiveType.Struct,
            Name = "KeyValue",
            Dims = [2],
            Childs =
            [
                new (EdfPrimitiveType.String, "Key"),
                new (EdfPrimitiveType.String, "Value"),
                new (EdfPrimitiveType.UInt8, "Test", [3]),
            ]
        };
        EdfType inf2 = new()
        {
            Type = EdfPrimitiveType.Struct,
            Name = "KeyValue",
            Dims = [2],
            Childs =
            [
                new (EdfPrimitiveType.String, "Key"),
                new (EdfPrimitiveType.String, "Value"),
                new (EdfPrimitiveType.UInt8, "Test", [3]),
            ]
        };
        EdfType inf3 = new()
        {
            Type = EdfPrimitiveType.Struct,
            Name = "KeyValue",
            Dims = [2],
            Childs =
            [
                new (EdfPrimitiveType.String, "Key2"),
                new (EdfPrimitiveType.String, "Value"),
                new (EdfPrimitiveType.UInt8, "Test", [3]),
                new (EdfPrimitiveType.String, "Key3"),
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

