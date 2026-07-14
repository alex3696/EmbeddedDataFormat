using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EdfNet.Ref;
using System.Collections.Generic;

namespace Bench;

public class MyPosition
{
    public double X { get; set; }
    public double Y { get; set; }
    public double Z { get; set; }
}

[MemoryDiagnoser(false)]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class ZerroAllocDecomposer
{
    [Params(1, 1_000)]
    public int Size { get; set; }

    private List<MyPosition> _list = null!;
    AotPrimitiveDecomposer _dcs;
    MyArrayBufferWriter _buf;

    [GlobalSetup]
    public void Setup()
    {
        _dcs = new AotPrimitiveDecomposer();
        _buf = new MyArrayBufferWriter(30000);
        _list = new(Size);
        for (int i = 0; i < Size; i++)
            _list.Add(new MyPosition() { X = i, Y = i / 2d, Z = i / 3d });
        _dcs.Decompose(new MyPosition(), _buf);
    }

    [Benchmark(Baseline = true)]
    public void Decompose()
    {
        _buf.Clear();
        //_dcs.Decompose(_list, _buf);
        foreach (var item in _list)
            _dcs.Decompose(item, _buf);
    }
}
