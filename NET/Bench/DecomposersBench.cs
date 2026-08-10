using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;

namespace Bench;
/*
| Method                   | Job            | Runtime        | Mean       | Error    | StdDev   | Ratio | RatioSD | Allocated | Alloc Ratio |
|------------------------- |--------------- |--------------- |-----------:|---------:|---------:|------:|--------:|----------:|------------:|
| StdReflection_GetValue   | .NET 10.0      | .NET 10.0      | 1,767.8 ns | 21.49 ns | 20.11 ns |  1.00 |    0.02 |    3944 B |        1.00 |
| StackDecomposer_GetValue | .NET 10.0      | .NET 10.0      |   815.8 ns |  2.22 ns |  2.08 ns |  0.46 |    0.01 |     608 B |        0.15 |
| YeldDecomposer_GetValue  | .NET 10.0      | .NET 10.0      | 1,085.0 ns | 10.74 ns |  9.52 ns |  0.61 |    0.01 |    2016 B |        0.51 |
| Generator_GetValue       | .NET 10.0      | .NET 10.0      |   191.4 ns |  1.05 ns |  0.98 ns |  0.11 |    0.00 |      64 B |        0.02 |
| StdReflection_GetValue   | NativeAOT 10.0 | NativeAOT 10.0 |         NA |       NA |       NA |     ? |       ? |        NA |           ? |
| StackDecomposer_GetValue | NativeAOT 10.0 | NativeAOT 10.0 |         NA |       NA |       NA |     ? |       ? |        NA |           ? |
| YeldDecomposer_GetValue  | NativeAOT 10.0 | NativeAOT 10.0 |         NA |       NA |       NA |     ? |       ? |        NA |           ? |
| Generator_GetValue       | NativeAOT 10.0 | NativeAOT 10.0 |   350.9 ns |  1.77 ns |  1.66 ns |  0.20 |    0.00 |      64 B |        0.02 |
 */

[MemoryDiagnoser(false)]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class DecomposersBench
{
    //[Params(1, 1_000)]
    public int Size { get; set; } = 1;

    private readonly NetTest.Decomposers _decomposers = new();
    public DecomposersBench()
    {
    }

    [GlobalSetup]
    public void Setup()
    {
    }

    [Benchmark(Baseline = true)]
    public void Generator_GetValue() => _decomposers.Generator_GetValue();
}
