using NetEdf.Base;
using NetEdf.src;
using NetEdf.StoreTypes;
using System.Text;
using NetEdf;
namespace NetEdfTest;


[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
public struct MyPos
{
    public UInt32 X;
    public UInt32 Y;
    public UInt32 Z;
}


[BinarySerializable]
public partial class KeyVal
{
    public string Test { get; set; }
    public int Key { get; set; }
    public int Val { get; set; }

}

[TestClass]
public class TestStructSerialize
{
    static string _testPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
    static string GetTestFilePath(string filename) => Path.Combine(_testPath, filename);




    public partial class KeyValueStruct
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public byte[] Arr { get; set; }
    }
    [TestMethod]
    public void TestPackUnpack()
    {
        KeyVal kvs = new() { Key = 0xFABC, Val = 0x1234, Test="123" };
        byte [] bin = kvs.Serialize();
        //KeyVal.

        TypeRec TestStructInf = new()
        {
            Inf = new()
            {
                Type = PoType.Struct,
                Name = "KeyValue",
                Dims = [2],
                Items =
                [
                    new (PoType.String, "Key"),
                    new (PoType.String, "Value"),
                    new()
                    {
                        Type = PoType.Struct, Name = "Internal",
                        Items =
                        [
                            new ("Test", PoType.UInt8, [3]),
                        ]
                    }
                ]
            }
        };
        byte[] binBuf = new byte[1024];
        var memStream = new MemoryStream(binBuf);
        using var bw = new BinWriter(memStream, Header.Default, Primitives.BinToBin);
        bw.WriteInfo(TestStructInf);

        KeyValueStruct val1 = new() { Key = "Key1", Value = "Value1", Arr = [11, 22, 33] };
        KeyValueStruct val2 = new() { Key = "Key2", Value = "Value2", Arr = [11, 22, 33] };

        bw.WriteBin(TestStructInf.Inf, val1);
        bw.WriteBin(TestStructInf.Inf, val2);
        memStream.Close();

        var mssrc = new MemoryStream(binBuf);
        byte[] buf = new byte[1024];
        var mem = new MemoryStream(buf);

        var reader = new BinReader(mssrc);

        if (!reader.ReadBlock())
            Assert.Fail("there are no block");
        var header = reader.ReadHeader();
        if (!reader.ReadBlock())
            Assert.Fail("there are no block");
        var rec = reader.ReadInfo();
        Assert.IsNotNull(rec);
        if (!reader.ReadBlock())
            Assert.Fail("there are no block");

        reader.TryRead(rec.Inf, out KeyValueStruct? data);

    }



    Var _intVar = new
    (
        inf: new TypeInfo()
        {
            Name = "weight variable",
            Type = PoType.Int32,
        },
        values: [BitConverter.GetBytes(0xFFFFFFFF)]
    );
    Var _charVar = new
    (
        inf: new TypeInfo()
        {
            Name = "CharArrayVariable",
            Type = PoType.String,
        },
        values:
        [
            BString.GetBytes("Char"),
            BString.GetBytes("Value"),
            BString.GetBytes("Array     Value"),
        ]
    );

    Var _complexVar = new
    (
        inf: new TypeInfo()
        {
            Name = "ComplexVariable",
            Type = PoType.Struct,
            Dims = [],
            Items =
            [
                new TypeInfo("time", PoType.Int64),
                new TypeInfo
                (
                    "State",
                    [3],
                    [
                        new TypeInfo("text", PoType.Int8),
                        new TypeInfo
                        (
                            "Pos",
                            [],
                            [
                                new TypeInfo("x", PoType.Int32),
                                new TypeInfo("y", PoType.Int32),
                            ]
                        ),
                        new TypeInfo("Temp", PoType.Double, [2, 2] )
                    ]
                )
            ]
        },
        values: [FileUtils.GetRandom(208)]
        //values: [new byte[208]]
    );


    Var _bigVar = new
    (
        inf: new TypeInfo()
        {
            Name = "BigVarName",
            Type = PoType.Struct,
            Dims = [],
            Items =
            [
                new TypeInfo("seq", PoType.UInt8),
                new TypeInfo("time", PoType.Int64),
                new TypeInfo("echo", PoType.Double, [20]),
                new TypeInfo("err", PoType.UInt8),
            ]
        }
    );

    TypeInfo _lasBlockType = new()
    {
        Name = "~WELL INFORMATION",
        Type = PoType.Struct,
        Dims = [],
        Items =
        [
            new TypeInfo("MNEM", PoType.String),
            new TypeInfo("UNIT", PoType.String),
            new TypeInfo("DATA", PoType.String),
            new TypeInfo("INFORMATION", PoType.String),
        ]
    };

    [TestMethod(DisplayName = "Text Write Read")]
    public void Test_TextReadWrite()
    {
        var path = GetTestFilePath("test.txt");
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var tw = new TxtWriter(stream, Header.Default, Primitives.BinToStr);

        tw.Write(_intVar);
        if (null != _intVar.Info)
        {
            tw.WriteVarData(BitConverter.GetBytes((int)1));
            tw.WriteVarData([04, 00, 00, 00, 05, 00, 00, 00, 06, 00]);
            tw.WriteVarData([00, 00]);
            tw.WriteVarData([13, 14]);
        }
        tw.Write(_charVar);
        tw.Write(_complexVar);
    }

    [TestMethod(DisplayName = "Bin Write Read")]
    public void Test_StructWriteRead()
    {
        var path = GetTestFilePath("test.bdf");
        {
            // write
            using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
            using var bw = new BinWriter(stream, Header.Default, Primitives.BinToBin);
            bw.Write(_intVar);
            bw.Write(_charVar);
            bw.Write(_complexVar);
        }
        // read
        using var rstream = new FileStream(path, FileMode.Open, FileAccess.Read);
        using var br = new BinReader(rstream);

        var hRestored = br.Cfg;
        bool b2 = br.TryReadVar(out var rintVar);
        bool b3 = br.TryReadVar(out var rCharVar);
        bool b4 = br.TryReadVar(out var rcomplexVar);

        if (b2 && b3 && b4)
        {
            Assert.AreEqual(Header.Default, hRestored);
            Assert.AreEqual(_intVar.Info, rintVar?.Info);
            Assert.AreEqual(_charVar.Info, rCharVar?.Info);
            Assert.AreEqual(_complexVar.Info, rcomplexVar?.Info);
            Assert.IsTrue(Var.IsEqual(_intVar.Values, rintVar?.Values));
            Assert.IsTrue(Var.IsEqual(_charVar.Values, rCharVar?.Values));
            return;
        }
        Assert.Fail();
    }


    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode, Pack = 1)]
    public struct LasBlock
    {
        [MarshalAs(UnmanagedType.BStr)]
        public string Mnem;
        [MarshalAs(UnmanagedType.BStr)]
        public string Unit;
        [MarshalAs(UnmanagedType.BStr)]
        public string Data;
        [MarshalAs(UnmanagedType.BStr)]
        public string Desc;
    }

    public struct LasBlock1
    {
        public string Mnem;
        public UInt32 dd;
    }



    [TestMethod(DisplayName = "LAS test")]
    public void Test_BinLas()
    {
        var eb = Encoding.Unicode.GetBytes("AAA");
        var ssb = StructSerialize.ToBytes(new LasBlock1() { Mnem = "AAA" });

        var lasBlock = new Var(_lasBlockType, values: []);
        lasBlock.AddValue(new LasBlock() { Mnem = "STRT", Unit = ".na", Data = "0", Desc = "START INDEX" });
        lasBlock.AddValue(new LasBlock() { Mnem = "STOP", Unit = ".na", Data = "0", Desc = "STOP INDEX" });
        lasBlock.AddValue(new LasBlock() { Mnem = "STEP", Unit = ".na", Data = "1", Desc = "STEP" });
        lasBlock.AddValue(new LasBlock() { Mnem = "PERIOD", Unit = ".ms", Data = "1000", Desc = "REQUEST PERIOD" });
        lasBlock.AddValue(new LasBlock() { Mnem = "DATE", Unit = "", Data = "2025/05/15", Desc = "LOG START DATE" });

        var comment = Var.Make("Comment", "Test Comment");

        var pos = new Var(inf: new TypeInfo("Position", PoType.Int32, [3]));
        pos.AddValue((new int[3] { 1, 2, 3 }).GetBytes());
        pos.AddValue((new int[3] { 4, 5, 6 }).GetBytes());

        var pos2 = new Var
        (
            inf: new TypeInfo("PositionXYZ", PoType.Struct, [],
            [
                new TypeInfo("x", PoType.Int32),
                new TypeInfo("y", PoType.Int32),
                new TypeInfo("z", PoType.Int32),
            ])
        );
        pos2.AddValue((new int[3] { 11, 12, 13 }).GetBytes());
        pos2.AddValue((new int[3] { 14, 15, 16 }).GetBytes());


        var path = GetTestFilePath("binLas.bdf");
        using var stream = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinWriter(stream, Header.Default, Primitives.BinToBin);

        var txtpath = GetTestFilePath("binLas.txt");
        using var tstream = new FileStream(txtpath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
        using var tw = new TxtWriter(tstream, Header.Default, Primitives.BinToStr);

        void WriteLas(BaseBlockWriter w)
        {
            w.Write(lasBlock);
            w.Flush();
            w.WriteVarData(new LasBlock() { Mnem = "FLD", Unit = ".", Data = "Field", Desc = "FIELD" });
            w.WriteVarData(new LasBlock() { Mnem = "CLU", Unit = ".", Data = "Cluster", Desc = "CLUSTER" });
            w.WriteVarData(new LasBlock() { Mnem = "WELL", Unit = ".", Data = "Well", Desc = "WELL" });

            w.Write(comment);
            w.Write(pos);
            w.WriteVarData(new MyPos() { X = 31, Y = 32, Z = 33 });
            w.Write(pos2);
            w.WriteVarData(new MyPos() { X = 31, Y = 32, Z = 33 });

            w.Write(_bigVar);
            w.Flush();
            List<byte> bb = [];
            bb.Add(11);
            bb.AddRange(BitConverter.GetBytes((Int64)12));
            bb.AddRange(BitConverter.GetBytes((Double)1.3));
            w.WriteVarData(bb.ToArray());
            w.WriteVarData(new byte[170]);
            w.WriteVarData(new byte[170]);
            w.Flush();

        }
        WriteLas(bw);
        WriteLas(tw);
    }

    [TestMethod(DisplayName = "Block copy")]
    public void Test_BlockCopy()
    {
        {
            var pathB = GetTestFilePath("BlockCopySrc.bdf");
            using var srcStream = new FileStream(pathB, FileMode.Create, FileAccess.ReadWrite);
            using var bw = new BinWriter(srcStream, Header.Default, Primitives.BinToBin);

            var pathT = GetTestFilePath("BlockCopySrc.tdf");
            using var srcStreamT = new FileStream(pathT, FileMode.Create, FileAccess.Write);
            using var tw = new TxtWriter(srcStreamT, Header.Default, Primitives.BinToStr);

            tw.Write(_intVar);
            tw.Write(_charVar);
            tw.Write(_complexVar);

            bw.Write(_intVar);
            bw.Write(_charVar);
            bw.Write(_complexVar);
            bw.Flush();
        }

        {
            var pathB = GetTestFilePath("BlockCopySrc.bdf");
            using var srcStream = new FileStream(pathB, FileMode.Open, FileAccess.Read);
            using var srcReader = new BinReader(srcStream, null, Primitives.BinToBin);

            var dstPathB = GetTestFilePath("BlockCopyDst.bdf");
            using var dstStreamB = new FileStream(dstPathB, FileMode.Create, FileAccess.Write);
            using var dstWriterB = new BinWriter(dstStreamB, srcReader.Cfg, Primitives.BinToBin);

            var dstPathT = GetTestFilePath("BlockCopyDst.tdf");
            using var dstStreamT = new FileStream(dstPathT, FileMode.Create, FileAccess.Write);
            using var dstWriterT = new TxtWriter(dstStreamT, srcReader.Cfg, Primitives.BinToStr);

            while (srcReader.TryGet(out BinBlock? bb))
            {
                switch (bb.Type)
                {
                    case BlockType.Header:
                        dstWriterB.Write(Header.Parse(bb.Data));
                        dstWriterT.Write(Header.Parse(bb.Data));
                        break;
                    case BlockType.VarInfo:
                        dstWriterB.WriteVarInfo(TypeInfo.Parse(bb.Data));
                        dstWriterT.WriteVarInfo(TypeInfo.Parse(bb.Data));
                        break;
                    case BlockType.VarData:
                        dstWriterB.WriteVarData(bb.Data);
                        dstWriterT.WriteVarData(bb.Data);
                        break;
                    default: break;
                }
                srcReader.Clear();
            }
        }

        Assert.IsTrue(FileUtils.FileCompare(GetTestFilePath("BlockCopySrc.bdf"), GetTestFilePath("BlockCopyDst.bdf")));
        Assert.IsTrue(FileUtils.FileCompare(GetTestFilePath("BlockCopySrc.tdf"), GetTestFilePath("BlockCopyDst.tdf")));
    }

    [TestMethod(DisplayName = "Zero block write")]
    public void Test_ZeroBlock()
    {
        using var srcStream = new FileStream(GetTestFilePath("ZeroBlock.bdf"), FileMode.Create, FileAccess.ReadWrite);
        using var srcBw = new BinWriter(srcStream, Header.Default, Primitives.BinToBin);
        srcBw.Write(_intVar);
        srcBw.Flush();
        srcBw.WriteVarData((int)0);
        srcBw.WriteVarData((int)0);
        srcBw.WriteVarData((int)0);
        srcBw.WriteVarData((int)0);
        srcBw.WriteVarData((int)0);
    }


    TypeInfo _keyStrValType = new()
    {
        Name = "KeyStrVal",
        Type = PoType.Struct,
        Dims = [],
        Items =
        [
            new TypeInfo("Key", PoType.String),
            new TypeInfo("Value", PoType.String),
        ]
    };
    public struct KeyStrVal
    {
        public string Key;
        public string? Value;
    }

    TypeInfo _keyValUnitType = new()
    {
        Name = "KeyVal",
        Type = PoType.Struct,
        Dims = [],
        Items =
        [
            new TypeInfo("Key", PoType.String),
            new TypeInfo("Value", PoType.String),
            new TypeInfo("Unit", PoType.String),
        ]
    };
    public struct KeyValUnit
    {
        public string Key;
        public string? Value;
        public string? Unit;
    }



}

