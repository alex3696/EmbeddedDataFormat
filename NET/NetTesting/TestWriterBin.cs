namespace NetTest;

[TestClass]
public class TestWriterBin
{
    public int Size { get; set; } = 1;
#pragma warning disable CS8618
    MemoryStream _msGen;
    EdfNet.Gen.WriterBin _writerEnum;
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
        _msGen?.Dispose();
        _writerEnum?.Dispose();

        Size = count;
        _msGen = new MemoryStream(1000);
        _writerEnum = new(_msGen);
        _writerEnum.Write(TestClasses_Content.KeyValSchema);
    }


    [TestMethod]
    public void Writer_Enum()
    {
        for (int i = 0; i < Size; i++)
        {
            _msGen.Position = 0;
            var enm = TestClasses_Content.TestValue.GetByteEnumerator();
            _writerEnum.WriteEnumerator(ref enm);
        }
    }

}
