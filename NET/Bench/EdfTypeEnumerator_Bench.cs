using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;

namespace Bench;
/*
| Method                                    | Job            | Mean       | Ratio | Allocated | Alloc Ratio |
|------------------------------------------ |--------------- |-----------:|------:|----------:|------------:|
| EdfTypeEnumeratorStack                    | .NET 10.0      |   173.5 ns |  1.00 |         - |          NA |
| EdfTypeEnumeratorStackInlineArray         | .NET 10.0      |   213.1 ns |  1.23 |         - |          NA |
| EdfTypeEnumeratorStackInlineArrayUseCache | .NET 10.0      |   105.6 ns |  0.61 |         - |          NA |
| EdfTypeEnumeratorTokenNoCache             | .NET 10.0      |   330.2 ns |  1.90 |         - |          NA |
| EdfTypeEnumeratorTokenUseCache            | .NET 10.0      |   122.9 ns |  0.71 |         - |          NA |
| EdfTypeEnumeratorRecursive                | .NET 10.0      |   147.3 ns |  0.85 |         - |          NA |
| EdfTypeEnumeratorRecursiveClass           | .NET 10.0      |   156.0 ns |  0.90 |         - |          NA |
| EdfTypeEnumeratorYield                    | .NET 10.0      | 1,152.9 ns |  6.65 |    3968 B |          NA |
| EdfTypeEnumeratorStack                    | NativeAOT 10.0 |   168.5 ns |  0.97 |         - |          NA |
| EdfTypeEnumeratorStackInlineArray         | NativeAOT 10.0 |   197.0 ns |  1.14 |         - |          NA |
| EdfTypeEnumeratorStackInlineArrayUseCache | NativeAOT 10.0 |   146.2 ns |  0.84 |         - |          NA |
| EdfTypeEnumeratorTokenNoCache             | NativeAOT 10.0 |   386.1 ns |  2.23 |         - |          NA |
| EdfTypeEnumeratorTokenUseCache            | NativeAOT 10.0 |   191.9 ns |  1.11 |         - |          NA |
| EdfTypeEnumeratorRecursive                | NativeAOT 10.0 |   163.6 ns |  0.94 |         - |          NA |
| EdfTypeEnumeratorRecursiveClass           | NativeAOT 10.0 |   150.9 ns |  0.87 |         - |          NA |
| EdfTypeEnumeratorYield                    | NativeAOT 10.0 | 1,945.2 ns | 11.21 |    3968 B |          NA | 
 */

[MemoryDiagnoser(false)]
[HideColumns("Runtime", "Error", "StdDev", "Median", "RatioSD")]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class EdfTypeEnumerator_Bench
{
    readonly EdfTypeEnumerator_Test _tst = new();
    [Benchmark(Baseline = true)] public void EdfTypeEnumeratorStack() => _tst.EdfTypeEnumeratorStack();
    [Benchmark]
    public void EdfTypeEnumeratorStackInlineArray()
    {
        _tst._enm.EnableCache = false;
        _tst.EdfTypeEnumeratorStackInlineArray();
    }
    [Benchmark]
    public void EdfTypeEnumeratorStackInlineArrayUseCache()
    {
        _tst._enm.EnableCache = true;
        _tst.EdfTypeEnumeratorStackInlineArray();
    }
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
    [Benchmark] public void EdfTypeEnumeratorRecursive() => _tst.EdfTypeEnumeratorRecursive();
    [Benchmark] public void EdfTypeEnumeratorRecursiveClass() => _tst.EdfTypeEnumeratorRecursiveClass();
    [Benchmark] public void EdfTypeEnumeratorYield() => _tst.EdfTypeEnumeratorYield();
}
