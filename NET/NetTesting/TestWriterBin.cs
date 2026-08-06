namespace NetTest;

[TestClass]
public class TestWriterBin
{
    public int Size { get; set; } = 1;
#pragma warning disable CS8618
    MemoryStream _msGen;
    MemoryStream _msRef;
    EdfNet.Gen.WriterBin _writerEnum;
    EdfNet.Ref.WriterBin _writerRef;
    MyPosition[] _list;
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
        _msRef?.Dispose();
        _writerEnum?.Dispose();
        _writerRef?.Dispose();

        Size = count;
        _msGen = new MemoryStream(1000);
        _msRef = new MemoryStream(1000);
        _writerEnum = new(_msGen);
        _writerRef = new(_msRef);
        _list = new MyPosition[Size];
        for (int i = 0; i < Size; i++)
            _list[i] = new MyPosition() { X = i, Y = i * 2, Z = i * 3 };
        _writerEnum.Write(MyPosition.GetEdfSchema());
        _writerRef.Write(MyPosition.GetEdfSchema());
    }


    [TestMethod]
    public void Writer_Enum()
    {
        for (int i = 0; i < Size; i++)
        {
            _msGen.Position = 0;
            var enm = new MyPositionByteEnumerator(_list[i]);
            _writerEnum.WriteEnumerator(ref enm);
        }
    }
    [TestMethod]
    public void Writer_Reflection()
    {
        for (int i = 0; i < Size; i++)
        {
            _msRef.Position = 0;
            _writerRef.Write(_list[i]);
        }
    }
}
