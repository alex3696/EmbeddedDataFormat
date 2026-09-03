using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;

namespace Bench;

/*
| Method       | Runtime        | Mean       | Ratio | Gen0       | Allocated    | Alloc Ratio |
|------------- |--------------- |-----------:|------:|-----------:|-------------:|------------:|
| BinaryWriter | .NET 10.0      |   586.5 ms |  1.00 |          - |      5.22 KB |        1.00 |
| BinaryWriter | NativeAOT 10.0 |   758.7 ms |  1.29 |          - |     10.44 KB |        2.00 |
|              |                |            |       |            |              |             |
| TextWriter   | .NET 10.0      | 1,117.0 ms |  1.00 |          - |      8.68 KB |        1.00 |
| TextWriter   | NativeAOT 10.0 | 2,025.4 ms |  1.81 |          - |     13.69 KB |        1.58 |
|              |                |            |       |            |              |             |
| BinaryReader | .NET 10.0      |   745.8 ms |  1.00 | 91000.0000 | 750028.88 KB |        1.00 |
| BinaryReader | NativeAOT 10.0 |   864.4 ms |  1.16 | 91000.0000 | 750469.77 KB |        1.00 |
|              |                |            |       |            |              |             |
| TextReader   | .NET 10.0      | 2,058.2 ms |  1.00 | 91000.0000 | 750021.33 KB |        1.00 |
| TextReader   | NativeAOT 10.0 | 2,244.2 ms |  1.09 | 91000.0000 | 750015.63 KB |        1.00 |
*/


[MemoryDiagnoser(true)]
[HideColumns("Error", "StdDev", "Median", "RatioSD")]
//[SimpleJob(warmupCount: 0, iterationCount: 1, invocationCount: 1, launchCount: 1, runtimeMoniker: RuntimeMoniker.HostProcess, baseline: false)]
[SimpleJob(warmupCount: 0, iterationCount: 1, invocationCount: 1, launchCount: 1, runtimeMoniker: RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(warmupCount: 0, iterationCount: 1, invocationCount: 1, launchCount: 1, runtimeMoniker: RuntimeMoniker.NativeAot10_0, baseline: false)]
public class ReaderWriterFileBench
{
    TestConverters _test = new();

    //[Params(1, 1000)]
    public int Size { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        //_test.Setup(Size);
    }

    [Benchmark] public void BinaryWriter() => _test.CreateBin();
    [Benchmark] public void TextWriter() => _test.CreateText();
    [Benchmark] public void BinaryReader() => _test.BinaryReader();
    [Benchmark] public void TextReader() => _test.TextReader();
}
