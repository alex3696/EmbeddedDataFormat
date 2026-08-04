using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;

namespace Bench;
/*
| Method                          | Job            | Runtime        | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|-------------------------------- |--------------- |--------------- |-----------:|---------:|---------:|------:|--------:|----------:|------------:|
| EdfTypeEnumeratorStack          | .NET 10.0      | .NET 10.0      |   143.0 ns |  0.92 ns |  0.71 ns |  1.00 |    0.01 |         - |          NA |
| EdfTypeEnumeratorYield          | .NET 10.0      | .NET 10.0      | 1,283.6 ns | 25.63 ns | 60.41 ns |  8.98 |    0.42 |    3968 B |          NA |
| EdfTypeEnumeratorRecursive      | .NET 10.0      | .NET 10.0      |   131.7 ns |  1.10 ns |  0.98 ns |  0.92 |    0.01 |         - |          NA |
| EdfTypeEnumeratorRecursiveClass | .NET 10.0      | .NET 10.0      |   160.8 ns |  2.47 ns |  2.19 ns |  1.12 |    0.02 |         - |          NA |
| EdfTypeEnumeratorStack          | NativeAOT 10.0 | NativeAOT 10.0 |   263.4 ns |  4.39 ns |  4.11 ns |  1.84 |    0.03 |         - |          NA |
| EdfTypeEnumeratorYield          | NativeAOT 10.0 | NativeAOT 10.0 | 2,925.0 ns | 50.89 ns | 47.60 ns | 20.46 |    0.34 |    3968 B |          NA |
| EdfTypeEnumeratorRecursive      | NativeAOT 10.0 | NativeAOT 10.0 |   306.3 ns |  3.13 ns |  2.92 ns |  2.14 |    0.02 |         - |          NA |
| EdfTypeEnumeratorRecursiveClass | NativeAOT 10.0 | NativeAOT 10.0 |   238.6 ns |  1.19 ns |  1.06 ns |  1.67 |    0.01 |         - |          NA |
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
    public void EdfTypeEnumeratorYield() => _tst.EdfTypeEnumeratorYield();
    [Benchmark]
    public void EdfTypeEnumeratorRecursive() => _tst.EdfTypeEnumeratorRecursive();
    [Benchmark]
    public void EdfTypeEnumeratorRecursiveClass() => _tst.EdfTypeEnumeratorRecursiveClass();
}
