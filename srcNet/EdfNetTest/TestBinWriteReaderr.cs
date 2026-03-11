using NetEdf.src;
using Newtonsoft.Json.Linq;
using System.Reflection.PortableExecutable;

namespace NetEdfTest;

[TestClass]
public class TestBinWriteReaderr
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
             

    }

    public struct ArmorSettings
    {
        public ushort DefenseValue { get; set; }      
        public ushort MagicResistance { get; set; }
        public double Weight { get; set; }
        public ushort Durability { get; set; }
        public ushort MaxDurability { get; set; }
        public byte RarityLevel{ get; set; }
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
                    new (PoType.Double, "Weight"),
                    new (PoType.UInt16, "Durability"),
                    new (PoType.UInt16, "MaxDurability"),
                    new (PoType.UInt8, "RarityLevel"),
    
                ]
            }
        };

        ArmorSettings armSet = new()
        {
            DefenseValue = 10,
            MagicResistance = 10,
            Weight = 6.75,
            Durability = 15,
            MaxDurability = 30,
            RarityLevel = 3,
        };

        using (var file = new FileStream(binFile, FileMode.Create))
        {
            using (var bw = new BinWriter(file))
            {
                bw.Write(ArmorRec);
                Assert.AreEqual(EdfErr.IsOk, bw.Write(armSet));
                Assert.AreEqual(17, bw.CurrentQty);
            }
        }
        Assert.IsTrue(File.Exists(binFile));
    }

    //Запись больших данных

    [TestMethod]
    public void WriteReadBigDataTest()
    {
        string binFile = GetTestFilePath("BigDataTest.bdf");
        long[] arr = new long[1000];

        for (int i = 0; i < arr.Length; i++)
            arr[i] = i * 10;

        TypeRec arrRec = new()
        {
            Inf = new()
            {
                Type = PoType.Int64,
                Name = "IntArray",
                Dims = [1000],
            }
        };

        using (var file = new FileStream(binFile, FileMode.Create))
        {
            using (var bw = new BinWriter(file))
            {
                bw.Write(arrRec);
                Assert.AreEqual(EdfErr.IsOk, bw.Write(arr));
                Assert.AreEqual(EdfErr.SrcDataRequred, bw.Write(arr.AsSpan(0, 250).ToArray()));
                Assert.AreEqual(EdfErr.SrcDataRequred, bw.Write(arr.AsSpan(250, arr.Length - 400).ToArray()));
                Assert.AreEqual(EdfErr.IsOk, bw.Write(arr.AsSpan(arr.Length - 150).ToArray()));
        
            }
        }
        Assert.IsTrue(File.Exists(binFile));
    }
}
