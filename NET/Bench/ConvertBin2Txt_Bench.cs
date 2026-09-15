using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;

namespace Bench;

/*
12700
| Method           | Runtime        | Mean    | Ratio | Allocated | Alloc Ratio |
|----------------- |--------------- |--------:|------:|----------:|------------:|
| 'Binary -> Text' | .NET 10.0      | 1.425 s |  1.00 |  16.32 KB |        1.00 |
| 'Binary -> Text' | NativeAOT 10.0 | 2.008 s |  1.41 |  26.38 KB |        1.62 |
|                  |                |         |       |           |             |
| 'Text -> Binary' | .NET 10.0      | 1.991 s |  1.00 |  14.28 KB |        1.00 |
| 'Text -> Binary' | NativeAOT 10.0 | 2.409 s |  1.21 |  25.69 KB |        1.80 |

8845hs
| Method           | Runtime        | Mean    | Ratio | Allocated | Alloc Ratio |
|----------------- |--------------- |--------:|------:|----------:|------------:|
| 'Binary -> Text' | .NET 10.0      | 1.572 s |  1.00 |  16.32 KB |        1.00 |
| 'Binary -> Text' | NativeAOT 10.0 | 2.471 s |  1.57 |  24.41 KB |        1.50 |
|                  |                |         |       |           |             |
| 'Text -> Binary' | .NET 10.0      | 2.449 s |  1.00 |  14.28 KB |        1.00 |
| 'Text -> Binary' | NativeAOT 10.0 | 2.724 s |  1.11 |  25.41 KB |        1.78 |

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

