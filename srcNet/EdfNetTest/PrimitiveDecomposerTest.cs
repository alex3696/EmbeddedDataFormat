using NetEdf;
using NetEdf.src;

namespace NetEdfTest;

[DecomposeGenerator]
public partial class KeyValueStruct
{
    public string? Key { get; set; }
    public string? Value { get; set; }
    public byte[]? Arr { get; set; }
}

[DecomposeGenerator]
public partial class MyPos1
{
    public UInt32 X { get; set; }
    public UInt32 Y { get; set; }
    public UInt32 Z { get; set; }
}


[DecomposeGenerator]
partial class KeyValue
{
    public string? Key { get; set; }
    public string? Value { get; set; }
};

[DecomposeGenerator]
partial class ComplexVariable1
{
    public long Time { get; set; }
    [DecomposeGenerator]
    public partial class StateT
    {
        public sbyte Text { get; set; }
        [DecomposeGenerator]
        public partial class PosT
        {
            public int x { get; set; }
            public int y { get; set; }
        };
        public PosT Pos { get; set; }
        public double[,] Temp { get; set; }
    };
    public StateT[] State { get; set; }
};

[TestClass]
public class PrimitiveDecomposerTest
{
    [TestMethod]
    public void DecomposeSimpleTypeTest()
    {
        int num = 12365;

        var decomposer = new PrimitiveDecomposer(num).ToArray();

        Assert.AreEqual(12365, decomposer[0]);
    }

    [TestMethod]
    public void DecomposeCollectionTypeTest()
    {
        int[] nums = { 1, 2, 3, 4 };

        var decomposer = new PrimitiveDecomposer(nums).ToArray();
        Assert.AreEqual((int)1, decomposer[0]);
        Assert.AreEqual((int)2, decomposer[1]);
        Assert.AreEqual((int)3, decomposer[2]);
        Assert.AreEqual((int)4, decomposer[3]);
    }

    [TestMethod]
    public void DecomposeDifficultTypeTest()
    {
        var playerInfo = new
        {
            Name = "Player",
            Health = 100,
            Level = 25,
            SkillPoints = 2,
            CountAchievements = 35

        };

        var decomposer = new PrimitiveDecomposer(playerInfo).ToArray();

        Assert.AreEqual("Player", decomposer[0]);
        Assert.AreEqual(100, decomposer[1]);
        Assert.AreEqual(25, decomposer[2]);
        Assert.AreEqual(2, decomposer[3]);
        Assert.AreEqual(35, decomposer[4]);
    }

    [TestMethod]
    public void DecomposeGenTest()
    {
        MyPos1 data = new() { X = 1, Y = 2, Z = 3 };
        var mypos = new MyPos1();
        var flatObj = mypos.Decompose(data).ToArray();
        //Assert.AreEqual(flatObj[0], (uint)1);
        //Assert.AreEqual(flatObj[1], (uint)2);
        //Assert.AreEqual(flatObj[2], (uint)3);

        ComplexVariable1 complex = new();
        var cv = new ComplexVariable1()
        {
            Time = -123,
            State =
            [
                new(){ Text = 1,Pos = new (){x=11,y=12 },Temp = new double[2,2]{ {1.1,1.2 },{1.3,1.4 } }  },
                new(){ Text = 2,Pos = new (){x=21,y=22 },Temp = new double[2,2]{ {2.1,2.2 },{2.3,2.4 } }  },
                new(){ Text = 3,Pos = new (){x=31,y=32 },Temp = new double[2,2]{ {3.1,3.2 },{3.3,3.4 } }  },
            ]
        };
        var dec = complex.Decompose(cv);

        KeyValueStruct str = new KeyValueStruct();
        KeyValueStruct val1 = new() { Key = "Key1", Value = "Value1", Arr = [11, 12, 13] };
        KeyValueStruct val2 = new() { Key = "Key2", Value = "Value2", Arr = [21, 22, 23] };
        KeyValueStruct[] kvArr = [val1, val2];
        flatObj = str.Decompose(kvArr);
    }
}
