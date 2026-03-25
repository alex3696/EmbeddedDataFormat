using NetEdf.src;
using static NetEdfTest.TestStructSerialize;

namespace NetEdfTest;

[TestClass]
public class TestCsvWriter
{
    static string _testPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
    static string GetTestFilePath(string filename) => Path.Combine(_testPath, filename);


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
        TypeRec rec = new()
        {
            Inf = new() { Type = PoType.Int32, Name = "variable" },
            Id = 0xF0F1F2F3
        };

        using (var file = new FileStream(csvFile, FileMode.Create))
        {
            using (var wr = new CsvWriter(file))
            {
               // wr.Write(TestStructInf);
                wr.Write(new TypeRec() { Inf = comlexVarInf });
                //wr.Write(keyValueType);
                //wr.Write(rec);
                wr.Write(cv);
            }
        }
        Assert.IsTrue(File.Exists(csvFile));

    }
}
