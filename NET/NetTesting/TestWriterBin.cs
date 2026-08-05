namespace NetTest;

[TestClass]
public class TestWriterBin
{
    public int Size { get; set; } = 1;
#pragma warning disable CS8618
    MemoryStream _ms;
    EdfNet.Gen.WriterBin _writerEnum;
    EdfNet.Ref.WriterBin _writerRef;
    MyPosition[] _list;
#pragma warning restore CS8618

    public TestWriterBin()
        :this(1)
    {

    }
    public TestWriterBin(int count)
    {
        Setup(count);
    }


    public void Setup(int count = 1)
    {
        _ms?.Dispose();
        _writerEnum?.Dispose();
        _writerRef?.Dispose();

        Size = count;
        _ms = new MemoryStream(100_000 * 4 * 8);
        _writerEnum = new(_ms);
        _writerRef = new(_ms);
        _list = new MyPosition[Size];
        for (int i = 0; i < Size; i++)
            _list[i] = new MyPosition() { X = i, Y = i * 2, Z = i * 3 };
        _writerEnum.Write(MyPosition.GetEdfSchema());
        _writerRef.Write(MyPosition.GetEdfSchema());
    }


    [TestMethod]
    public void Writer_Enum()
    {
        _ms.Position = 0;
        for (int i = 0; i < Size; i++)
        {
            var enm = new MyPositionByteEnumerator(_list[i]);
            _writerEnum.WriteEnumerator(ref enm);
        }
    }
    [TestMethod]
    public void Writer_Reflection()
    {
        _ms.Position = 0;
        for (int i = 0; i < Size; i++)
        {
            _writerRef.Write(_list[i]);
        }
    }
}
