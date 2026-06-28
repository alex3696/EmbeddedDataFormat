using NetEdf.src;
using System.Text;
using static NetEdfTest.TestStructSerialize;

namespace NetEdfTest;

[TestClass]
public class TestCsvWriter
{
    static string _testPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
    static string GetTestFilePath(string filename) => Path.Combine(_testPath, filename);

    static byte[] GetCString(string str, int len)
    {
        var ret = new byte[len];
        Encoding.UTF8.GetBytes(str, ret.AsSpan());
        return ret;
    }


    struct KeyValue
    {
        public string? Key { get; set; }
        public string? Value { get; set; }
    };
    struct ComplexVariable
    {
        public long Time { get; set; }
        public struct StateT
        {
            public sbyte Text { get; set; }
            public struct PosT
            {
                public int x { get; set; }
                public int y { get; set; }
            };
            public PosT Pos { get; set; }
            public double[,] Temp { get; set; }
        };
        public StateT[] State { get; set; }
    };

    [TestMethod]
    public void PackToCsvTest()
    {
        string csvFile = GetTestFilePath("CsvTest.csv");

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
                        new (PoType.Double, "Temp", [3,2]),
                    ]
                }
          ]
        };
        var cv = new ComplexVariable()
        {
            Time = -123,
            State =
            [
                new(){ Text = 1,Pos = new (){x=11,y=12 },Temp = new double[3,2]{ {1.1,1.2 },{1.3,1.4 }, { 1.3, 1.4 }}},
                new(){ Text = 2,Pos = new (){x=21,y=22 },Temp = new double[3,2]{ {2.1,2.2 },{2.3,2.4 }, { 1.3, 1.4 }}},
                new(){ Text = 3,Pos = new (){x=31,y=32 },Temp = new double[3,2]{ {3.1,3.2 },{3.3,3.4 }, { 1.3, 1.4 }}},
            ]
        };

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

        TypeRec keyValueType = new()
        {
            Id = 0,
            Name = "VariableKV",
            Desc = "comment",
            Inf = new TypeInf()
            {
                Type = PoType.Struct,
                Name = "KeyValue",
                Childs = new TypeInf[]
                {
                    new TypeInf(PoType.String, "Key"),
                    new TypeInf(PoType.String, "Value"),
                }
            }
        };
        
        int[] test = new int[100];
        for (uint i = 0; i < 100; i++)
            test[i] = (int)i;

        TypeRec rec = new()
        {
            Inf = new() { Type = PoType.Int32, Name = "variable" },
            Id = 0xF0F1F2F3
        };

        TypeRec tchar = new() { Inf = new(PoType.Char, string.Empty, [20]), Id = 0, Name = "Char Text" };

        TypeRec t = new() { Inf = new(PoType.Int32), Id = 0, Name = "weight variable" };

        TypeRec td = new() { Inf = new(PoType.Double), Id = 0, Name = "TestDouble" };

        using (var file = new FileStream(csvFile, FileMode.Create))
        {
            using (var wr = new CsvWriter(file))
            {
                wr.Write(TestStructInf);
                wr.Write(kvArr);

                wr.Write(new TypeRec() { Inf = comlexVarInf });
                wr.Write(cv);
                wr.Write(cv);
                wr.Write(cv);
                wr.Write(cv);

                wr.Write(keyValueType);
                wr.Write(new KeyValue() { Key = "Key1", Value = "Value1" });
                wr.Write(new KeyValue() { Key = "Key2", Value = "Value2" });
                wr.Write(new KeyValue() { Key = "Key3", Value = "Value3" });

                wr.Write(rec);
                Assert.AreEqual(EdfErr.IsOk, (EdfErr)wr.Write(test));

                wr.Write(tchar);
                Assert.AreEqual(EdfErr.IsOk, wr.Write(GetCString("Char", 20)));
                Assert.AreEqual(EdfErr.IsOk, wr.Write(GetCString("Value", 20)));
                Assert.AreEqual(EdfErr.IsOk, wr.Write(GetCString("Array     Value", 20)));

                wr.Write(t);
                Assert.AreEqual(EdfErr.IsOk, wr.Write(unchecked((int)0xFFFFFFFF)));

                wr.Write(td);
                Assert.AreEqual(EdfErr.IsOk, wr.Write(1.1d));
                Assert.AreEqual(EdfErr.IsOk, wr.Write(2.1d));
                Assert.AreEqual(EdfErr.IsOk, wr.Write(3.1d));
            }
        }
        Assert.IsTrue(File.Exists(csvFile));

    }
}
