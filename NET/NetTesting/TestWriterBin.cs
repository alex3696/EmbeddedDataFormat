namespace NetTest;

[TestClass]
public class TestWriterBin
{
    public int Size { get; set; } = 1;
#pragma warning disable CS8618
    MemoryStream _msEnm;
    MemoryStream _msGen;
    EdfNet.Gen.WriterBin _writerEnum;
    EdfNet.Gen.WriterBin2 _writerGen;
#pragma warning restore CS8618

    public TestWriterBin()
        : this(1)
    {

    }
    public TestWriterBin(int count)
    {
        Setup(count);
    }

    public void Setup(int count = 1)
    {
        Size = count;

        _writerEnum?.Dispose();
        _msEnm?.Dispose();
        _msEnm = new MemoryStream(1000);
        _writerEnum = new(_msEnm);
        _writerEnum.Write(TestClasses_Content.KeyValSchema);
        var enm = TestClasses_Content.TestValue.GetByteEnumerator();
        _writerEnum.WriteEnumerator(ref enm);

        _writerGen?.Dispose();
        _msGen?.Dispose();
        _msGen = new MemoryStream(1000);
        _writerGen = new(_msGen);
        _writerGen.Write(TestClasses_Content.KeyValSchema);
        _writerGen.Write(TestClasses_Content.TestValue);
    }


    [TestMethod]
    public void Writer_Enum()
    {
        for (int i = 0; i < Size; i++)
        {
            _msEnm.Position = 0;
            var enm = TestClasses_Content.TestValue.GetByteEnumerator();
            _writerEnum.WriteEnumerator(ref enm);
        }
    }
    [TestMethod]
    public void Writer_Gen2()
    {
        for (int i = 0; i < Size; i++)
        {
            _msGen.Position = 0;
            _writerGen.Write(TestClasses_Content.TestValue);
        }
    }
}
