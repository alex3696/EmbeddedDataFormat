using NetEdf.src;

namespace NetEdfTest;

[TestClass]
public class TestBinWriteReaderr
{

    static string _testPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
    static string GetTestFilePath(string filename) => Path.Combine(_testPath, filename);

    //Чтение из Бинарного
    //Запись больших данных
    //Чтение больших данных

    public struct PlayerStats
    {
        public string Name { get; set; }
        public sbyte Health { get; set; }
        public ushort Level { get; set; }

        public byte SkillPoints { get; set; }
        public uint CountAchievements { get; set; }
    }

     //Запись в бинарный
    [TestMethod]
    public void WriterReaderTest()
    {
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

        PlayerStats ps = new()
        {
            Name = "Player",
            Health = 100,
            Level = 25,
            SkillPoints = 2,
            CountAchievements = 35
        };

        PlayerStats ps1 = new()
        {
            Name = "Player1",
            Health = 52,
            Level = 55,
            SkillPoints = 0,
            CountAchievements = 125
        };

        PlayerStats ps2 = new()
        { 
            Name = "Player2",
            Health = 75,
            Level = 44,
            SkillPoints = 2,
            CountAchievements = 120
        };

        PlayerStats[] psMassive = [ps1, ps2, ps];

        byte[] binBuf = new byte[1024];
        using (var memStream = new MemoryStream(binBuf))
        {
            using (var bw = new BinWriter(memStream))
            {
                bw.Write(playerRec);
                Assert.AreEqual(EdfErr.IsOk, bw.Write(psMassive));
                Assert.AreEqual(31, bw.CurrentQty);
            }
        }

    }

    public struct ArmorSettings
    {
        public ushort DefenseValue;      
        public ushort MagicResistance;   
        public Half Weight;         
        public ushort Durability;    
        public ushort MaxDurability;  

        // Качество предмета: 1 - Обычное, 2 - Редкое 3 - Эпическое 4 - Легендарное
        public byte RarityLevel;
    }

    [TestMethod]
    public void WriterReaderFrowFileTest()
    {
        string binFile = GetTestFilePath("ArmorSettings2.bdf");

        TypeRec ArmorRec = new()
        {
            Inf = new()
            {
                Type = PoType.Struct,
                Name = "ArmorSettings",
                Childs =
                [
                    new (PoType.UInt16, "DefenseValue"),
                    new (PoType.UInt16, "MagicResistance"),
                    new (PoType.Half, "Weight"),
                    new (PoType.UInt16, "Durability"),
                    new (PoType.UInt16, "MaxDurability"),
                    new (PoType.UInt8, "RarityLevel")
                ]
            }
        };
        
        using (var file = new FileStream(binFile, FileMode.Create))
        {
            using (var bw = new BinWriter(file))
            {
                bw.Write(ArmorRec);
            }
        }
    }
}
