using NetEdf.src;

namespace NetEdfTest;

[TestClass]
public class BinToTxtConverterTest
{

    static string _testPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
    static string GetTestFilePath(string filename) => Path.Combine(_testPath, filename);
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

    [TestMethod]
    public void BinToTxtTest()
    {
        string binFile = GetTestFilePath("ArmorSettings.bdf");
        string txtFile = GetTestFilePath("ArmorSettingsTxt.tdf");
        string converterFile = GetTestFilePath("ArmorSettingsConverter.tdf");

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

        using (var file = new FileStream(txtFile, FileMode.Create))
        {
            using (var bw = new TxtWriter(file))
            {
                bw.Write(ArmorRec);
                bw.Write(armSet);
            }
        }
        Assert.IsTrue(File.Exists(binFile));

        using (var file = new BinToTxtConverter(binFile, converterFile))
        {
            file.Execute();

        }
        Assert.IsTrue(File.Exists(converterFile));
        bool isEqual = FileUtils.FileCompare(txtFile, converterFile);
        Assert.IsTrue(isEqual);
    }
}
