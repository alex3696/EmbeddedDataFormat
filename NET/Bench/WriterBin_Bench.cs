using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;

namespace Bench;

/*
| Method      | Job            | Size | Mean         | Ratio | Allocated | Alloc Ratio |
|------------ |--------------- |----- |-------------:|------:|----------:|------------:|
| Writer_Gen2 | .NET 10.0      | 1    |     434.7 ns |  1.00 |         - |          NA |
| Writer_Gen2 | NativeAOT 10.0 | 1    |     624.0 ns |  1.44 |         - |          NA |
|             |                |      |              |       |           |             |
| Writer_Gen2 | .NET 10.0      | 1000 | 422,381.0 ns |  1.00 |         - |          NA |
| Writer_Gen2 | NativeAOT 10.0 | 1000 | 617,021.6 ns |  1.46 |       6 B |          NA |
*/


[MemoryDiagnoser(false)]
[HideColumns("Runtime", "Error", "StdDev", "Median", "RatioSD")]
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
