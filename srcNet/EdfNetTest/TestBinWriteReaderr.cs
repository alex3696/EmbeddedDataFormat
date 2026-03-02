using NetEdf.src;

namespace NetEdfTest;

[TestClass]
public class TestBinWriteReaderr
{
    //Запись в бинарный
    //Чтение из Бинарного
    //Запись больших данных
    //Чтение больших данных

    
    static string _testPath = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)}";
    static string GetTestFilePath(string filename) => Path.Combine(_testPath, filename);

    struct KeyValue
    {
        public int Key { get; set; }
        public string Value { get; set; }
    }

    [TestMethod]
    public void WriterTest()
    {
        TypeRec typeRec = new()
        {
            Inf = new()
            {
                Type = PoType.Struct,
                Name = "KeyValue",
                Dims = [2],
                Childs =
                [
                    new (PoType.Int32, "Key"),
                    new (PoType.String, "Value"),
                ]
            }
        };

        KeyValue kv = new KeyValue { Key = 1, Value = "value1" };
        KeyValue kv1 = new KeyValue { Key = 2, Value = "value2" };
        KeyValue[] kvMassive = [kv, kv1];
        byte[] binBuf = new byte[1024];
        using (var memStream = new MemoryStream(binBuf)) 
        using (var bw = new BinWriter(memStream))
        {
            bw.Write(typeRec);
            bw.Write(kvMassive);
            Assert.AreEqual(22, bw.CurrentQty);
           
        }
    }
}
