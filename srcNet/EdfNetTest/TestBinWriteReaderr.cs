using NetEdf.src;
using Newtonsoft.Json.Linq;
using System.Reflection.PortableExecutable;

namespace NetEdfTest;

[TestClass]
public class TestBinWriteReaderr
{

    static string _testPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
    static string GetTestFilePath(string filename) => Path.Combine(_testPath, filename);

  
    //Запись больших данных
    //Чтение больших данных

    class PlayerStats
    {
        public string Name { get; set; }
        public sbyte Health { get; set; }
        public ushort Level { get; set; }
        public byte SkillPoints { get; set; }
        public uint CountAchievements { get; set; }

        public bool Equals(PlayerStats? other)
        {
            if (other is null)
                return false;
            if (!string.Equals(Name, other.Name))
                return false;
            if (!Equals(Health, other.Health))
                return false;
            if (!Equals(Level, other.Level))
                return false;
            if(!Equals(SkillPoints, other.SkillPoints))
                return false;
            if(!Equals(CountAchievements, other.CountAchievements))
                return false;
            return true;
        }
      
    }

   
    [TestMethod]
    public void WriterReaderTest()
    {
        TypeRec playerRec = new()
        {
            Inf = new()
            {
                Type = PoType.Struct,
                Name = "PlayerInfo",
                Dims = [2],
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

        PlayerStats[] psMassive = [ps, ps1];

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

        var mssrc = new MemoryStream(binBuf);
        using var reader = new BinReader(mssrc);

        Assert.IsTrue(reader.ReadBlock());

        var rec = reader.ReadInfo();
        Assert.IsNotNull(rec);

        Assert.IsTrue(reader.ReadBlock());

        reader.TryRead(out PlayerStats[]? psData);

        Assert.IsTrue(psMassive[0].Equals(psData[0]));
        Assert.IsTrue(psMassive[1].Equals(psData[1]));
    }

    public struct ArmorSettings
    {
        public ushort DefenseValue;      
        public ushort MagicResistance;   
        public Half Weight;         
        public ushort Durability;    
        public ushort MaxDurability;
        public byte RarityLevel;
    }

    [TestMethod]
    public void WriterReaderFrowFileTest()
    {
        string binFile = GetTestFilePath("ArmorSettings.bdf");

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

        ArmorSettings armSet = new()
        {
            DefenseValue = 10,
            MagicResistance = 10,
            Weight = (Half)6.75,
            Durability = 15,
            MaxDurability = 30,
            RarityLevel = 3
        };

        using (var file = new FileStream(binFile, FileMode.Create))
        {
            using (var bw = new BinWriter(file))
            {
                bw.Write(ArmorRec);
                bw.Write(armSet);
            }
        }
        Assert.IsTrue(File.Exists(binFile));

        using (var file = new FileStream(binFile, FileMode.Open))
        {
            using (var reader = new BinReader(file))
            {
                
            }
            file.Seek(0, SeekOrigin.End);
        }
        
    }
                
}
