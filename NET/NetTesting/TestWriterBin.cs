namespace NetTest;

[TestClass]
public class TestWriterBin
{
    public int Size { get; set; } = 1;
#pragma warning disable CS8618
    MemoryStream _msGen;
    EdfNet.Gen.WriterBin _writerGen;
#pragma warning restore CS8618

    public TestWriterBin()
        : this(1)
    {

    }
    public TestWriterBin(int count)
    {
        Size = count;
        _writerGen?.Dispose();
        _msGen?.Dispose();
        _msGen = new MemoryStream(1000);
        _writerGen = new(_msGen);
        _writerGen.WriteSchema(TestClasses_Content.KeyValSchema);
        _writerGen.WriteValue(TestClasses_Content.TestValue);
    }

    [TestMethod]
    public void Writer_Gen2()
    {
        for (int i = 0; i < Size; i++)
        {
            _msGen.Position = 0;
            _writerGen.WriteValue(TestClasses_Content.TestValue);
        }
    }
}
