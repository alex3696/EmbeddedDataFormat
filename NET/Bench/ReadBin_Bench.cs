using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using EdfNet.Core;
using NetTest;
using System;
using System.IO;

namespace Bench;

/*
| Method                | Job            | InvocationCount | Mean       | Ratio | Allocated | Alloc Ratio |
|---------------------- |--------------- |---------------- |-----------:|------:|----------:|------------:|
| Read_Avg              | .NET 10.0      | 1               |   296.6 ns |  1.00 |     384 B |        1.00 |
| Read_Avg              | NativeAOT 10.0 | 1               |   425.5 ns |  1.44 |     384 B |        1.00 |
|                       |                |                 |            |       |           |             |
| Read_BeforeStartFirst | .NET 10.0      | 1000000         | 1,558.2 ns |  1.00 |   13241 B |        1.00 |
| Read_BeforeStartFirst | NativeAOT 10.0 | 1000000         | 2,365.3 ns |  1.52 |   13257 B |        1.00 |
|                       |                |                 |            |       |           |             |
| Read_First            | .NET 10.0      | 1000000         | 2,041.6 ns |  1.00 |   14009 B |        1.00 |
| Read_First            | NativeAOT 10.0 | 1000000         | 3,016.1 ns |  1.48 |   14026 B |        1.00 |

 */

[MemoryDiagnoser(false)]
[HideColumns("Runtime", "Error", "StdDev", "Median", "RatioSD")]
[SimpleJob(RuntimeMoniker.Net10_0, baseline: true)]
[SimpleJob(RuntimeMoniker.NativeAot10_0)]
public class ReadBin_Bench
{
#pragma warning disable CS8618
    MemoryStream _msGen;
    //EdfBinaryWriter _writerGen;
    EdfBinaryReader _reader;
#pragma warning restore CS8618
    //[Params(1, 1000)] public int Size { get; set; } 
    public const int NCOUNT = 1_000_000;
    public int Size { get; set; } = NCOUNT;

    [GlobalSetup]
    public void Setup()
    {
        _msGen = new MemoryStream(Size * 1000);
        using EdfBinaryWriter _writerGen = new(_msGen);
        _writerGen.WriteSchema(TestClasses_Content.KeyValSchema);
        for (int i = 0; i < Size; i++)
        {
            _writerGen.WriteValue(TestClasses_Content.TestValue);
        }
        _writerGen.Flush();

    }

    [IterationSetup]
    public void IterationSetup()
    {
        //if (!reader.ReadBlock())
        //    Assert.Fail("there are no block");
        //if (reader.GetBlockType() != EdfBlockType.Config)
        //    Assert.Fail("there are no config block");
        // Перед каждой итерацией сбрасываем поток и пропускаем мета-блоки
        _msGen.Position = 0;
        _reader = new EdfBinaryReader(_msGen);

        if (!_reader.ReadBlock() || _reader.GetBlockType() != EdfBlockType.Schema)
            throw new InvalidOperationException("Schema block missing");
        if (!_reader.ReadBlock() || _reader.GetBlockType() != EdfBlockType.Data)
            throw new InvalidOperationException("Data block missing");
    }

    public void ReadGen(int count)
    {
        while (0 < count--)
        {
            if (0 < _reader.DataAvailable)
            {
                _reader.ReadValue<ComplexType>();
                //var restored = _reader.ReadValue<ComplexType>();
                //if (!TestClasses_Content.TestValue.Equals(restored))
                //    throw new Exception("schema equals");
            }
            else
            {
                if (!_reader.ReadBlock() || _reader.GetBlockType() != EdfBlockType.Data)
                    throw new InvalidOperationException("Data block missing");
            }
        }
        //Console.WriteLine($"Blocks readed {block} Stream pos {_msGen.Position}");
    }
    [Benchmark(OperationsPerInvoke = 1)]
    [InvocationCount(NCOUNT)]
    public void Read_BeforeStartFirst()
    {
        IterationSetup();
    }
    [Benchmark(OperationsPerInvoke = 1)]
    [InvocationCount(NCOUNT)]
    public void Read_First()
    {
        IterationSetup();
        ReadGen(1);
    }

    [Benchmark(OperationsPerInvoke = NCOUNT)]
    public void Read_Avg() => ReadGen(NCOUNT);

}
