using NetEdf.src;
using System.ComponentModel;

namespace NetEdfTest;

[TestClass]
public class TestTxtWriterReader
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

    [TestMethod]
    public void WriteHeaderAndTypeRecTest()
    {
        string txtFile = GetTestFilePath("t_write.tdf");

        Header header = new Header();
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
            writer.Write(header);
            writer.Write(playerRec);
            Assert.AreEqual(EdfErr.IsOk, writer.Write(new PlayerStats()
            {Name = "Player",Health = 100,Level = 25,SkillPoints = 2,CountAchievements = 35}));
            Assert.AreEqual(EdfErr.IsOk, writer.Write(new PlayerStats()
            {Name = "Player1",Health = 52,  Level = 55, SkillPoints = 0,CountAchievements = 125}));
        }
        Assert.IsTrue(File.Exists(txtFile));

    }

  

  
}
