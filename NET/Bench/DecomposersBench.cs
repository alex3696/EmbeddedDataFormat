using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using NetTest;
using System.Buffers;

namespace Bench;
/*
// struct
| Method                   | Job            | Runtime        | Size | Mean           | Error         | StdDev        | Ratio | Allocated | Alloc Ratio |
|------------------------- |--------------- |--------------- |----- |---------------:|--------------:|--------------:|------:|----------:|------------:|
| StdReflection_GetValue   | .NET 10.0      | .NET 10.0      | 1    |     131.663 ns |     0.5704 ns |     0.5057 ns |  1.00 |     424 B |        1.00 |
| StackDecomposer_GetValue | .NET 10.0      | .NET 10.0      | 1    |     124.595 ns |     0.3639 ns |     0.3226 ns |  0.95 |     336 B |        0.79 |
| Generator_GetValue       | .NET 10.0      | .NET 10.0      | 1    |       4.813 ns |     0.0128 ns |     0.0114 ns |  0.04 |         - |        0.00 |
| StdReflection_GetValue   | NativeAOT 10.0 | NativeAOT 10.0 | 1    |      32.530 ns |     0.0946 ns |     0.0838 ns |  0.25 |      88 B |        0.21 |
| StackDecomposer_GetValue | NativeAOT 10.0 | NativeAOT 10.0 | 1    |      54.770 ns |     0.2673 ns |     0.2232 ns |  0.42 |     192 B |        0.45 |
| Generator_GetValue       | NativeAOT 10.0 | NativeAOT 10.0 | 1    |      12.143 ns |     0.0532 ns |     0.0497 ns |  0.09 |         - |        0.00 |
|                          |                |                |      |                |               |               |       |           |             |
| StdReflection_GetValue   | .NET 10.0      | .NET 10.0      | 1000 | 139,923.060 ns | 1,430.5327 ns | 1,338.1212 ns |  1.00 |  424000 B |        1.00 |
| StackDecomposer_GetValue | .NET 10.0      | .NET 10.0      | 1000 | 124,938.011 ns |   663.4878 ns |   620.6269 ns |  0.89 |  336000 B |        0.79 |
| Generator_GetValue       | .NET 10.0      | .NET 10.0      | 1000 |   4,058.391 ns |     7.6454 ns |     6.7775 ns |  0.03 |         - |        0.00 |
| StdReflection_GetValue   | NativeAOT 10.0 | NativeAOT 10.0 | 1000 |  30,702.673 ns |   209.4370 ns |   174.8894 ns |  0.22 |   88000 B |        0.21 |
| StackDecomposer_GetValue | NativeAOT 10.0 | NativeAOT 10.0 | 1000 |  55,064.289 ns |    57.8581 ns |    48.3141 ns |  0.39 |  192000 B |        0.45 |
| Generator_GetValue       | NativeAOT 10.0 | NativeAOT 10.0 | 1000 |  10,780.495 ns |    46.0001 ns |    43.0285 ns |  0.08 |         - |        0.00 |

// struct StrongBox
| Method                   | Job            | Runtime        | Size | Mean           | Error         | StdDev      | Ratio | Allocated | Alloc Ratio |
|------------------------- |--------------- |--------------- |----- |---------------:|--------------:|------------:|------:|----------:|------------:|
| StdReflection_GetValue   | .NET 10.0      | .NET 10.0      | 1    |     136.301 ns |     1.0503 ns |   0.9825 ns |  1.00 |     424 B |        1.00 |
| StackDecomposer_GetValue | .NET 10.0      | .NET 10.0      | 1    |     131.516 ns |     0.6160 ns |   0.5461 ns |  0.96 |     248 B |        0.58 |
| Generator_GetValue       | .NET 10.0      | .NET 10.0      | 1    |       4.840 ns |     0.0197 ns |   0.0185 ns |  0.04 |         - |        0.00 |
| StdReflection_GetValue   | NativeAOT 10.0 | NativeAOT 10.0 | 1    |      31.203 ns |     0.2328 ns |   0.2178 ns |  0.23 |      88 B |        0.21 |
| StackDecomposer_GetValue | NativeAOT 10.0 | NativeAOT 10.0 | 1    |      43.635 ns |     0.1890 ns |   0.1676 ns |  0.32 |     104 B |        0.25 |
| Generator_GetValue       | NativeAOT 10.0 | NativeAOT 10.0 | 1    |      10.529 ns |     0.0879 ns |   0.0823 ns |  0.08 |         - |        0.00 |
|                          |                |                |      |                |               |             |       |           |             |
| StdReflection_GetValue   | .NET 10.0      | .NET 10.0      | 1000 | 134,097.393 ns | 1,012.1580 ns | 946.7732 ns |  1.00 |  424000 B |        1.00 |
| StackDecomposer_GetValue | .NET 10.0      | .NET 10.0      | 1000 | 129,904.224 ns |   393.8759 ns | 349.1607 ns |  0.97 |  248000 B |        0.58 |
| Generator_GetValue       | .NET 10.0      | .NET 10.0      | 1000 |   4,096.178 ns |    29.9449 ns |  28.0105 ns |  0.03 |         - |        0.00 |
| StdReflection_GetValue   | NativeAOT 10.0 | NativeAOT 10.0 | 1000 |  31,022.610 ns |   548.3396 ns | 486.0888 ns |  0.23 |   88000 B |        0.21 |
| StackDecomposer_GetValue | NativeAOT 10.0 | NativeAOT 10.0 | 1000 |  46,223.953 ns |   400.6933 ns | 374.8088 ns |  0.34 |  104000 B |        0.25 |
| Generator_GetValue       | NativeAOT 10.0 | NativeAOT 10.0 | 1000 |  10,595.946 ns |    48.6452 ns |  43.1227 ns |  0.08 |         - |        0.00 |

// class
| Method                   | Job            | Runtime        | Size | Mean           | Error       | StdDev      | Ratio | Allocated | Alloc Ratio |
|------------------------- |--------------- |--------------- |----- |---------------:|------------:|------------:|------:|----------:|------------:|
| StdReflection_GetValue   | .NET 10.0      | .NET 10.0      | 1    |     140.399 ns |   1.2472 ns |   1.1056 ns |  1.00 |     424 B |        1.00 |
| StackDecomposer_GetValue | .NET 10.0      | .NET 10.0      | 1    |     126.033 ns |   0.6520 ns |   0.5780 ns |  0.90 |     248 B |        0.58 |
| Generator_GetValue       | .NET 10.0      | .NET 10.0      | 1    |       5.166 ns |   0.0184 ns |   0.0172 ns |  0.04 |         - |        0.00 |
| StdReflection_GetValue   | NativeAOT 10.0 | NativeAOT 10.0 | 1    |      30.688 ns |   0.2102 ns |   0.1966 ns |  0.22 |      88 B |        0.21 |
| StackDecomposer_GetValue | NativeAOT 10.0 | NativeAOT 10.0 | 1    |      42.694 ns |   0.1587 ns |   0.1484 ns |  0.30 |     104 B |        0.25 |
| Generator_GetValue       | NativeAOT 10.0 | NativeAOT 10.0 | 1    |      10.450 ns |   0.0493 ns |   0.0461 ns |  0.07 |         - |        0.00 |
|                          |                |                |      |                |             |             |       |           |             |
| StdReflection_GetValue   | .NET 10.0      | .NET 10.0      | 1000 | 129,576.186 ns | 465.7075 ns | 412.8376 ns |  1.00 |  424000 B |        1.00 |
| StackDecomposer_GetValue | .NET 10.0      | .NET 10.0      | 1000 | 125,911.200 ns | 481.3999 ns | 450.3018 ns |  0.97 |  248000 B |        0.58 |
| Generator_GetValue       | .NET 10.0      | .NET 10.0      | 1000 |   3,958.897 ns |  13.7382 ns |  12.8507 ns |  0.03 |         - |        0.00 |
| StdReflection_GetValue   | NativeAOT 10.0 | NativeAOT 10.0 | 1000 |  29,285.034 ns | 210.3367 ns | 196.7491 ns |  0.23 |   88000 B |        0.21 |
| StackDecomposer_GetValue | NativeAOT 10.0 | NativeAOT 10.0 | 1000 |  44,274.398 ns | 150.4010 ns | 140.6852 ns |  0.34 |  104000 B |        0.25 |
| Generator_GetValue       | NativeAOT 10.0 | NativeAOT 10.0 | 1000 |  10,313.167 ns |  31.6644 ns |  29.6189 ns |  0.08 |         - |        0.00 | 

 | Method                   | Job            | Runtime        | Size | Mean           | Error       | StdDev      | Ratio | Allocated | Alloc Ratio |
|------------------------- |--------------- |--------------- |----- |---------------:|------------:|------------:|------:|----------:|------------:|
| StdReflection_GetValue   | .NET 10.0      | .NET 10.0      | 1    |     142.851 ns |   0.9216 ns |   0.8621 ns |  1.00 |     424 B |        1.00 |
| StackDecomposer_GetValue | .NET 10.0      | .NET 10.0      | 1    |      71.604 ns |   0.4794 ns |   0.4484 ns |  0.50 |     120 B |        0.28 |
| Generator_GetValue       | .NET 10.0      | .NET 10.0      | 1    |       4.918 ns |   0.0224 ns |   0.0199 ns |  0.03 |         - |        0.00 |
| StdReflection_GetValue   | NativeAOT 10.0 | NativeAOT 10.0 | 1    |      32.299 ns |   0.0976 ns |   0.0913 ns |  0.23 |      88 B |        0.21 |
| StackDecomposer_GetValue | NativeAOT 10.0 | NativeAOT 10.0 | 1    |      54.091 ns |   0.1609 ns |   0.1505 ns |  0.38 |     120 B |        0.28 |
| Generator_GetValue       | NativeAOT 10.0 | NativeAOT 10.0 | 1    |      10.625 ns |   0.0310 ns |   0.0290 ns |  0.07 |         - |        0.00 |
|                          |                |                |      |                |             |             |       |           |             |
| StdReflection_GetValue   | .NET 10.0      | .NET 10.0      | 1000 | 133,974.578 ns | 417.1740 ns | 390.2248 ns |  1.00 |  424000 B |        1.00 |
| StackDecomposer_GetValue | .NET 10.0      | .NET 10.0      | 1000 |  75,714.315 ns | 374.9917 ns | 350.7674 ns |  0.57 |  120000 B |        0.28 |
| Generator_GetValue       | .NET 10.0      | .NET 10.0      | 1000 |   4,057.546 ns |  10.9409 ns |  10.2341 ns |  0.03 |         - |        0.00 |
| StdReflection_GetValue   | NativeAOT 10.0 | NativeAOT 10.0 | 1000 |  30,878.413 ns | 136.6034 ns | 121.0953 ns |  0.23 |   88000 B |        0.21 |
| StackDecomposer_GetValue | NativeAOT 10.0 | NativeAOT 10.0 | 1000 |  53,379.413 ns | 841.8392 ns | 787.4569 ns |  0.40 |  120000 B |        0.28 |
| Generator_GetValue       | NativeAOT 10.0 | NativeAOT 10.0 | 1000 |  10,525.222 ns |  47.4162 ns |  44.3531 ns |  0.08 |         - |        0.00 |

 */

[MemoryDiagnoser(false)]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class DecomposersBench
{
    [Params(1, 1_000)]
    public int Size { get; set; }

    private MyPosition[] _list;
    private readonly NetTest.Decomposers _decomposers = new();
    private readonly ArrayBufferWriter<byte> _buf;
    public DecomposersBench()
    {
        _buf = new ArrayBufferWriter<byte>(32);
        _decomposers.StdReflection_GetValue(Decomposers.DefaultVal, _buf); _buf.ResetWrittenCount();
        _decomposers.StackDecomposer_GetValue(Decomposers.DefaultVal, _buf); _buf.ResetWrittenCount();
        _decomposers.Generator_GetValue(Decomposers.DefaultVal, _buf); _buf.ResetWrittenCount();
    }

    [GlobalSetup]
    public void Setup()
    {
        _list = new MyPosition[Size];
        for (int i = 0; i < Size; i++)
            _list[i] = new MyPosition() { X = i, Y = i, Z = i };
    }

    [Benchmark(Baseline = true)]
    public void StdReflection_GetValue()
    {
        for (int i = 0; i < Size; i++)
        {
            _buf.ResetWrittenCount();
            _decomposers.StdReflection_GetValue(_list[i], _buf);
        }
    }
    [Benchmark]
    public void StackDecomposer_GetValue()
    {
        for (int i = 0; i < Size; i++)
        {
            _buf.ResetWrittenCount();
            _decomposers.StackDecomposer_GetValue(_list[i], _buf);
        }
    }
    [Benchmark]
    public void Generator_GetValue()
    {
        for (int i = 0; i < Size; i++)
        {
            _buf.ResetWrittenCount();
            _decomposers.Generator_GetValue(_list[i], _buf);
        }
    }
}
