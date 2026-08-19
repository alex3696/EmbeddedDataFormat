using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;

namespace Bench;
/*
| Method                            | Job            | Mean       | Ratio | Allocated | Alloc Ratio |
|---------------------------------- |--------------- |-----------:|------:|----------:|------------:|
| EdfTypeEnumeratorStack            | .NET 10.0      |   162.9 ns |  1.00 |         - |          NA |
| EdfTypeEnumeratorStackInlineArray | .NET 10.0      |   189.4 ns |  1.16 |         - |          NA |
| EdfTypeEnumeratorYield            | .NET 10.0      | 1,186.3 ns |  7.28 |    3968 B |          NA |
| EdfTypeEnumeratorRecursive        | .NET 10.0      |   130.5 ns |  0.80 |         - |          NA |
| EdfTypeEnumeratorRecursiveClass   | .NET 10.0      |   137.9 ns |  0.85 |         - |          NA |
| EdfTypeEnumeratorToken            | .NET 10.0      |   322.8 ns |  1.98 |         - |          NA |
| EdfTypeEnumeratorStack            | NativeAOT 10.0 |   161.9 ns |  0.99 |         - |          NA |
| EdfTypeEnumeratorStackInlineArray | NativeAOT 10.0 |   188.5 ns |  1.16 |         - |          NA |
| EdfTypeEnumeratorYield            | NativeAOT 10.0 | 1,885.2 ns | 11.58 |    3968 B |          NA |
| EdfTypeEnumeratorRecursive        | NativeAOT 10.0 |   152.3 ns |  0.94 |         - |          NA |
| EdfTypeEnumeratorRecursiveClass   | NativeAOT 10.0 |   140.0 ns |  0.86 |         - |          NA |
| EdfTypeEnumeratorToken            | NativeAOT 10.0 |   396.5 ns |  2.43 |         - |          NA | 
 */

[MemoryDiagnoser(false)]
[HideColumns("Runtime", "Error", "StdDev", "Median", "RatioSD")]
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
    [Benchmark] public void EdfTypeEnumeratorYield() => _tst.EdfTypeEnumeratorYield();
    [Benchmark] public void EdfTypeEnumeratorRecursive() => _tst.EdfTypeEnumeratorRecursive();
    [Benchmark] public void EdfTypeEnumeratorRecursiveClass() => _tst.EdfTypeEnumeratorRecursiveClass();
    [Benchmark]
    public void EdfTypeEnumeratorToken() => _tst.EdfTypeEnumeratorToken();
}
