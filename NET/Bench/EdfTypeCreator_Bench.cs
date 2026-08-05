using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;

namespace Bench;
/*
| Method     | Job            | Runtime        | Mean       | Error    | StdDev    | Ratio | RatioSD | Allocated | Alloc Ratio |
|----------- |--------------- |--------------- |-----------:|---------:|----------:|------:|--------:|----------:|------------:|
| Generator  | .NET 10.0      | .NET 10.0      |   141.1 ns |  2.82 ns |   3.47 ns |  1.00 |    0.03 |   1.46 KB |        1.00 |
| Reflection | .NET 10.0      | .NET 10.0      | 5,329.8 ns | 98.09 ns | 169.19 ns | 37.81 |    1.48 |   3.13 KB |        2.14 |
| Generator  | NativeAOT 10.0 | NativeAOT 10.0 |   152.2 ns |  3.06 ns |   4.76 ns |  1.08 |    0.04 |   1.46 KB |        1.00 |
| Reflection | NativeAOT 10.0 | NativeAOT 10.0 | 4,120.2 ns | 81.93 ns |  76.64 ns | 29.23 |    0.87 |   1.66 KB |        1.14 |
 */

[MemoryDiagnoser(false)]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class EdfTypeCreator_Bench
{
    readonly EdfTypeCreator_Test _tst = new();

    [GlobalSetup]
    public void Setup() { }

    [Benchmark(Baseline = true)]
    public void Generator() => _tst.GetGenSchema();
    [Benchmark]
    public void Reflection() => _tst.GetReflSchema();
}
