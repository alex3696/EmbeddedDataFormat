using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;
using System.Buffers;

namespace Bench;


[MemoryDiagnoser(false)]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class DecomposersBench
{
    [Params(1, 1_000)]
    public int Size { get; set; }

    private MyPosition[] _list;
    private readonly NetTest.Decomposers _decomposers = new();
    private readonly ArrayBufferWriter<byte> _buf;
    public DecomposersBench()
    {
        _buf = new ArrayBufferWriter<byte>(32);
        _decomposers.StdReflection_GetValue(Decomposers.DefaultVal, _buf); _buf.ResetWrittenCount();
        _decomposers.Delegate1_GetValue(Decomposers.DefaultVal, _buf); _buf.ResetWrittenCount();
        _decomposers.StackDecomposer_GetValue(Decomposers.DefaultVal, _buf); _buf.ResetWrittenCount();
        _decomposers.Generator_GetValue(Decomposers.DefaultVal, _buf); _buf.ResetWrittenCount();
    }

    [GlobalSetup]
    public void Setup()
    {
        _list = new MyPosition[Size];
        for (int i = 0; i < Size; i++)
            _list[i] = new MyPosition() { X = i, Y = i, Z = i };
    }

    [Benchmark(Baseline = true)]
    public void StdReflection_GetValue()
    {
        for (int i = 0; i < Size; i++)
        {
            _buf.ResetWrittenCount();
            _decomposers.StdReflection_GetValue(_list[i], _buf);
        }
    }
    [Benchmark]
    public void StackDecomposer_GetValue()
    {
        for (int i = 0; i < Size; i++)
        {
            _buf.ResetWrittenCount();
            _decomposers.StackDecomposer_GetValue(_list[i], _buf);
        }
    }
    [Benchmark]
    public void Delegate1_GetValue()
    {
        for (int i = 0; i < Size; i++)
        {
            _buf.ResetWrittenCount();
            _decomposers.Delegate1_GetValue(_list[i], _buf);
        }
    }
    [Benchmark]
    public void Generator_GetValue()
    {
        for (int i = 0; i < Size; i++)
        {
            _buf.ResetWrittenCount();
            _decomposers.Generator_GetValue(_list[i], _buf);
        }
    }
}
