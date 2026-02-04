using NetEdf;
using NetEdf.Base;
using NetEdf.src;
using NetEdf.StoreTypes;
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




    public partial class KeyValueStruct
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public byte[] Arr { get; set; }
    }
    [TestMethod]
    public void TestPackUnpack()
    {
        KeyVal kvs = new() { Key = 0xFABC, Val = 0x1234, Test = "123", subVal=new SubVal() };
        Span<byte> sa = stackalloc byte[1024];
        kvs.SerializeBin(sa);
        int bc = KeyVal.DeserializeBin(sa, out var okv);


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
                    new (PoType.UInt8, "Test", [3]),
                ]
            }
        };
        byte[] binBuf = new byte[1024];
        using (var memStream = new MemoryStream(binBuf))
        using (var bw = new BinWriter(memStream))
        {
            bw.WriteInfo(TestStructInf);
            KeyValueStruct val1 = new() { Key = "Key1", Value = "Value1", Arr = [11, 22, 33] };
            KeyValueStruct val2 = new() { Key = "Key2", Value = "Value2", Arr = [11, 22, 33] };
            bw.Write(TestStructInf.Inf, val1);
            bw.Write(TestStructInf.Inf, val2);
            Assert.AreEqual(30, bw.CurrentQty);
        }
        var mssrc = new MemoryStream(binBuf);
        byte[] buf = new byte[1024];
        var mem = new MemoryStream(buf);

        var reader = new BinReader(mssrc);

        //if (!reader.ReadBlock())
        //    Assert.Fail("there are no block");
        //var header = reader.ReadHeader();
        if (!reader.ReadBlock())
            Assert.Fail("there are no block");
        var rec = reader.ReadInfo();
        Assert.IsNotNull(rec);
        if (!reader.ReadBlock())
            Assert.Fail("there are no block");

        reader.TryRead(rec.Inf, out KeyValueStruct? data);

    }




}

