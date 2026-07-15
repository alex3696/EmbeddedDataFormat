using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;

namespace Bench;

[MemoryDiagnoser(false)]
[HideColumns("Job", "Error", "StdDev", "Median", "RatioSD")]
//[SimpleJob(RuntimeMoniker.Net80)]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class Schema
{
    GenSerializationTests? _tests;

    [GlobalSetup]
    public void Setup()
    {
        _tests = new();
    }

    [Benchmark(Baseline = true)]
    public void Schema_FlatIRecursive() => _tests?.Schema_FlatIRecursive();
    [Benchmark]
    public void Schema_FlatEnumerable() => _tests?.Schema_FlatEnumerable();

}
