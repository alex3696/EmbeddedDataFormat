using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;

namespace Bench;

/*
| Method           | Runtime        | Mean    | Ratio | Allocated | Alloc Ratio |
|----------------- |--------------- |--------:|------:|----------:|------------:|
| 'Binary -> Text' | .NET 10.0      | 1.384 s |  1.00 |  16.68 KB |        1.00 |
| 'Binary -> Text' | NativeAOT 10.0 | 1.816 s |  1.31 |   25.8 KB |        1.55 |
|                  |                |         |       |           |             |
| 'Text -> Binary' | .NET 10.0      | 1.997 s |  1.00 |  14.64 KB |        1.00 |
| 'Text -> Binary' | NativeAOT 10.0 | 2.515 s |  1.26 |  26.05 KB |        1.78 |

8845hs
| Method           | Runtime        | Mean    | Ratio | Allocated | Alloc Ratio |
|----------------- |--------------- |--------:|------:|----------:|------------:|
| 'Binary -> Text' | .NET 10.0      | 1.561 s |  1.00 |  16.32 KB |        1.00 |
| 'Binary -> Text' | NativeAOT 10.0 | 2.566 s |  1.64 |  24.41 KB |        1.50 |
|                  |                |         |       |           |             |
| 'Text -> Binary' | .NET 10.0      | 2.453 s |  1.00 |  14.28 KB |        1.00 |
| 'Text -> Binary' | NativeAOT 10.0 | 3.090 s |  1.26 |  23.72 KB |        1.66 |
 */

[MemoryDiagnoser(true)]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
//[SimpleJob(warmupCount: 0, iterationCount: 1, invocationCount: 1, launchCount: 1, runtimeMoniker: RuntimeMoniker.HostProcess, baseline: false)]
[SimpleJob(warmupCount: 0, iterationCount: 1, invocationCount: 1, launchCount: 1, runtimeMoniker: RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(warmupCount: 0, iterationCount: 1, invocationCount: 1, launchCount: 1, runtimeMoniker: RuntimeMoniker.NativeAot10_0, baseline: false)]
public class ConvertBin2Txt_Bench
{
#pragma warning disable CS8618
    TestConverters _сonverter = new();
#pragma warning restore CS8618
    [GlobalSetup]
    public void Setup()
    {

    }

    [Benchmark(Description = "Binary -> Text")]
    public void BinToTxtConvert()
    {
        _сonverter.BinToTxtConvert();
    }

    [Benchmark(Description = "Text -> Binary")]
    public void TxtToBinConvert()
    {
        _сonverter.TxtToBinConvert();
    }
    [GlobalCleanup]
    public void DeleteConvertedFiles() => _сonverter.DeleteConvertedFiles();
}

