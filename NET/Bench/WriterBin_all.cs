using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System.IO;

namespace Bench;

[MemoryDiagnoser(false)]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class WriterBin_all
{
    [Params(1, 100_000)]
    public int Size { get; set; }

#pragma warning disable CS8618
    MemoryStream _ms;
    EdfNet.Gen.WriterBin _writerEnum;
    EdfNet.Ref.WriterBin _writerRef;
    MyPosition[] _list;
#pragma warning restore CS8618

    [GlobalSetup]
    public void Setup()
    {
        _ms = new MemoryStream(100_000 * 4 * 8);
        _writerEnum = new(_ms);
        _writerRef = new(_ms);
        _list = new MyPosition[Size];
        for (int i = 0; i < Size; i++)
            _list[i] = new MyPosition() { X = i, Y = i / 2d, Z = i / 3d };
        _writerEnum.Write(MyPosition.GetEdfSchema());
        _writerRef.Write(MyPosition.GetEdfSchema());
    }

    [Benchmark(Baseline = true)]
    public void Writer_Boxed()
    {
        _ms.Position = 0;
        for (int i = 0; i < Size; i++)
        {
            var enm = new MyPositionByteEnumerator(_list[i]);
            _writerEnum.WriteEnumerator(ref enm);
        }
    }
    [Benchmark]
    public void Writer_Reflexion()
    {
        _ms.Position = 0;
        for (int i = 0; i < Size; i++)
        {
            _writerRef.Write(_list[i]);
        }
    }
}
