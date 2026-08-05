using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;

namespace Bench;
/*
| Method                            | Job            | Runtime        | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|---------------------------------- |--------------- |--------------- |-----------:|---------:|---------:|------:|--------:|----------:|------------:|
| EdfTypeEnumeratorStack            | .NET 10.0      | .NET 10.0      |   134.5 ns |  0.39 ns |  0.32 ns |  1.00 |    0.00 |         - |          NA |
| EdfTypeEnumeratorStackInlineArray | .NET 10.0      | .NET 10.0      |   161.8 ns |  1.19 ns |  1.00 ns |  1.20 |    0.01 |         - |          NA |
| EdfTypeEnumeratorYield            | .NET 10.0      | .NET 10.0      | 1,256.9 ns | 22.90 ns | 36.98 ns |  9.34 |    0.27 |    3968 B |          NA |
| EdfTypeEnumeratorRecursive        | .NET 10.0      | .NET 10.0      |   121.5 ns |  0.91 ns |  0.81 ns |  0.90 |    0.01 |         - |          NA |
| EdfTypeEnumeratorRecursiveClass   | .NET 10.0      | .NET 10.0      |   144.8 ns |  0.83 ns |  0.74 ns |  1.08 |    0.01 |         - |          NA |
| EdfTypeEnumeratorStack            | NativeAOT 10.0 | NativeAOT 10.0 |   250.0 ns |  2.77 ns |  2.59 ns |  1.86 |    0.02 |         - |          NA |
| EdfTypeEnumeratorStackInlineArray | NativeAOT 10.0 | NativeAOT 10.0 |   246.4 ns |  2.34 ns |  2.19 ns |  1.83 |    0.02 |         - |          NA |
| EdfTypeEnumeratorYield            | NativeAOT 10.0 | NativeAOT 10.0 | 2,759.6 ns | 38.36 ns | 39.39 ns | 20.51 |    0.29 |    3968 B |          NA |
| EdfTypeEnumeratorRecursive        | NativeAOT 10.0 | NativeAOT 10.0 |   225.1 ns |  4.28 ns |  4.00 ns |  1.67 |    0.03 |         - |          NA |
| EdfTypeEnumeratorRecursiveClass   | NativeAOT 10.0 | NativeAOT 10.0 |   240.7 ns |  1.64 ns |  1.53 ns |  1.79 |    0.01 |         - |          NA |
 */

[MemoryDiagnoser(false)]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class EdfTypeEnumerator_Bench
{
    readonly EdfTypeEnumerator_Test _tst = new();
    /*
    [Params(1, 1_000)]
    public int Size { get; set; }
    */
    [GlobalSetup]
    public void Setup() { }

    [Benchmark(Baseline = true)]
    public void EdfTypeEnumeratorStack() => _tst.EdfTypeEnumeratorStack();
    [Benchmark]
    public void EdfTypeEnumeratorStackInlineArray() => _tst.EdfTypeEnumeratorStackInlineArray();
    [Benchmark]
    public void EdfTypeEnumeratorYield() => _tst.EdfTypeEnumeratorYield();
    [Benchmark]
    public void EdfTypeEnumeratorRecursive() => _tst.EdfTypeEnumeratorRecursive();
    [Benchmark]
    public void EdfTypeEnumeratorRecursiveClass() => _tst.EdfTypeEnumeratorRecursiveClass();
}
