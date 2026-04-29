using NetEdf.src;
using System.ComponentModel;
using System.Net.WebSockets;

namespace NetEdfTest;

[TestClass]
public class TestTxtWriter
{
    static string _testPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
    static string GetTestFilePath(string filename) => Path.Combine(_testPath, filename);

    struct PlayerStats
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

        using (var file = new FileStream(txtFile, FileMode.Create))
        using (var writer = new TxtWriter(file))
        {
            writer.Write(playerRec);
            Assert.AreEqual(EdfErr.IsOk, writer.Write(new PlayerStats()
            {Name = "Player",Health = 100,Level = 25,SkillPoints = 2,CountAchievements = 35}));
            Assert.AreEqual(EdfErr.IsOk, writer.Write(new PlayerStats()
            {Name = "Player1",Health = 52,  Level = 55, SkillPoints = 0,CountAchievements = 125}));
        }
        Assert.IsTrue(File.Exists(txtFile));
        using (var file = new FileStream(txtFile, FileMode.Open))
        using (var reader = new TextRead(file))
        {
            reader.ReadInfo();
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

        for(int i = 0; i < bigNums.Length; ++i)
            bigNums[i] = i * 10;

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
    }
}
