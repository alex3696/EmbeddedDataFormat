using NetEdf.src;
using static NetEdfTest.TestStructSerialize;
using static System.Net.WebRequestMethods;

namespace NetEdfTest;

[TestClass]
public class ReflectionTest
{
    //class Position
    //{
    //    public int X { get; set; }
    //    public int Y { get; set; }
    //}
    //struct KeyValueStruct3
    //{
    //    public string Key { get; set; }
    //    public string? Value { get; set; }
    //    public Position Pos { get; set; }
    //    public Position Pos1 { get; set; }
    //    public Position Pos2 { get; set; }
    //    public Position[] Pos3 { get; set; }

    //    public int[] Data { get; set; }
    //}


    //struct KeyValue
    //{
    //    public string? Key { get; set; }
    //    public string? Value { get; set; }
    //};
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
    public void TestSimple1()
    {
        var inf = new TypeInf()
        {
            Type = PoType.Int32,
        };
        Reflection reflection = new();
        var result = reflection.GetInf((int)123);
        var result1 = reflection.GetInf((int)123);
        Assert.AreEqual(inf, result);
        var result2 = reflection.GetInf(typeof(int));
        Assert.AreEqual(inf, result2);
    }


    [TestMethod]
    public void TestMethod1()
    {
        KeyValueStruct val1 = new() { Key = "Key1", Value = "Value1", Arr = [11, 12, 13] };
        KeyValueStruct val2 = new() { Key = "Key2", Value = "Value2", Arr = [21, 22, 23] };
        KeyValueStruct[] kvArr = [val1, val2];

        KeyValueStruct keyValueStruct1 = new();

        var inf = new TypeInf()
        {
            Type = PoType.Struct,
            Name = "KeyValue",
            Dims = [2],
            Childs =
            [
                new (PoType.String, "Key"),
                new (PoType.String, "Value"),
                new (PoType.UInt8, "Arr", [3]),
            ]
        };




        Reflection reflection = new Reflection();
        var result1 = reflection.GetInf(kvArr);
        result1.Name = "KeyValue";
        //var result = reflection.GetPropertyAndName(keyValueStruct1);

        // Assert.AreEqual(inf, result1);

        Assert.AreEqual(inf, result1);


        //var result1 = reflection.GetValueType(ss);
        /*
        TypeRec typeRec = new()
        {
            Inf = new()
            {
                Type = PoType.Struct,
                Name = "KeyValue",
                Dims = [2],
                Childs = [
                    new (PoType.String, "Key"),
                    new (PoType.String, "Value")
                   ]
            }
        };
        */
        //TypeRec typeRec1 = new()
        //{
        //    Inf = new()
        //    {
        //        Type = PoType.Struct,
        //        Name = "KeyValue",
        //        Dims = [2],
        //        Childs = new TypeInf[reflection.typeInfo.Count]
        //    }
        //};

        //reflection.Fill(typeRec1);


        //var actual = typeRec1.Inf.Equals(typeRec.Inf);

        //Assert.IsTrue(actual);
    }
}
