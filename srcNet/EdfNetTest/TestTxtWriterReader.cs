using Microsoft.Testing.Platform.Extensions.Messages;
using NetEdf.src;
using System.ComponentModel;
using System.Net.WebSockets;

namespace NetEdfTest;

[TestClass]
public class TestTxtWriterReader
{
    static string _testPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
    static string GetTestFilePath(string filename) => Path.Combine(_testPath, filename);


    class PlayerStats
    {
        public string Name { get; set; }
        public sbyte Health { get; set; }
        public ushort Level { get; set; }
        public byte SkillPoints { get; set; }
        public uint CountAchievements { get; set; }
    }
    //запись маленьких данных в текстовый файл
    [TestMethod]
    public void WriteReadTest()
    {
        string txtFile = GetTestFilePath("PlayerInfo.tdf");

        TypeRec playerRec = new()
        {
            Inf = new()
            {
                Type = PoType.Struct,
                Name = "PlayerInfo",
                Childs =
                [
                    new (PoType.String, "Name"),
                    new (PoType.Int8, "Healtg"),
                    new (PoType.UInt16, "Level"),
                    new (PoType.UInt8, "SkillPoints"),
                    new (PoType.UInt32, "CountAchievements")
                ]
            }
        };
        var playerStats1 = new PlayerStats()
        { Name = "Player", Health = 100, Level = 25, SkillPoints = 2, CountAchievements = 35 };
        var playerStats2 = new PlayerStats()
        { Name = "Player1", Health = 52, Level = 55, SkillPoints = 0, CountAchievements = 125 };
        using (var file = new FileStream(txtFile, FileMode.Create))
        using (var writer = new TxtWriter(file))
        {
            writer.Write(playerRec);
            Assert.AreEqual(EdfErr.IsOk, writer.Write(playerStats1));
            Assert.AreEqual(EdfErr.IsOk, writer.Write(playerStats2));
        }
        Assert.IsTrue(File.Exists(txtFile));
        using (var file = new FileStream(txtFile, FileMode.Open))
        using (var reader = new NetEdf.src.TextReader(file))
        {
            var rec = reader.ReadInfo();
            Assert.IsTrue(playerRec.Inf.Equals(rec.Inf));
            reader.TryRead(out PlayerStats? ret);
            Assert.AreEqual(playerStats1.Name, ret.Name);
            Assert.AreEqual(playerStats1.Health, ret.Health);
            Assert.AreEqual(playerStats1.Level, ret.Level);
            Assert.AreEqual(playerStats1.SkillPoints, ret.SkillPoints);
            Assert.AreEqual(playerStats1.CountAchievements, ret.CountAchievements);

            reader.TryRead(out PlayerStats? ret1);
            Assert.AreEqual(playerStats2.Name, ret1.Name);
            Assert.AreEqual(playerStats2.Health, ret1.Health);
            Assert.AreEqual(playerStats2.Level, ret1.Level);
            Assert.AreEqual(playerStats2.SkillPoints, ret1.SkillPoints);
            Assert.AreEqual(playerStats2.CountAchievements, ret1.CountAchievements);

        }
    }


    //запись больших данных в текстовый файл
    [TestMethod]
    public void WriteBigDataTest()
    {
        string txtFile = GetTestFilePath("BigData.tdf");
        string txtPathWriteFile = GetTestFilePath("BigPathWriteData.tdf");

        TypeRec bigData = new()
        {
            Inf = new()
            {
                Type = PoType.Int64,
                Name = "BigNumbers",
                Dims = [1000]
            }
        };
        long[] bigNums = new long[1000];

        for (int i = 0; i < bigNums.Length; ++i)
            bigNums[i] = i;

        using (var file = new FileStream(txtFile, FileMode.Create))
        using (var writer = new TxtWriter(file))
        {
            writer.Write(bigData);
            Assert.AreEqual(EdfErr.IsOk, writer.Write(bigNums));
        }
        Assert.IsTrue(File.Exists(txtFile));

        using (var file = new FileStream(txtPathWriteFile, FileMode.Create))
        using (var writer = new TxtWriter(file))
        {
            writer.Write(bigData);
            Assert.AreEqual(EdfErr.SrcDataRequred, writer.Write(bigNums.AsSpan(0, 150).ToArray()));
            Assert.AreEqual(EdfErr.SrcDataRequred, writer.Write(bigNums.AsSpan(150, bigNums.Length - 500).ToArray()));
            Assert.AreEqual(EdfErr.IsOk, writer.Write(bigNums.AsSpan(bigNums.Length - 350).ToArray()));
        }
        Assert.IsTrue(File.Exists(txtPathWriteFile));

        using (var file = new FileStream(txtFile, FileMode.Open))
        using (var reader = new NetEdf.src.TextReader(file))
        using (var writer = new StreamWriter(GetTestFilePath("BigData.txt")))
        {
            var rec = reader.ReadInfo();
            Assert.IsTrue(bigData.Inf.Equals(rec.Inf));
            reader.TryRead(out long[]? data);
            Assert.IsTrue(bigNums.SequenceEqual(data));
            for (int i = 0; i < data.Length; i++)
                writer.Write($"{data[i]} ");
        }
    }

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
            public double[] Temp { get; set; }
        };
        public StateT[] State { get; set; }
    };

    [TestMethod]
    public void ReadTextTest()
    {
        string txtFile = GetTestFilePath("t_writeComplexVariable.tdf");

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
                        new (PoType.Double, "Temp", [2]),
                    ]
                }
           ]
        };

        var cv = new ComplexVariable()
        {
            Time = -123,
            State =
            [
                new(){ Text = 1,Pos = new (){x=11,y=12 },Temp = new double[2]{1.3,1.4}},
                new(){ Text = 2,Pos = new (){x=21,y=22 },Temp = new double[2]{2.1,2.2}},
                new(){ Text = 3,Pos = new (){x=31,y=32 },Temp = new double[2]{3.3,3.4}},
            ]
        };

        using (var file = new FileStream(txtFile, FileMode.Create))
        using (var writer = new TxtWriter(file))
        {
            writer.Write(new TypeRec() { Inf = comlexVarInf });
            writer.Write(cv);
        }
        Assert.IsTrue(File.Exists(txtFile));

        string outputFilePath = "output_results.txt";

        using (var file = new FileStream(txtFile, FileMode.Open))
        using (var reader = new NetEdf.src.TextReader(file))
        using (var writer = new StreamWriter(GetTestFilePath(outputFilePath), false, System.Text.Encoding.UTF8))
        {
            var rec = reader.ReadInfo();
            Assert.IsTrue(comlexVarInf.Equals(rec.Inf));
            reader.TryRead(out ComplexVariable? ret);
            var result = ret.Value;

            Assert.AreEqual(cv.Time, result.Time);
            Assert.IsNotNull(result.State);
            Assert.AreEqual(cv.State.Length, result.State.Length);

            writer.WriteLine($"Time: {result.Time}");
            writer.WriteLine($"States Count: {result.State.Length}");
            writer.WriteLine(new string('-', 20));

            for (int i = 0; i < cv.State.Length; i++)
            {
                var expectedState = cv.State[i];
                var actualState = result.State[i];

                Assert.AreEqual(expectedState.Text, actualState.Text);
                Assert.AreEqual(expectedState.Pos.x, actualState.Pos.x);
                Assert.AreEqual(expectedState.Pos.y, actualState.Pos.y);
                Assert.IsNotNull(actualState.Temp);
                CollectionAssert.AreEqual(expectedState.Temp, actualState.Temp);

                writer.WriteLine($"State #{i}:");
                writer.WriteLine($"  Text: {actualState.Text}");
                writer.WriteLine($"  Position: X={actualState.Pos.x}, Y={actualState.Pos.y}");
                writer.WriteLine($"  Temperatures: {string.Join(", ", actualState.Temp)}");
                writer.WriteLine();
            }
        }
    }
}

