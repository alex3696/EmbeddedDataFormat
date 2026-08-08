using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;

namespace Bench;

/*
PrimitiveDecomposer + Enum
| Method           | Job            | Runtime        | Size   | Mean             | Error          | StdDev         | Ratio | RatioSD | Allocated  | Alloc Ratio |
|----------------- |--------------- |--------------- |------- |-----------------:|---------------:|---------------:|------:|--------:|-----------:|------------:|
| Writer_Enum      | .NET 10.0      | .NET 10.0      | 1      |         28.50 ns |       0.325 ns |       0.304 ns |  1.00 |    0.01 |          - |          NA |
| Writer_Reflexion | .NET 10.0      | .NET 10.0      | 1      |        193.95 ns |       3.565 ns |       2.977 ns |  6.81 |    0.12 |      424 B |          NA |
| Writer_Enum      | NativeAOT 10.0 | NativeAOT 10.0 | 1      |         47.84 ns |       0.376 ns |       0.333 ns |  1.68 |    0.02 |          - |          NA |
| Writer_Reflexion | NativeAOT 10.0 | NativeAOT 10.0 | 1      |        945.70 ns |       6.819 ns |       6.378 ns | 33.19 |    0.40 |      352 B |          NA |
|                  |                |                |        |                  |                |                |       |         |            |             |
| Writer_Enum      | .NET 10.0      | .NET 10.0      | 100000 |  2,710,992.22 ns |  27,689.357 ns |  23,121.867 ns |  1.00 |    0.01 |          - |          NA |
| Writer_Reflexion | .NET 10.0      | .NET 10.0      | 100000 | 19,248,642.50 ns | 157,989.261 ns | 147,783.253 ns |  7.10 |    0.08 | 42401222 B |          NA |
| Writer_Enum      | NativeAOT 10.0 | NativeAOT 10.0 | 100000 |  4,503,099.76 ns |  35,819.665 ns |  29,911.042 ns |  1.66 |    0.02 |          - |          NA |
| Writer_Reflexion | NativeAOT 10.0 | NativeAOT 10.0 | 100000 | 93,740,408.89 ns | 431,758.175 ns | 403,866.865 ns | 34.58 |    0.32 | 35200000 B |          NA | 
 
 
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
    public void Writer_Enum() => _test.Writer_Enum();
    [Benchmark]
    public void Writer_Reflection() => _test.Writer_Reflection();
}
