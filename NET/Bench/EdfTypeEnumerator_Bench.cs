using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;

namespace Bench;
/*
| Method                            | Job            | Mean       | Ratio | Allocated | Alloc Ratio |
|---------------------------------- |--------------- |-----------:|------:|----------:|------------:|
| EdfTypeEnumeratorStack            | .NET 10.0      |   134.7 ns |  1.00 |         - |          NA |
| EdfTypeEnumeratorStackInlineArray | .NET 10.0      |   155.7 ns |  1.16 |         - |          NA |
| EdfTypeEnumeratorTokenNoCache     | .NET 10.0      |   285.3 ns |  2.12 |         - |          NA |
| EdfTypeEnumeratorTokenUseCache    | .NET 10.0      |   150.5 ns |  1.12 |         - |          NA |
| EdfTypeEnumeratorTokenBuildAnUse  | .NET 10.0      |   435.5 ns |  3.23 |         - |          NA |
| EdfTypeEnumeratorRecursive        | .NET 10.0      |   123.7 ns |  0.92 |         - |          NA |
| EdfTypeEnumeratorRecursiveClass   | .NET 10.0      |   149.0 ns |  1.11 |         - |          NA |
| EdfTypeEnumeratorYield            | .NET 10.0      | 1,213.8 ns |  9.02 |    3968 B |          NA |
| EdfTypeEnumeratorStack            | NativeAOT 10.0 |   237.4 ns |  1.76 |         - |          NA |
| EdfTypeEnumeratorStackInlineArray | NativeAOT 10.0 |   249.2 ns |  1.85 |         - |          NA |
| EdfTypeEnumeratorTokenNoCache     | NativeAOT 10.0 |   526.8 ns |  3.91 |         - |          NA |
| EdfTypeEnumeratorTokenUseCache    | NativeAOT 10.0 |   248.2 ns |  1.84 |         - |          NA |
| EdfTypeEnumeratorTokenBuildAnUse  | NativeAOT 10.0 |   740.1 ns |  5.50 |         - |          NA |
| EdfTypeEnumeratorRecursive        | NativeAOT 10.0 |   221.7 ns |  1.65 |         - |          NA |
| EdfTypeEnumeratorRecursiveClass   | NativeAOT 10.0 |   230.7 ns |  1.71 |         - |          NA |
| EdfTypeEnumeratorYield            | NativeAOT 10.0 | 2,720.7 ns | 20.21 |    3968 B |          NA | 
 */

[MemoryDiagnoser(false)]
[HideColumns("Runtime", "Error", "StdDev", "Median", "RatioSD")]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class EdfTypeEnumerator_Bench
{
    readonly EdfTypeEnumerator_Test _tst = new();
    [Benchmark(Baseline = true)] public void EdfTypeEnumeratorStack() => _tst.EdfTypeEnumeratorStack();
    [Benchmark] public void EdfTypeEnumeratorStackInlineArray() => _tst.EdfTypeEnumeratorStackInlineArray();
    [Benchmark]
    public void EdfTypeEnumeratorTokenNoCache()
    {
        _tst._enmToken.EnableCache = false;
        _tst.EdfTypeEnumeratorToken();
    }
    [Benchmark]
    public void EdfTypeEnumeratorTokenUseCache()
    {
        _tst._enmToken.EnableCache = true;
        _tst.EdfTypeEnumeratorToken();
    }
    [Benchmark]
    public void EdfTypeEnumeratorTokenBuildAnUse()
    {
        _tst._enmToken.EnableCache = true;
        _tst._enmToken.Reset(null);
        _tst.EdfTypeEnumeratorToken();
    }
    [Benchmark] public void EdfTypeEnumeratorRecursive() => _tst.EdfTypeEnumeratorRecursive();
    [Benchmark] public void EdfTypeEnumeratorRecursiveClass() => _tst.EdfTypeEnumeratorRecursiveClass();
    [Benchmark] public void EdfTypeEnumeratorYield() => _tst.EdfTypeEnumeratorYield();
}
