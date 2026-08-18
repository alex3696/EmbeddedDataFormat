using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;

namespace Bench;

/*
// ref fn
| Method      | Job            | Runtime        | Mean     | Error   | StdDev  | Ratio | Allocated | Alloc Ratio |
|------------ |--------------- |--------------- |---------:|--------:|--------:|------:|----------:|------------:|
| Writer_Enum | .NET 10.0      | .NET 10.0      | 365.8 ns | 1.73 ns | 1.45 ns |  1.00 |         - |          NA |
| Writer_Enum | NativeAOT 10.0 | NativeAOT 10.0 | 505.5 ns | 1.96 ns | 1.74 ns |  1.38 |         - |          NA |

| Method      | Job            | Runtime        | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------ |--------------- |--------------- |-----------:|---------:|---------:|------:|--------:|----------:|------------:|
| Writer_Gen2 | .NET 10.0      | .NET 10.0      |   696.5 ns |  3.29 ns |  3.08 ns |  1.00 |    0.01 |         - |          NA |
| Writer_Gen2 | NativeAOT 10.0 | NativeAOT 10.0 | 3,029.6 ns | 13.57 ns | 12.69 ns |  4.35 |    0.03 |     833 B |          NA |
 */


[MemoryDiagnoser(false)]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class WriterBin_Bench
{
    private readonly TestWriterBin _test = new();

    //[Params(1, 1000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        //_test.Setup(Size);
    }

    [Benchmark(Baseline = true)]
    public void Writer_Gen2() => _test.Writer_Gen2();
}
