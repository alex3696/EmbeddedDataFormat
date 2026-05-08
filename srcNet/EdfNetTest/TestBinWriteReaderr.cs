using NetEdf.src;
using Newtonsoft.Json.Converters;
using System.IO.Compression;
using System.Text;

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
            if (!Equals(SkillPoints, other.SkillPoints))
                return false;
            if (!Equals(CountAchievements, other.CountAchievements))
                return false;
            return true;
        }

    }

    // Запись и чтение структуры данных в память с помощью BinWriter и BinReader, а также проверка на корректность записанных и прочитанных данных.
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

        using var mssrc = new MemoryStream(binBuf);
        using var reader = new BinReader(mssrc);
        Assert.IsTrue(reader.ReadBlock());
        var rec = reader.ReadInfo();
        Assert.IsNotNull(rec);
        Assert.IsTrue(playerRec.Inf.Equals(rec.Inf));

        reader.ReadBlock();
        Assert.AreEqual(EdfErr.IsOk, reader.TryRead(out PlayerStats[]? psRead));
        Assert.IsTrue(psMassive[0].Equals(psRead[0]));
        Assert.IsTrue(psMassive[1].Equals(psRead[1]));


    }
    
    class ArmorSettings
    {
        public ushort DefenseValue { get; set; }
        public ushort MagicResistance { get; set; }
        public double Weight { get; set; }
        public ushort Durability { get; set; }
        public ushort MaxDurability { get; set; }
        public byte RarityLevel { get; set; }

        public bool Equals(ArmorSettings? other)
        {
            if (other is null)
                return false;
            if (!Equals(DefenseValue, other.DefenseValue))
                return false;
            if (!Equals(MagicResistance, other.MagicResistance))
                return false;
            if (!Equals(Weight, other.Weight))
                return false;
            if (!Equals(Durability, other.Durability))
                return false;
            if (!Equals(MaxDurability, other.MaxDurability))
                return false;
            if (!Equals(RarityLevel, other.RarityLevel))
                return false;
            return true;
        }
    }

    // Запись и чтение структуры данных в файл с помощью BinWriter и BinReader, а также проверка на корректность записанных и прочитанных данных.
    [TestMethod]
    public void WriterReaderFromFileTest()
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

        using (var file = new FileStream(binFile, FileMode.Open))
        {
            using var reader = new BinReader(file);
            Assert.IsTrue(reader.ReadBlock());
            var rec = reader.ReadInfo();
            Assert.IsNotNull(rec);
            Assert.IsTrue(ArmorRec.Inf.Equals(rec.Inf));
            reader.ReadBlock();
            Assert.AreEqual(EdfErr.IsOk, reader.TryRead(out ArmorSettings? armSetRead));
            Assert.IsTrue(armSet.Equals(armSetRead));
        }
    }

    //Запись и чтение большого массива данных с проверкой на частичную запись при превышении размера блока данных.

    [TestMethod]
    public void WriteReadBigDataTest()
    {
        string binFile = GetTestFilePath("BigDataTest.bdf");
        string binPathWriteFile = GetTestFilePath("BigDataPathWriteTest.bdf");
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
            using var bw = new BinWriter(file);
            bw.Write(arrRec);
            Assert.AreEqual(EdfErr.IsOk, bw.Write(arr));
        }
        Assert.IsTrue(File.Exists(binFile));

        using (var file = new FileStream(binPathWriteFile, FileMode.Create))
        {
            using var bw = new BinWriter(file);
            bw.Write(arrRec);
            Assert.AreEqual(EdfErr.IsOk, bw.Write(arr));
            Assert.AreEqual(EdfErr.SrcDataRequred, bw.Write(arr.AsSpan(0, 250).ToArray()));
            Assert.AreEqual(EdfErr.SrcDataRequred, bw.Write(arr.AsSpan(250, arr.Length - 400).ToArray()));
            Assert.AreEqual(EdfErr.IsOk, bw.Write(arr.AsSpan(arr.Length - 150).ToArray()));
        }
        Assert.IsTrue(File.Exists(binPathWriteFile));

        using (var file = new FileStream(binFile, FileMode.Open))
        {
            using var reader = new BinReader(file);
            Assert.IsTrue(reader.ReadBlock());
            var rec = reader.ReadInfo();
            Assert.IsNotNull(rec);
            Assert.IsTrue(arrRec.Inf.Equals(rec.Inf));

            StringBuilder sb = new(1000);
            long[]? arrRead = null;
            try
            {
                while (reader.ReadBlock() && BlockType.VarData == reader.GetBlockType())
                {
                    reader.TryRead(out arrRead);
                    Console.WriteLine($"BlockLen={reader.GetBlockLen()} {reader.GetBlockSeq()}" +
                        $" {arrRead is not null}");
                }
            }
            catch (EndOfStreamException ex)
            {
                Console.WriteLine($"file end msg={ex}");
            }

            if (arrRead is not null)
            {
                foreach (var item in arrRead)
                    sb.Append($"{item}, ");
                Console.WriteLine($"[{arrRead.Length}] {sb}");
            }

            Assert.IsTrue(arr.SequenceEqual(arrRead));
        }
    }

    class ComplexVariable
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
    public void WriteReadDiffStructTest()
    {
        string binFile = GetTestFilePath("ComplexVariable.bdf");
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
               new(){ Text = 1,Pos = new (){x=11,y=12 },Temp = new double[2] {1.1,1.2 }  },
                new(){ Text = 2,Pos = new (){x=21,y=22 },Temp = new double[2]{2.3,2.4 }  },
                new(){ Text = 3,Pos = new (){x=31,y=32 },Temp = new double[2]{3.3,3.4 }  },
            ]
        };

        using (var file = new FileStream(binFile, FileMode.Create))
        {
            using var bw = new BinWriter(file);
            bw.Write(new TypeRec() { Inf = comlexVarInf });
            Assert.AreEqual(EdfErr.IsOk, bw.Write(cv));
        }

        using (var file = new FileStream(binFile, FileMode.Open))
        {
            using var reader = new BinReader(file);
            Assert.IsTrue(reader.ReadBlock());
            var rec = reader.ReadInfo();
            Assert.IsNotNull(rec);
            Assert.IsTrue(comlexVarInf.Equals(rec.Inf));
            reader.ReadBlock();
            Assert.AreEqual(EdfErr.IsOk, reader.TryRead(out ComplexVariable? ret));
         

        }
    }
}
